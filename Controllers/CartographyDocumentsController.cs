using api.Data;
using api.Dtos.Cmdb;
using api.Models.Cmdb;
using api.Services.Cmdb;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace api.Controllers;

[ApiController]
[Route("api/cartography")]
public sealed class CartographyDocumentsController : ControllerBase
{
    private const string GeneralDomainCode = "GENERAL";
    private const string GeneralDomainTitle = "Cartographie générale du SI ESF";
    private const string DefaultDomainSectionContentHtml =
        "<p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla aliquam felis eget lacus feugiat, maximus sodales est elementum. Donec vel justo dictum, dignissim tellus in, dapibus erat. Vestibulum commodo turpis non ipsum finibus dignissim. Vestibulum in sapien at neque sagittis ultrices. Ut eget arcu ut est placerat feugiat. Maecenas vel finibus nibh, a pharetra libero. Nam commodo nibh non facilisis sagittis.</p>" +
        "<p>Proin nec accumsan mi, sed viverra lectus. Mauris consectetur maximus dui porttitor sollicitudin. Mauris at purus elit. Suspendisse tincidunt et justo eget efficitur. Quisque sed pulvinar magna. Fusce a volutpat magna, at ultricies turpis. Aenean eu purus at augue facilisis tempus nec at nulla. Nulla nunc dolor, finibus at luctus suscipit, viverra ut urna. Fusce sit amet bibendum erat, vitae pharetra quam. Etiam quis lacus nec dolor cursus semper vitae in diam. Aliquam fermentum massa vel lobortis suscipit. Curabitur semper eros arcu.</p>";

    private const string DefaultDomainSectionPlainText =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla aliquam felis eget lacus feugiat, maximus sodales est elementum. Donec vel justo dictum, dignissim tellus in, dapibus erat. Vestibulum commodo turpis non ipsum finibus dignissim. Vestibulum in sapien at neque sagittis ultrices. Ut eget arcu ut est placerat feugiat. Maecenas vel finibus nibh, a pharetra libero. Nam commodo nibh non facilisis sagittis.\n\n" +
        "Proin nec accumsan mi, sed viverra lectus. Mauris consectetur maximus dui porttitor sollicitudin. Mauris at purus elit. Suspendisse tincidunt et justo eget efficitur. Quisque sed pulvinar magna. Fusce a volutpat magna, at ultricies turpis. Aenean eu purus at augue facilisis tempus nec at nulla. Nulla nunc dolor, finibus at luctus suscipit, viverra ut urna. Fusce sit amet bibendum erat, vitae pharetra quam. Etiam quis lacus nec dolor cursus semper vitae in diam. Aliquam fermentum massa vel lobortis suscipit. Curabitur semper eros arcu.";

    private static readonly IReadOnlyList<CartographyDomainSectionTemplate> DomainSectionTemplates =
    [
        new("APPLICATION_ARCHITECTURE", "Architecture Applicative", 1, 1000),
        new("MAIN_ARTICULATION", "Articulation principale", 2, 1100),
        new("FUNCTIONAL_DISTRIBUTION", "Répartition fonctionnelle", 2, 1200),
        new("OTHER_SYSTEMS", "Autres systèmes", 2, 1300),
        new("GENERAL_CARTOGRAPHY", "Cartographie générale", 2, 1400),
        new("MAIN_FLOWS_CARTOGRAPHY", "Cartographie des principaux flux", 2, 1500),
        new("TECHNICAL_ARCHITECTURE", "Architecture Technique", 1, 2000),
        new("APPLICATION_CATEGORIZATION", "Catégorisation des applications", 2, 2100),
        new("ON_PREMISE_SOLUTIONS", "Solutions déployées « On Premise »", 2, 2200),
        new("CLOUD_SOLUTIONS", "Solutions déployées « On The Cloud »", 2, 2300),
        new("OS_VIRTUALIZATION_DISTRIBUTION", "Répartition des applications sur les différents OS / virtualisation", 2, 2400),
        new("VIRTUAL_SERVER_NATURE", "Nature des serveurs virtuels hébergeant les applications", 2, 2500),
        new("OPERATING_SYSTEMS", "Systèmes d’exploitation / Versions", 2, 2600),
        new("DATABASES", "Bases de données", 2, 2700),
    ];

    private static readonly IReadOnlyList<CartographyDomainSectionTemplate> GeneralDomainSectionTemplates =
    [
        new("GENERAL_01_PRESENTATION", "01 — Présentation générale", 1, 1000,
            "<p>Point d'entrée documentaire de la cartographie du SI ESF. Cette vision chapeau organise la lecture du système d'information autour des trois SI métiers, du Digital ESF et des capacités transverses.</p>"),
        new("GENERAL_01_01_SYNTHESIS", "01.01 — Synthèse exécutive", 2, 1010,
            "<p>Le SI ESF est lu progressivement depuis une vision générale vers les cartographies métier détaillées. La cartographie générale n'est pas un quatrième SI métier : elle porte la cohérence transverse entre Épargne, Banque et Asset Management.</p>"),
        new("GENERAL_01_02_SCOPE", "01.02 — Périmètre de la cartographie", 2, 1020,
            "<p>Périmètre : SI Épargne, SI Banque, SI Asset Management, Digital ESF et capacités transverses associées.</p>"),
        new("GENERAL_01_03_SOURCES", "01.03 — Sources utilisées", 2, 1030,
            "<ul><li>Cartographie applicative du DAS Épargne et Services Financiers — juin/juillet 2026.</li><li>Cartographie générale du SI Banque V0.</li><li>Architecture applicative ESF t0.5.</li><li>Modèle ArchiMate SI ESF V1.</li><li>Référentiel CMDB+.</li></ul>"),
        new("GENERAL_01_04_STATUS", "01.04 — Statut du référentiel", 2, 1040,
            "<p>Version 0.1, juillet 2026. Statut : document chapeau en draft, à consolider avec les Product Owners, la CMDB et les instances d'urbanisation.</p>"),
        new("GENERAL_01_05_TRACEABILITY", "01.05 — Traçabilité du document", 2, 1050,
            "<p>Origine : Patrick BENHAMOU. Création : 22/07/2026. Dernière mise à jour : 22/07/2026. Validation cible : CODIR DSI ESF. Classification : usage interne.</p>"),

        new("GENERAL_02_TOP_DOWN", "02 — Lecture Top-Down du SI", 1, 2000,
            "<p>La cartographie générale propose une lecture en quatre niveaux : identité des SI métiers, articulation Front-to-Back, outils structurants puis fonctions couvertes.</p>"),
        new("GENERAL_02_LEVEL_1", "Niveau 1 — De quels SI métiers parle-t-on ?", 2, 2010,
            "<p>Identification des trois piliers métier : SI Épargne, SI Banque et SI Asset Management, complétés par Digital ESF et les capacités transverses.</p>"),
        new("GENERAL_02_LEVEL_2", "Niveau 2 — Comment s'articule l'ensemble ?", 2, 2020,
            "<p>Vision d'ensemble Front-to-Back : acteurs, front, intégration, data, back-offices métier, échanges et socles Groupe.</p>"),
        new("GENERAL_02_LEVEL_3", "Niveau 3 — Quels sont les outils structurants ?", 2, 2030,
            "<p>Zoom hélicoptère sur les applications majeures qui structurent chaque SI métier.</p>"),
        new("GENERAL_02_LEVEL_4", "Niveau 4 — Quelles fonctions sont couvertes ?", 2, 2040,
            "<p>Cartographie fonctionnelle détaillée par domaine fonctionnel, application structurante et finalité.</p>"),

        new("GENERAL_03_LEVEL_1", "03 — Niveau 1 — Vue des SI métiers", 1, 3000,
            "<p>Le SI ESF combine trois systèmes métier, une expérience digitale transverse et des capacités mutualisées.</p><ul><li>SI Épargne : distribution et gestion de l'épargne financière, assurance vie, retraite et épargne salariale.</li><li>SI Banque : comptes espèces et titres, opérations bancaires, paiements, échanges et reporting réglementaire.</li><li>SI Asset Management : gestion d'actifs, ordres, valorisation, données de marché, risques, ESG, conformité et reporting investisseurs.</li><li>Digital ESF : couche front transverse.</li><li>Capacités transverses : intégration, data, GED, éditique, finance, conformité et socles Groupe.</li></ul>"),

        new("GENERAL_04_LEVEL_2", "04 — Niveau 2 — Vision Front-to-Back", 1, 4000,
            "<p>La vision Front-to-Back place les utilisateurs et partenaires à l'entrée de la chaîne de valeur, puis Digital ESF, les services d'intégration et d'exposition, les capacités data/documentaires et les back-offices métier.</p>"),
        new("GENERAL_04_01_ACTORS", "04.01 — Acteurs", 2, 4010,
            "<p>Prospects, clients, CGP, entreprises, conseillers, gestionnaires, partenaires et acteurs Groupe.</p>"),
        new("GENERAL_04_02_FRONT", "04.02 — Front & relation client", 2, 4020,
            "<p>CRM, marketing, espaces digitaux, relation conseiller, réclamations et animation des réseaux.</p>"),
        new("GENERAL_04_03_DIGITAL_ESF", "04.03 — Digital ESF", 2, 4030,
            "<p>Couche front transverse portant les parcours, services digitaux, souscription, connaissance client et interactions avec les back-offices.</p>"),
        new("GENERAL_04_04_INTEGRATION", "04.04 — Intégration & exposition", 2, 4040,
            "<p>API Front / REST, API Gateway, MuleSoft, ODI, hubs métier et exposition contrôlée des services.</p>"),
        new("GENERAL_04_05_DATA", "04.05 — Data & pilotage", 2, 4050,
            "<p>DWH par métier, Jupyter, reporting, référentiels, consolidation et pilotage transverse.</p>"),
        new("GENERAL_04_06_SAVINGS_BACK", "04.06 — Back-office Épargne", 2, 4060,
            "<p>Gestion des contrats, comptabilité, POT, technique, PENELOP, historique GRESHAM et fonctions documentaires associées.</p>"),
        new("GENERAL_04_07_BANK_BACK", "04.07 — Back-office Banque", 2, 4070,
            "<p>Cash, comptes espèces, titres, mandats, conformité, échanges interbancaires, documents et data.</p>"),
        new("GENERAL_04_08_AM_BACK", "04.08 — Back-office Asset Management", 2, 4080,
            "<p>Portefeuilles, ordres, marchés, valorisation, risques, ESG, conformité et reporting investisseurs.</p>"),
        new("GENERAL_04_09_EXCHANGES", "04.09 — Échanges transverses & support", 2, 4090,
            "<p>Flux partenaires, échanges bancaires, GED, éditique, archivage, support et circulation des données.</p>"),
        new("GENERAL_04_10_FOUNDATIONS", "04.10 — Socles & écosystème", 2, 4100,
            "<p>Socles Groupe, hébergement, cybersécurité, environnement de travail, EDI, Data & IA.</p>"),
        new("GENERAL_04_CHAINS", "Chaînes fonctionnelles de référence", 2, 4110,
            "<ul><li>Entrer en relation.</li><li>Connaître et servir.</li><li>Distribuer et souscrire.</li><li>Gérer.</li><li>Piloter et échanger.</li></ul>"),

        new("GENERAL_05_LEVEL_3", "05 — Niveau 3 — Outils structurants", 1, 5000,
            "<p>Vision hélicoptère des applications qui structurent les trois métiers.</p>"),
        new("GENERAL_05_01_SAVINGS", "05.01 — SI Épargne", 2, 5010,
            "<ul><li>Digital Plateforme ESF / COSY : parcours et services digitaux.</li><li>SUNSHINE : usine de gestion des contrats d'épargne et retraite.</li><li>SI GRESHAM : fonctions historiques en trajectoire de décommissionnement.</li><li>Hubs, GED et éditique : alimentation, documents et échanges.</li></ul>"),
        new("GENERAL_05_02_BANK", "05.02 — SI Banque", 2, 5020,
            "<ul><li>Olympic Banking : cash, comptes espèces et traitements bancaires.</li><li>Boréal : comptes-titres.</li><li>SOLIAM : mandats et unités de compte.</li><li>Hub Banque, E-Banks et SWIFT : échanges bancaires.</li></ul>"),
        new("GENERAL_05_03_AM", "05.03 — SI Asset Management", 2, 5030,
            "<ul><li>SOLIAM et Jump : gestion d'actifs et portefeuilles.</li><li>Bloomberg, Must, DMA, Jupyter, DWH AM et OLIS : marché, données, valorisation et pilotage.</li><li>APIRisk, ESG Connect, BELT, AFTERDATA et Cognitive Credit : risques, ESG et conformité.</li></ul>"),

        new("GENERAL_06_LEVEL_4", "06 — Niveau 4 — Cartographie fonctionnelle détaillée", 1, 6000,
            "<p>Représentation fonctionnelle fine par domaine, avec applications structurantes et finalité fonctionnelle.</p>"),
        new("GENERAL_06_01_SAVINGS", "06.01 — SI Épargne", 2, 6010,
            "<ul><li>Relation & distribution : MyForce, Marketing Cloud, My GRESHAM, ATLAS.</li><li>Parcours & souscription : Digital Plateforme ESF / COSY, NETHEOS, DOCAPOST.</li><li>Gestion des contrats : SUNSHINE Gestion / Comptabilité / POT / Technique / PENELOP.</li><li>Gestion historique : SI GRESHAM, Base rétrocessions AS400, Comptarel.</li><li>Documents : GED EverSuite, Editique Epargne, SOLED, Everial, Clic'Doc.</li><li>Data & pilotage : DWH Epargne, Hub GRESHAM, Hub Intencial, Reporting Data.</li></ul>"),
        new("GENERAL_06_02_BANK", "06.02 — SI Banque", 2, 6020,
            "<ul><li>Cash & comptes espèces : Olympic Banking, Paiement Groupe, BBFI, OIC.</li><li>Titres : Boréal, Base Fonds, SOFI, OMEGA FA.</li><li>Mandats / UC : SOLIAM, COBRA, Module Tréso.</li><li>Reporting & conformité : Evolan Report, Capital Compliance, ETAFI, Crédit Expert.</li><li>Échanges : Hub Banque, E-Banks, SWIFT Alliance Lite2, SWIFT Manager.</li><li>Documents & data : GED EverSuite, SOLED, DWH Banque.</li></ul>"),
        new("GENERAL_06_03_AM", "06.03 — SI Asset Management", 2, 6030,
            "<ul><li>Gestion d'actifs : SOLIAM, Jump, Bloomberg AIM.</li><li>Marchés & exécution : Bloomberg, OLIS / CACEIS, plateformes d'exécution.</li><li>Data & valorisation : DMA AM, Must, Jupyter, DWH AM.</li><li>Risques : APIRisk et outils risques OPC / mandats / GSM / PMS.</li><li>ESG : ESG Connect, MSCI, EthiFinance, Iceberg Data.</li><li>Conformité : BELT, AFTERDATA, Cognitive Credit.</li></ul>"),
        new("GENERAL_06_04_TRANSVERSE", "06.04 — Capacités transverses", 2, 6040,
            "<p>Capacités communes mutualisées autour des trois SI métiers : front, intégration, data, GED, finance, conformité, échanges, tiers et socles Groupe.</p>"),

        new("GENERAL_07_TRANSVERSE", "07 — Capacités transverses", 1, 7000,
            "<ul><li>Front & CRM.</li><li>Intégration & API.</li><li>Data & pilotage.</li><li>GED, archivage & éditique.</li><li>Finance & trésorerie.</li><li>Conformité & contrôle.</li><li>Échanges & tiers.</li><li>Socles Groupe.</li></ul>"),

        new("GENERAL_08_GOVERNANCE", "08 — Gouvernance de la cartographie", 1, 8000,
            "<ul><li>Identifiant unique d'application.</li><li>Propriétaire fonctionnel et technique.</li><li>Date d'arrêté, périmètre et source.</li><li>Cycle de vie normalisé.</li><li>Gestion séparée des flux.</li><li>Rapprochement CMDB / ArchiMate / inventaires.</li><li>Validation Product Owner, responsable de domaine, Urbanisation / Architecture et instance de gouvernance.</li></ul>"),

        new("GENERAL_09_QUALITY", "09 — Qualité et maintien du référentiel", 1, 9000,
            "<p>Trajectoire de consolidation du référentiel.</p>"),
        new("GENERAL_09_STEPS", "Maintien en qualité", 2, 9010,
            "<ol><li>Validation de la nomenclature.</li><li>Réconciliation des noms, identifiants CMDB et statuts.</li><li>Affectation des applications aux fonctions.</li><li>Complétude des flux et référentiels.</li><li>Publication et revue périodique.</li></ol>"),

        new("GENERAL_10_APPLICATIONS", "10 — Référentiel des applications", 1, 10000,
            "<p>La liste applicative du document source doit être rapprochée du référentiel CMDB existant. La vue cible est filtrable par nom, domaine, sous-domaine, description, ID CMDB, statut, Product Owner et propriétaire technique, avec ouverture de la fiche CI lorsque l'application existe.</p>"),

        new("GENERAL_11_APPLICATION_CARD", "11 — Modèle de fiche applicative", 1, 11000,
            "<ul><li>Identité : nom, alias, ID CMDB, statut.</li><li>Positionnement : métier, domaine, fonctions et utilisateurs.</li><li>Description : finalité, fonctionnalités, processus et volumes.</li><li>Données & flux : données maîtres, entrées/sorties, partenaires et modalités d'échange.</li><li>Responsabilités : Product Owner, propriétaire technique, entité légale et éditeur.</li><li>Technique : nature, hébergement, technologies, environnements et dépendances.</li><li>Risques : criticité, obsolescence, sécurité, continuité et conformité.</li><li>Trajectoire : cible, projets, décisions, échéances et décommissionnement.</li></ul>"),

        new("GENERAL_12_TO_QUALIFY", "12 — Applications à qualifier", 1, 12000,
            "<p>Vue qualité à construire depuis le référentiel applicatif : domaine non déterminé, description absente, ID CMDB absent, propriétaire absent, fonction non affectée ou statut non renseigné.</p>"),

        new("GENERAL_13_REFERENCES", "13 — Références documentaires", 1, 13000,
            "<p>Sources et références : documents Word, Excel, PowerPoint, PDF, CMDB, modèles ArchiMate, cartographies et documents de travail. Métadonnées attendues : titre, type, version, date, auteur, source, description, pièce jointe et URL éventuelle.</p>"),
    ];

    private readonly ApplicationDBContext _db;
    private readonly ICartographyDocumentService _documentService;

    public CartographyDocumentsController(
        ApplicationDBContext db,
        ICartographyDocumentService documentService)
    {
        _db = db;
        _documentService = documentService;
    }

    [HttpGet("entity-document")]
    public async Task<IActionResult> GenerateEntityDocument(
        [FromQuery] string employerEntity,
        [FromQuery] bool includeDomainSections = true,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentService.GenerateAsync(
            employerEntity,
            includeDomainSections,
            cancellationToken);
        if (document is null)
        {
            return NotFound(
                "Aucune application métier active n'est rattachée à cette entité.");
        }

        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpGet("entity-document/pdf")]
    public async Task<IActionResult> GenerateEntityDocumentPdf(
        [FromQuery] string employerEntity,
        [FromQuery] bool includeDomainSections = true,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentService.GeneratePdfAsync(
            employerEntity,
            includeDomainSections,
            cancellationToken);
        if (document is null)
        {
            return NotFound(
                "Aucun CI actif n'est rattaché à cette entité.");
        }

        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpGet("domain-documents/{employerEntity}")]
    public async Task<ActionResult<CartographyDomainDocumentDto>> GetDomainDocument(
        string employerEntity,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmployerEntity = employerEntity.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmployerEntity))
        {
            return BadRequest("Le domaine employeur CMDB est obligatoire.");
        }

        if (!IsGeneralDomain(normalizedEmployerEntity) &&
            !await DomainExistsAsync(normalizedEmployerEntity, cancellationToken))
        {
            return NotFound("Aucun CI actif n'est rattaché à ce domaine.");
        }

        var document = await EnsureDomainDocumentAsync(
            normalizedEmployerEntity,
            cancellationToken);

        return Ok(ToDto(document));
    }

    [HttpPut("domain-documents/sections/{sectionId:int}")]
    public async Task<ActionResult<CartographyDomainDocumentSectionDto>> UpdateDomainDocumentSection(
        int sectionId,
        UpdateCartographyDomainDocumentSectionDto dto,
        CancellationToken cancellationToken = default)
    {
        var section = await _db.CartographyDomainDocumentSections
            .Include(x => x.CartographyDomainDocument)
            .FirstOrDefaultAsync(x => x.Id == sectionId, cancellationToken);
        if (section is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            section.Title = dto.Title.Trim();
        }

        if (dto.HeadingLevel is not null)
        {
            section.HeadingLevel = NormalizeHeadingLevel(dto.HeadingLevel.Value);
        }

        section.ContentHtml = string.IsNullOrWhiteSpace(dto.ContentHtml)
            ? null
            : dto.ContentHtml;
        section.PlainText = string.IsNullOrWhiteSpace(dto.PlainText)
            ? null
            : dto.PlainText;
        section.EditorJson = string.IsNullOrWhiteSpace(dto.EditorJson)
            ? null
            : dto.EditorJson;
        section.UpdatedDate = DateTime.UtcNow;
        section.CartographyDomainDocument.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(section));
    }

    [HttpPost("domain-documents/{employerEntity}/sections")]
    public async Task<ActionResult<CartographyDomainDocumentSectionDto>> CreateDomainDocumentSection(
        string employerEntity,
        CreateCartographyDomainDocumentSectionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest("Le titre de la rubrique est obligatoire.");
        }

        var document = await EnsureDomainDocumentAsync(employerEntity.Trim(), cancellationToken);
        var orderedSections = document.Sections
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToList();
        var anchorSectionId = dto.AnchorSectionId ?? dto.AfterSectionId;
        var anchorIndex = anchorSectionId is null
            ? orderedSections.Count - 1
            : orderedSections.FindIndex(x => x.Id == anchorSectionId.Value);
        var insertBefore = string.Equals(
            dto.InsertPosition,
            "Before",
            StringComparison.OrdinalIgnoreCase);
        var sortOrder = CalculateInsertedSortOrder(
            orderedSections,
            anchorIndex,
            insertBefore);

        var section = new CartographyDomainDocumentSection
        {
            SectionKey = $"CUSTOM_{Guid.NewGuid():N}".ToUpperInvariant(),
            Title = dto.Title.Trim(),
            HeadingLevel = NormalizeHeadingLevel(dto.HeadingLevel),
            SortOrder = sortOrder,
            ContentHtml = string.IsNullOrWhiteSpace(dto.ContentHtml)
                ? "<p></p>"
                : dto.ContentHtml,
            PlainText = string.IsNullOrWhiteSpace(dto.PlainText)
                ? null
                : dto.PlainText,
            EditorJson = string.IsNullOrWhiteSpace(dto.EditorJson)
                ? null
                : dto.EditorJson,
            IsSystem = false,
        };

        document.Sections.Add(section);
        document.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(section));
    }

    private async Task<CartographyDomainDocument> EnsureDomainDocumentAsync(
        string employerEntity,
        CancellationToken cancellationToken)
    {
        var document = await _db.CartographyDomainDocuments
            .Include(x => x.Sections)
            .FirstOrDefaultAsync(x => x.EmployerEntity == employerEntity, cancellationToken);

        if (document is null)
        {
            document = new CartographyDomainDocument
            {
                EmployerEntity = employerEntity,
                Title = IsGeneralDomain(employerEntity)
                    ? GeneralDomainTitle
                    : $"Cartographie du SI - {employerEntity}",
            };
            _db.CartographyDomainDocuments.Add(document);
        }

        var templates = IsGeneralDomain(employerEntity)
            ? GeneralDomainSectionTemplates
            : DomainSectionTemplates;
        foreach (var template in templates)
        {
            if (document.Sections.Any(x => x.SectionKey == template.SectionKey))
            {
                continue;
            }

            document.Sections.Add(new CartographyDomainDocumentSection
            {
                SectionKey = template.SectionKey,
                Title = template.Title,
                HeadingLevel = template.HeadingLevel,
                SortOrder = template.SortOrder,
                ContentHtml = template.ContentHtml ?? DefaultDomainSectionContentHtml,
                PlainText = ToPlainText(template.ContentHtml) ?? DefaultDomainSectionPlainText,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        document.Sections = document.Sections
            .OrderBy(x => x.SortOrder)
            .ToList();
        return document;
    }

    private static CartographyDomainDocumentDto ToDto(CartographyDomainDocument document) =>
        new()
        {
            Id = document.Id,
            EmployerEntity = document.EmployerEntity,
            Title = document.Title,
            CreatedDate = document.CreatedDate,
            UpdatedDate = document.UpdatedDate,
            Sections = document.Sections
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .Select(ToDto)
                .ToList(),
        };

    private static CartographyDomainDocumentSectionDto ToDto(CartographyDomainDocumentSection section) =>
        new()
        {
            Id = section.Id,
            CartographyDomainDocumentId = section.CartographyDomainDocumentId,
            SectionKey = section.SectionKey,
            Title = section.Title,
            HeadingLevel = section.HeadingLevel,
            SortOrder = section.SortOrder,
            ContentHtml = section.ContentHtml,
            PlainText = section.PlainText,
            EditorJson = section.EditorJson,
            IsSystem = section.IsSystem,
            CreatedDate = section.CreatedDate,
            UpdatedDate = section.UpdatedDate,
        };

    private static int NormalizeHeadingLevel(int headingLevel) =>
        Math.Clamp(headingLevel, 1, 3);

    private async Task<bool> DomainExistsAsync(
        string employerEntity,
        CancellationToken cancellationToken)
    {
        var domainCandidates = await _db.ConfigurationItems
            .AsNoTracking()
            .Where(x => x.IsCurrent &&
                !x.IsPlaceholder &&
                ((x.ResponsibleEmployer != null && x.ResponsibleEmployer != "") ||
                 (x.EntityPath != null && x.EntityPath != "")))
            .Select(x => new
            {
                x.EntityPath,
                x.ResponsibleEmployer,
            })
            .ToListAsync(cancellationToken);

        return domainCandidates.Any(x => string.Equals(
            CmdbEmployerResolver.Resolve(x.EntityPath, x.ResponsibleEmployer),
            employerEntity,
            StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsGeneralDomain(string employerEntity) =>
        string.Equals(employerEntity, GeneralDomainCode, StringComparison.OrdinalIgnoreCase);

    private static string? ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        return WebUtility.HtmlDecode(
            System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " "))
            .Replace("  ", " ")
            .Trim();
    }

    private static int CalculateInsertedSortOrder(
        List<CartographyDomainDocumentSection> orderedSections,
        int anchorIndex,
        bool insertBefore)
    {
        if (orderedSections.Count == 0)
        {
            return 1000;
        }

        if (anchorIndex < 0)
        {
            anchorIndex = orderedSections.Count - 1;
        }

        var previousIndex = insertBefore ? anchorIndex - 1 : anchorIndex;
        var nextIndex = insertBefore ? anchorIndex : anchorIndex + 1;

        if (previousIndex < 0)
        {
            return orderedSections[0].SortOrder - 100;
        }

        if (nextIndex >= orderedSections.Count)
        {
            return orderedSections[^1].SortOrder + 100;
        }

        var previous = orderedSections[previousIndex].SortOrder;
        var next = orderedSections[nextIndex].SortOrder;
        if (next - previous > 1)
        {
            return previous + ((next - previous) / 2);
        }

        for (var index = 0; index < orderedSections.Count; index++)
        {
            orderedSections[index].SortOrder = (index + 1) * 1000;
        }

        previous = orderedSections[previousIndex].SortOrder;
        next = orderedSections[nextIndex].SortOrder;
        return previous + ((next - previous) / 2);
    }

    private sealed record CartographyDomainSectionTemplate(
        string SectionKey,
        string Title,
        int HeadingLevel,
        int SortOrder,
        string? ContentHtml = null);
}
