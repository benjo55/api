# HelloAsso Donation Lot 1

## Architecture

Flux cible implemente:

```mermaid
flowchart TD
    A[Page publique /faire-un-don] --> B[POST /api/public/donations/checkout]
    B --> C[Creation Don + Donateur + PaymentAttempt]
    C --> D[Create checkout intent HelloAsso]
    D --> E[Redirection navigateur vers HelloAsso]
    E --> F[Retour navigateur /faire-un-don/retour]
    F --> G[Polling GET /api/public/donations/{publicId}/status]
    H[Webhook POST /api/webhooks/helloasso] --> I[Inbox PaymentWebhookInbox]
    I --> J[Traitement asynchrone background]
    J --> K[Reconciliation GET checkout-intent]
    K --> L[Donation Paid]
    L --> M[Generation CERFA]
    M --> N[Archivage PDF + hash SHA-256]
    N --> O[Envoi mail du recu fiscal]
```

Le retour navigateur n'est jamais considere comme preuve de paiement.

## Composants backend

- Provider de paiement abstrait: `IPaymentProvider`
- Implementation HelloAsso: `HelloAssoPaymentProvider`
- OAuth token cache+refresh: `IHelloAssoTokenProvider` / `HelloAssoTokenProvider`
- Orchestration domaine lot 1: `IPublicDonationService` / `PublicDonationService`
- Traitement asynchrone webhooks: `PaymentWebhookBackgroundService`

## Donnees

Extensions et ajouts:

- `Donation`:
  - `PublicId`, `Reference`, `OrganizationId`, `PaymentConfirmedAt`, `RowVersion`
- `BeneficiaryOrganization`:
  - `LegalName`, `RnaNumber`, `Siret`, `Address`, `Email`, `IsEligibleForTaxReceipt`, `FiscalArticle`, `HelloAssoOrganizationSlug`, `IsDonationEnabled`
- `PaymentAttempt` (nouvelle table)
- `PaymentWebhookInbox` (nouvelle table)

Migration: `AddHelloAssoDonationLot1`.

## Configuration

Sections ajoutees:

```json
{
  "DonationCheckout": {
    "MinAmountEur": 1.0,
    "MaxAmountEur": 10000.0,
    "StatusPollingMaxSeconds": 120,
    "ReceiptTokenLifetimeMinutes": 15
  },
  "HelloAsso": {
    "Enabled": false,
    "Environment": "Sandbox",
    "BaseUrl": "https://api.helloasso-sandbox.com",
    "ClientId": "",
    "ClientSecret": "",
    "OrganizationSlug": "",
    "WebhookSignatureKey": "",
    "AllowedWebhookIpAddresses": [],
    "ItemName": "Don a l'association",
    "ReturnUrl": "https://localhost:5173/faire-un-don/retour",
    "BackUrl": "https://localhost:5173/faire-un-don",
    "ErrorUrl": "https://localhost:5173/faire-un-don/erreur",
    "HttpTimeoutSeconds": 20,
    "RetryCount": 3
  }
}
```

Secrets attendus:

- `HelloAsso:ClientId`
- `HelloAsso:ClientSecret`
- `HelloAsso:WebhookSignatureKey` (si mode signature)

Stocker en dev dans User Secrets, en prod via variables d'environnement/secret store.

## Endpoints Life

Public:

- `POST /api/public/donations/checkout`
- `GET /api/public/donations/{publicId}/status`
- `POST /api/public/donations/{publicId}/receipt-token`
- `GET /api/public/donations/{publicId}/receipt?token=...`

Webhook:

- `POST /api/webhooks/helloasso`

Admin:

- `GET /api/admin/donations`
- `GET /api/admin/donations/{id}`
- `POST /api/admin/donations/{id}/reconcile`
- `POST /api/admin/donations/{id}/resend-receipt`

## Endpoints HelloAsso utilises

- `POST /oauth2/token`
- `POST /v5/organizations/{organizationSlug}/checkout-intents`
- `GET /v5/organizations/{organizationSlug}/checkout-intents/{checkoutIntentId}`

## OAuth

- Grant `client_credentials`
- Token cache en memoire
- Refresh anticipe avant expiration
- Verrou asynchrone anti-renouvellements concurrents
- Retry limite aux erreurs transitoires
- Retry unique apres `401`

## Webhook et reconciliation

- Verification HMAC (`x-ha-signature`) si `WebhookSignatureKey` est configure
- Sinon controle IP source (`AllowedWebhookIpAddresses`)
- Inbox idempotente par hash payload (`Provider + PayloadHash` unique)
- Traitement asynchrone et reessayable
- Reconciliation systematique via API HelloAsso avant validation paiement

## Regles CERFA

Generation seulement apres paiement autorise reconcile.

- Unicite logique du recu par don actif
- Reutilisation du moteur CERFA existant (2041-RD)
- Archivage via `IDocumentBinaryStorage`
- Hash SHA-256 enregistre
- Envoi mail separe de la generation

## Frontend

Routes publiques:

- `/faire-un-don`
- `/faire-un-don/retour`
- `/faire-un-don/erreur`

Route admin:

- `/admin/donations`

Le retour affiche d'abord "paiement en cours de confirmation" puis poll le statut API.

## Procedure Sandbox

1. Creer une organisation de test sur HelloAsso Sandbox.
2. Recuperer `ClientId` et `ClientSecret` Sandbox.
3. Configurer `OrganizationSlug`.
4. Declarer l'URL webhook publique: `POST /api/webhooks/helloasso`.
5. Configurer `ReturnUrl`, `BackUrl`, `ErrorUrl` en HTTPS.
6. Activer `HelloAsso:Enabled=true`.
7. Realiser un paiement carte de test.
8. Verifier inbox webhook, reconciliation, statut donation.
9. Verifier generation PDF CERFA et envoi mail.

## Passage production

1. Dupliquer config sur l'environnement prod.
2. Basculer `BaseUrl` vers API HelloAsso production.
3. Injecter secrets production via coffre/variables.
4. Configurer IPs webhook ou signature key.
5. Verifier le stockage documentaire et SMTP.
6. Effectuer un test bout-en-bout supervise.

## Diagnostic rapide

- `400`: payload checkout invalide
- `401`: token OAuth invalide/expire
- `403`: droits Checkout absents
- `409`: organisation HelloAsso non eligible/non verifiee
- `429`: rate limiting HelloAsso
- `5xx`: indisponibilite temporaire HelloAsso

## Relance webhook / renvoi recu

- Relance reconciliation: `POST /api/admin/donations/{id}/reconcile`
- Renvoi recu: `POST /api/admin/donations/{id}/resend-receipt`
