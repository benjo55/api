# Génération PDF unifiée

## Objectif

Centraliser toute la génération PDF de Life, CERFA.top et Urbanisation.world derrière un seul parcours applicatif :

- une seule API de génération et de téléchargement ;
- une seule gestion des noms de fichiers, métadonnées, hash, stockage, erreurs et droits ;
- un rendu riche et prévisible pour texte structuré, tableaux, images, graphiques, annexes, modèles administratifs et documents multi-pages ;
- une stratégie de preview identique entre l'éditeur et le PDF final.

## Constat actuel

L'application contient plusieurs filières PDF :

- `api/documents/{documentType}/generate` : socle moderne avec `DocumentDefinition`, `IDocumentDataProvider`, `IDocumentRenderer`, preview/download/archive/email.
- `IPdfGenerationService` : conversion HTML vers PDF via Playwright, utilisée par les documents juridiques.
- `IPdfDocumentService` et `IPdfBusinessDocumentService` : génération de documents métier à partir de DTO PDF, avec templates et fusion PDF.
- `ITaxReceiptPdfGenerator` : génération des reçus fiscaux CERFA par remplissage de PDF officiel.
- `ICartographyDocumentService` : ancienne génération cartographie directe, partiellement redondante avec le socle `Documents`.
- anciennement `@react-pdf/renderer` côté front pour le PDF Boost, désormais remplacé par le type documentaire `boost-simulation`.

Le point d'entrée cible doit être le socle `Documents`, car il porte déjà les concepts transverses. Les moteurs spécialisés doivent devenir des renderers internes, pas des endpoints concurrents.

## Architecture cible

### 1. Orchestrateur unique

Conserver et enrichir `IDocumentGenerationService`.

Responsabilités :

- résolution du type documentaire ;
- contrôle des droits ;
- construction du modèle ;
- choix du renderer ;
- génération ;
- calcul du hash ;
- nommage ;
- stockage optionnel ;
- livraison `preview`, `download`, `archive`, `email` ;
- journalisation et corrélation.

### 2. Contrat de rendu unique

Tous les moteurs doivent exposer `IDocumentRenderer`.

Moteurs supportés :

- `QuestPdfDocumentRenderer` pour les documents métier structurés et très contrôlés ;
- `HtmlPdfDocumentRenderer` pour les documents éditoriaux riches, documents juridiques, tables des matières, CSS, pages complexes ;
- `PdfTemplateOverlayRenderer` pour les formulaires officiels CERFA à remplir sur gabarit PDF ;
- `PdfMergeRenderer` pour les dossiers composés de plusieurs PDF.

Le choix du moteur doit être une propriété de la `DocumentDefinition`, pas un endpoint séparé.

### 3. Modèle documentaire riche

Créer une représentation commune pour les blocs riches :

- paragraphes, titres, listes ;
- tableaux avec bordures, padding, alignements, couleur d'en-tête, couleur de ligne, couleur de cellule ;
- images et SVG ;
- graphiques ;
- sauts de page ;
- encadrés d'information, avertissement, erreur ;
- annexes ;
- variables métier ;
- signatures, QR codes, codes de contrôle.

Cette représentation doit pouvoir être rendue en HTML et en QuestPDF. L'éditeur riche doit produire un HTML compatible, mais l'API doit rester capable de normaliser ce HTML.

### 4. Stockage et artefacts

Étendre le socle `Documents` pour gérer :

- artefact temporaire de preview ;
- artefact final archivé ;
- remplacement/régénération si stockage purgé ;
- expiration des previews ;
- contenu hashé ;
- audit de génération ;
- téléchargement sécurisé.

Les documents juridiques et reçus fiscaux doivent réutiliser ce mécanisme au lieu d'avoir chacun leur circuit.

### 5. Front uniforme

Créer un seul client front `documentApi` et un seul composant d'action PDF :

- aperçu dans iframe ;
- ouverture nouvel onglet ;
- téléchargement ;
- régénération ;
- état "PDF obsolète" ;
- erreurs métiers lisibles ;
- progression quand une génération devient asynchrone.

Les écrans Cartographie, Contrat, CERFA, Documents juridiques et Boost doivent utiliser ce composant.

## Migration recommandée

### Étape 1 - Stabiliser le socle

- Ajouter une notion de moteur dans `DocumentDefinition`.
- Ajouter des options de rendu standard : page size, orientation, marges, header/footer, template, stockage.
- Extraire les helpers PDF communs : nommage, hash, merge, stockage, erreurs.
- Ajouter une suite de tests de rendu PDF avec inspection visuelle minimale.

### Étape 2 - Migrer les documents déjà proches

- Garder `contract-situation` et `information-system-cartography` dans `api/documents`.
- Supprimer progressivement les doublons de `ICartographyDocumentService` pour la génération PDF.
- Brancher le composant front unique sur ces deux documents.

### Étape 3 - Migrer les documents juridiques

- Envelopper `DocumentRenderService.RenderCanonicalHtml` dans un `IDocumentDataProvider`.
- Envelopper `IPdfGenerationService` dans un renderer HTML compatible `IDocumentRenderer`.
- Déplacer preview/download/artifact sous le socle documentaire.

Statut : le type documentaire `legal-document-revision` est enregistré dans le socle `api/documents` avec le moteur `HtmlToPdf`. Il réutilise le HTML canonique juridique et le service Playwright existant. L'ancien endpoint juridique reste disponible pendant la migration du front et du stockage d'artefacts.

### Étape 4 - Migrer contrats et dossiers client

- Transformer `PdfBusinessDocumentService` en providers + renderers.
- Remplacer `/api/pdf/contract-sheet` et `/api/pdf/client-case-file` par des types documentaires :
  - `contract-sheet`
  - `client-case-file`
  - `operations-history`
  - `asset-allocation-report`

Statut : le type documentaire `client-case-file` est enregistré dans le socle `api/documents` avec le moteur `PdfMerge`. Il réutilise `IPdfBusinessDocumentService.GenerateClientCaseFileAsync`, conserve les options d'inclusion des sous-documents et accepte les pièces PDF additionnelles. Le bouton de génération du dossier client côté contrat utilise maintenant `documentApi.generate("client-case-file", ...)`, avec fallback temporaire sur `/api/pdf/client-case-file`.

Statut : le type documentaire `contract-sheet` est enregistré dans le socle `api/documents`. Il réutilise `IPdfBusinessDocumentService.GenerateContractSheetAsync` et expose la fiche contrat en preview/download depuis l'écran détail contrat.

Statut : le type documentaire `operations-history` est enregistré dans le socle `api/documents`. Il réutilise `IPdfBusinessDocumentService.GenerateOperationsHistoryAsync` et expose l'historique des opérations en preview/download depuis l'écran détail contrat.

Statut : le type documentaire `asset-allocation-report` est enregistré dans le socle `api/documents`. Il réutilise `IPdfBusinessDocumentService.GenerateAssetAllocationReportAsync` et expose le rapport d'allocation d'actifs en preview/download depuis l'écran détail contrat.

### Étape 5 - Migrer les reçus fiscaux

- Conserver le remplissage du formulaire officiel comme moteur spécialisé.
- L'exposer comme type documentaire `tax-receipt`.
- Utiliser le même stockage, les mêmes métadonnées, le même audit et le même téléchargement sécurisé.

Statut : le type documentaire `tax-receipt` est enregistré dans le socle `api/documents` avec le moteur `PdfTemplateOverlay`. Il réutilise `ITaxReceiptService` pour régénérer le CERFA puis retourner le PDF officiel existant. Les écrans front de liste des reçus fiscaux et de détail donateur passent par `documentApi.generate("tax-receipt", ...)`, avec fallback temporaire sur les endpoints historiques.

### Étape 6 - Retirer les exceptions front

- Remplacer le PDF Boost front par une génération API.
- Garder le front pour l'aperçu et l'interaction, pas pour produire le PDF officiel.

Statut : le type documentaire `boost-simulation` est enregistré dans le socle `api/documents` avec le moteur `HtmlToPdf`. L'écran Boost envoie désormais la simulation locale à `documentApi.generate("boost-simulation", ...)` et propose preview/download via `DocumentActions`. Les composants Boost historiques basés sur `@react-pdf/renderer` et la dépendance npm associée ont été retirés.

## Règles de qualité

- Aucun PDF officiel ne doit être généré uniquement côté navigateur.
- Toute génération doit avoir un `CorrelationId`, un hash et un nom de fichier déterministe.
- Tout renderer doit avoir au moins un test de génération et un test de contenu minimal.
- Les documents à mise en page critique doivent avoir un test visuel par rendu PNG.
- Les tableaux doivent être testés explicitement : largeur, alignement, bordures, répétition d'en-tête si nécessaire.
- Les erreurs techniques doivent être traduites en messages utilisateur actionnables.

## Décision

Le socle `api.Services.Documents` devient la voie unique de génération PDF. Les services PDF existants ne sont pas supprimés immédiatement ; ils deviennent des moteurs internes appelés par `IDocumentRenderer`, puis les endpoints historiques sont dépréciés une fois les écrans front migrés.

Statut : les endpoints historiques `/api/pdf/generate`, `/api/pdf/merge`, `/api/pdf/contract-sheet` et `/api/pdf/client-case-file` sont conservés en compatibilité mais marqués dépréciés. Les écrans migrés passent par `/api/documents/{documentType}/generate`.
