using api.Data;
using api.Dtos.Cmdb;
using api.Models.Cmdb;
using api.Services.Cmdb;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/cartography")]
public sealed class CartographyDocumentsController : ControllerBase
{
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

        var domainExists = domainCandidates.Any(x => string.Equals(
            CmdbEmployerResolver.Resolve(x.EntityPath, x.ResponsibleEmployer),
            normalizedEmployerEntity,
            StringComparison.OrdinalIgnoreCase));
        if (!domainExists)
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
                Title = $"Cartographie du SI - {employerEntity}",
            };
            _db.CartographyDomainDocuments.Add(document);
        }

        foreach (var template in DomainSectionTemplates)
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
                ContentHtml = DefaultDomainSectionContentHtml,
                PlainText = DefaultDomainSectionPlainText,
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
        int SortOrder);
}
