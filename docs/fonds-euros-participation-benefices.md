# Fonds euros et participation aux bénéfices

Ce document décrit l'implémentation des fonds euros dans Life / Financial Life, ce qui a été ajouté, et comment utiliser la fonctionnalité dans l'application.

## Objectif fonctionnel

La fonctionnalité permet de gérer un support d'assurance-vie de type fonds euros, avec :

- une valeur comptable garantie ;
- une valorisation provisoire basée sur un taux indicatif, notamment le TME ;
- des lots de capital par contrat ;
- une participation aux bénéfices annuelle ;
- une opération de capitalisation `ParticipationBenefit` ;
- un écran back-office de paramétrage, simulation et clôture.

Le fonds euros n'est pas traité comme une unité de compte classique. Il utilise techniquement une valeur de part à `1`, mais sa valeur métier est calculée à partir des lots, des mouvements et des intérêts courus.

## Architecture ajoutée

### Backend API

Les principaux fichiers ajoutés sont :

- `Models/EuroFundModels.cs` : modèle de données fonds euros.
- `Dtos/EuroFund/EuroFundDtos.cs` : DTOs exposés par l'API.
- `Interfaces/IEuroFundServices.cs` : contrats de services.
- `Services/EuroFunds/EuroFundAccrualCalculator.cs` : calcul pur de PB.
- `Services/EuroFunds/EuroFundLotService.cs` : création et mise à jour des lots.
- `Services/EuroFunds/EuroFundValuationService.cs` : valorisation fonds euros.
- `Services/EuroFunds/EuroFundRevaluationService.cs` : simulation et clôture annuelle.
- `Controllers/EuroFundsController.cs` : endpoints back-office.

Les services sont enregistrés dans `Extensions/ServiceCollectionExtensions.cs`.

### Front-office back-office

L'écran d'administration est disponible dans :

- `front/src/features/euro-funds/EuroFundsAdminPage.tsx`
- `front/src/features/euro-funds/api.ts`
- `front/src/features/euro-funds/types.ts`

La route est :

```text
/back-office/euro-funds
```

L'entrée de menu est dans la section :

```text
Back-office Assurance > Fonds euros
```

## Modèle de données

Les tables ajoutées sont :

| Table                        | Rôle                                                                            |
| ---------------------------- | ------------------------------------------------------------------------------- |
| `EuroFundConfigurations`     | Paramétrage permanent du fonds euros.                                           |
| `EuroFundFinancialYears`     | Exercice annuel du fonds : TME, rendement actif, PPB, taux final servi, statut. |
| `ReferenceRates`             | Historique des taux de référence, notamment le TME.                             |
| `EuroFundLots`               | Lots de capital détenus par contrat et par fonds euros.                         |
| `EuroFundLotMovements`       | Historique des mouvements qui augmentent ou diminuent les lots.                 |
| `EuroFundRevaluations`       | Trace d'une clôture annuelle par contrat.                                       |
| `EuroFundRevaluationDetails` | Détail des segments de calcul de PB.                                            |

La table existante `FinancialSupports` est utilisée comme référentiel principal. Un fonds euros est identifié par :

```text
SupportNature = EuroFund
IsCapitalGuaranteed = true
SupportType = EURO
```

## Support APICIL Euro Garanti

Un support APICIL Euro Garanti a été créé en base locale :

```text
Id = 5272
Code = APICIL_EURO_GARANTI
Label = APICIL Euro Garanti
SupportType = EURO
SupportNature = EuroFund
Currency = EUR
Status = Actif
IsCapitalGuaranteed = true
LastValuationAmount = 1.00000
```

Requête SQL de création pour la production :

```sql
IF NOT EXISTS (
    SELECT 1
    FROM dbo.FinancialSupports
    WHERE Code = 'APICIL_EURO_GARANTI'
       OR LOWER(Label) = 'apicil euro garanti'
)
BEGIN
    INSERT INTO dbo.FinancialSupports (
        Code,
        Label,
        ISIN,
        SupportType,
        SupportNature,
        Currency,
        Status,
        MarketingName,
        LegalName,
        AssetManager,
        IsCapitalGuaranteed,
        IsClosed,
        CreatedDate,
        UpdatedDate,
        ContractManagementFeeOverrideEnabled,
        LastValuationAmount,
        LastValuationDate
    )
    VALUES (
        'APICIL_EURO_GARANTI',
        'APICIL Euro Garanti',
        '',
        'EURO',
        'EuroFund',
        'EUR',
        'Actif',
        'APICIL Euro Garanti',
        'APICIL Euro Garanti',
        'APICIL',
        1,
        0,
        SYSUTCDATETIME(),
        SYSUTCDATETIME(),
        0,
        1.00000,
        CONVERT(date, SYSUTCDATETIME())
    );
END;

SELECT
    Id,
    Code,
    Label,
    SupportType,
    SupportNature,
    Currency,
    Status,
    IsCapitalGuaranteed
FROM dbo.FinancialSupports
WHERE Code = 'APICIL_EURO_GARANTI'
   OR LOWER(Label) = 'apicil euro garanti';
```

## Calcul de participation aux bénéfices

Le calcul repose sur les lots.

Exemple :

1. Un versement de 100 000 EUR crée un lot de 100 000 EUR.
2. Un versement complémentaire crée un nouveau lot, avec sa propre date de valeur.
3. Un rachat ou arbitrage sortant diminue les lots.
4. À la clôture, chaque lot produit des intérêts selon sa durée d'exposition.

La formule actuellement implémentée est :

```text
PB = capital exposé × taux servi × nombre de jours / base annuelle
```

La base annuelle est :

- `365` pour une année normale ;
- `366` pour une année bissextile.

Le mode opérationnel livré est :

```text
Jours réels - prorata simple
```

Les autres conventions sont modélisées mais pas encore implémentées :

- quinzaine civile ;
- équivalent journalier composé ;
- règle personnalisée.

## Date de valeur

Le modèle Life existant ne contient pas encore de champ `ValueDate` dédié aux opérations.

L'implémentation utilise donc :

```text
OperationDate
```

comme date de valeur financière pour les lots fonds euros.

Si l'application introduit plus tard un vrai champ `ValueDate`, le service de lots pourra être adapté pour l'utiliser en priorité.

## Valorisation provisoire

La valorisation d'un fonds euros est composée de :

```text
valeur estimée = capital acquis + intérêts courus estimés
```

Le capital acquis vient des lots.

Les intérêts courus estimés sont calculés sans créer d'opération. Ils servent uniquement à afficher une valeur indicative avant clôture.

Le taux provisoire peut être paramétré avec :

| Méthode                       | Libellé écran                       | Usage                                                       |
| ----------------------------- | ----------------------------------- | ----------------------------------------------------------- |
| `None`                        | Aucun                               | Aucun intérêt provisoire.                                   |
| `FixedRate`                   | Taux fixe                           | Utilise un taux saisi manuellement.                         |
| `TmePercentage`               | Pourcentage du TME                  | Utilise le dernier TME connu multiplié par un pourcentage.  |
| `PreviousFinalRatePercentage` | Pourcentage du taux final précédent | Utilise un pourcentage du taux servi de l'année précédente. |
| `Custom`                      | Personnalisé                        | Réservé à une extension métier.                             |

Des planchers et plafonds peuvent être configurés.

## Intégration avec les opérations

Les fonds euros restent intégrés au moteur d'opérations existant.

### Entrées

Les mouvements suivants augmentent les lots :

- versement ;
- arbitrage entrant ;
- participation aux bénéfices ;
- paiement d'intérêts ou coupon si utilisé.

### Sorties

Les mouvements suivants diminuent les lots :

- rachat ;
- arbitrage sortant ;
- frais de gestion ;
- frais d'opération.

### Valeur technique

Pour rester compatible avec les tables existantes d'allocation et de holding :

```text
NAV = 1
CurrentShares = montant
CurrentAmount = montant valorisé par le service fonds euros
```

Le PRU d'un fonds euros n'a donc pas la même signification économique que le PRU d'une UC. Il est surtout technique.

## Utilisation dans le back-office

### 1. Créer ou vérifier le support fonds euros

Le support doit exister dans `FinancialSupports` avec :

```text
SupportNature = EuroFund
IsCapitalGuaranteed = true
```

Sinon il ne remonte pas dans l'écran `Fonds euros`.

### 2. Ouvrir l'écran Fonds euros

Aller dans :

```text
Back-office > Back-office Assurance > Fonds euros
```

ou directement :

```text
/back-office/euro-funds
```

### 3. Paramétrer le fonds

Dans le panneau de configuration :

- choisir la convention de calcul ;
- renseigner la date annuelle de crédit PB ;
- choisir le mode de taux provisoire ;
- renseigner le pourcentage du TME ou le taux fixe ;
- choisir le mode de sortie anticipée ;
- choisir le mode de consommation des lots ;
- indiquer si le taux est net ou brut de frais ;
- indiquer le traitement des frais de gestion ;
- saisir un plancher ou plafond si nécessaire.

Cliquer sur :

```text
Enregistrer
```

### 4. Historiser un TME

Dans la section `Historiser un TME` :

- saisir la date ;
- saisir le taux TME ;
- renseigner la source ;
- cliquer sur `Enregistrer TME`.

Ce TME peut ensuite servir à la valorisation provisoire et à la simulation.

### 5. Créer ou modifier l'exercice annuel

Dans `Exercice annuel`, renseigner :

- année ;
- TME ;
- rendement actif ;
- PPB début ;
- dotation PPB ;
- reprise PPB ;
- PPB fin ;
- taux final servi ;
- nature du taux final ;
- statut.

Cliquer sur :

```text
Enregistrer
```

### 6. Simuler la PB

Cliquer sur :

```text
Simuler
```

La simulation affiche :

- nombre de contrats concernés ;
- encours acquis ;
- PB totale ;
- PB moyenne ;
- taux appliqué ;
- détail par contrat ;
- nombre de segments utilisés pour le calcul.

La simulation ne crée aucune opération.

### 7. Finaliser l'exercice

Une fois le taux final servi validé, cliquer sur :

```text
Finaliser
```

La finalisation :

- calcule la PB par contrat ;
- crée une opération `ParticipationBenefit` exécutée ;
- capitalise la PB dans les lots ;
- trace la revalorisation ;
- marque l'exercice comme finalisé.

La finalisation est idempotente : une contrainte unique empêche de clôturer deux fois le même couple contrat / fonds / année.

## API disponible

Base route :

```text
/api/euro-funds
```

Endpoints principaux :

| Méthode | Route                                                   | Rôle                                     |
| ------- | ------------------------------------------------------- | ---------------------------------------- |
| `GET`   | `/api/euro-funds`                                       | Liste les supports fonds euros.          |
| `GET`   | `/api/euro-funds/{id}`                                  | Charge un fonds euros.                   |
| `PUT`   | `/api/euro-funds/{id}/configuration`                    | Enregistre la configuration.             |
| `GET`   | `/api/euro-funds/{id}/financial-years`                  | Liste les exercices annuels.             |
| `POST`  | `/api/euro-funds/{id}/financial-years`                  | Crée un exercice annuel.                 |
| `PUT`   | `/api/euro-funds/{id}/financial-years/{year}`           | Met à jour un exercice annuel.           |
| `POST`  | `/api/euro-funds/{id}/financial-years/{year}/preview`   | Simule la PB.                            |
| `POST`  | `/api/euro-funds/{id}/financial-years/{year}/finalize`  | Finalise la PB annuelle.                 |
| `POST`  | `/api/euro-funds/reference-rates`                       | Ajoute un taux de référence.             |
| `GET`   | `/api/euro-funds/{id}/contracts/{contractId}/valuation` | Valorise un fonds euros pour un contrat. |

## Migration et déploiement

La migration EF créée est :

```text
20260812160609_AddEuroFundParticipationBenefits
```

En local, elle a été appliquée avec :

```bash
dotnet ef database update
```

Pour la production :

1. Déployer le backend contenant les nouveaux modèles et services.
2. Appliquer la migration :

```bash
dotnet ef database update
```

ou appliquer le script SQL généré par EF selon la procédure de production.

3. Créer le support fonds euros APICIL si nécessaire avec la requête SQL fournie plus haut.
4. Déployer le front.
5. Ouvrir `/back-office/euro-funds` et paramétrer le fonds.

## Tests effectués

Backend :

```bash
dotnet test api.sln --no-restore
```

Résultat :

```text
152 tests passés
0 échec
```

Frontend :

```bash
npm run build
```

Résultat :

```text
Build réussi
```

## Limites connues

Les points suivants sont prêts dans le modèle mais pas encore entièrement automatisés :

- stratégies de consommation FIFO / LIFO / lots bonifiés en priorité ;
- convention quinzaine civile ;
- taux composé journalier ;
- moteur de campagnes bonus ;
- règles spécifiques de sortie anticipée ;
- reprise historique automatique des encours fonds euros existants.

Pour reprendre un historique existant, il faudra créer des `EuroFundLots` initiaux à partir des holdings ou allocations actuels, avec une date de valeur métier validée.
