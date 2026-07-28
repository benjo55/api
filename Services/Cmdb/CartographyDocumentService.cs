using api.Data;
using api.Models.Cmdb;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace api.Services.Cmdb;

public sealed record CartographyDocumentResult(
    byte[] Content,
    string FileName,
    string ContentType);

public interface ICartographyDocumentService
{
    Task<CartographyDocumentResult?> GenerateAsync(
        string employerEntity,
        bool includeDomainSections = true,
        CancellationToken cancellationToken = default);

    Task<CartographyDocumentResult?> GeneratePdfAsync(
        string employerEntity,
        bool includeDomainSections = true,
        CancellationToken cancellationToken = default);
}

public sealed class CartographyDocumentService : ICartographyDocumentService
{
    private const string WordContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string PdfContentType = "application/pdf";
    private const string Red = "C00000";
    private const string Blue = "009DD2";
    private const string Dark = "1F1F1F";
    private const string Gray = "6B7280";
    private const string FontName = "Century Gothic";
    private readonly ApplicationDBContext _db;

    public CartographyDocumentService(ApplicationDBContext db) => _db = db;

    public async Task<CartographyDocumentResult?> GenerateAsync(
        string employerEntity,
        bool includeDomainSections = true,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadDocumentContextAsync(
            employerEntity,
            includeDomainSections,
            cancellationToken);
        if (context is null)
        {
            return null;
        }

        var bytes = BuildDocument(
            context.EmployerEntity,
            context.ConfigurationItems,
            context.Applications,
            context.Flows,
            context.Relationships,
            context.DomainSections);
        var safeEntity = SanitizeFileName(context.EmployerEntity);
        return new CartographyDocumentResult(
            bytes,
            $"Cartographie_SI_{safeEntity}_{DateTime.Now:yyyyMMdd}.docx",
            WordContentType);
    }

    public async Task<CartographyDocumentResult?> GeneratePdfAsync(
        string employerEntity,
        bool includeDomainSections = true,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadDocumentContextAsync(
            employerEntity,
            includeDomainSections,
            cancellationToken);
        if (context is null)
        {
            return null;
        }

        var bytes = BuildPdfDocument(context);
        var safeEntity = SanitizeFileName(context.EmployerEntity);
        return new CartographyDocumentResult(
            bytes,
            $"Cartographie_SI_{safeEntity}_{DateTime.Now:yyyyMMdd}.pdf",
            PdfContentType);
    }

    private async Task<CartographyDocumentContext?> LoadDocumentContextAsync(
        string employerEntity,
        bool includeDomainSections,
        CancellationToken cancellationToken)
    {
        employerEntity = employerEntity.Trim();
        if (string.IsNullOrWhiteSpace(employerEntity))
        {
            return null;
        }

        var configurationItems = (await _db.ConfigurationItems.AsNoTracking()
            .Include(x => x.ApplicationProfile)
            .Where(x => x.IsCurrent && !x.IsPlaceholder)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken))
            .Where(x => string.Equals(
                CmdbEmployerResolver.Resolve(
                    x.EntityPath,
                    x.ResponsibleEmployer),
                employerEntity,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (configurationItems.Count == 0)
        {
            return null;
        }

        var applications = configurationItems
            .Where(x => string.Equals(
                x.Category,
                "Application Métier",
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Name)
            .ToList();

        var applicationIds = applications.Select(x => x.Id).ToHashSet();
        var flows = applicationIds.Count == 0
            ? new List<ApplicationFlow>()
            : (await _db.IntegrationFlows.AsNoTracking()
                .Where(x => x.Status != "Retired" &&
                    (applicationIds.Contains(x.SourceCiId) ||
                     applicationIds.Contains(x.TargetCiId)))
                .Select(x => new ApplicationFlow(
                    x.SourceCiId,
                    x.TargetCiId,
                    x.SourceCi.Name,
                    x.TargetCi.Name,
                    x.Name,
                    x.ExchangePattern.Name,
                    x.ExchangePattern.InteractionMode,
                    x.Technology != null ? x.Technology.Name : null))
                .ToListAsync(cancellationToken))
                .OrderBy(x => x.SourceName)
                .ThenBy(x => x.TargetName)
                .ThenBy(x => x.Name)
                .ToList();

        var relationships = applicationIds.Count == 0
            ? new List<ApplicationRelationship>()
            : (await _db.CmdbRelationships.AsNoTracking()
                .Where(x => x.IsCurrent &&
                    (applicationIds.Contains(x.SourceCiId) ||
                     applicationIds.Contains(x.TargetCiId)))
                .Select(x => new ApplicationRelationship(
                    x.SourceCiId,
                    x.TargetCiId,
                    x.SourceCi.Name,
                    x.TargetCi.Name,
                    x.SourceCi.ExternalCiNumber,
                    x.TargetCi.ExternalCiNumber,
                    x.SourceCi.Model,
                    x.TargetCi.Model,
                    x.SourceCi.Category,
                    x.TargetCi.Category,
                    x.RelationshipType.Name))
                .ToListAsync(cancellationToken))
                .OrderBy(x => x.SourceName)
                .ThenBy(x => x.TargetName)
                .ToList();

        var domainSections = includeDomainSections
            ? await _db.CartographyDomainDocuments
                .AsNoTracking()
                .Where(x => x.EmployerEntity == employerEntity)
                .SelectMany(x => x.Sections)
                .OrderBy(x => x.SortOrder)
                .Select(x => new DomainDocumentSection(
                    x.Title,
                    x.HeadingLevel,
                    x.SortOrder,
                    x.ContentHtml,
                    x.PlainText))
                .ToListAsync(cancellationToken)
            : [];

        return new CartographyDocumentContext(
            employerEntity,
            configurationItems,
            applications,
            flows,
            relationships,
            domainSections);
    }

    private static byte[] BuildDocument(
        string employerEntity,
        IReadOnlyList<ConfigurationItem> configurationItems,
        IReadOnlyList<ConfigurationItem> applications,
        IReadOnlyList<ApplicationFlow> flows,
        IReadOnlyList<ApplicationRelationship> relationships,
        IReadOnlyList<DomainDocumentSection> domainSections)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());
            AddStyles(mainPart);
            AddSettings(mainPart);
            var headerId = AddHeader(mainPart);
            var footerId = AddFooter(mainPart, employerEntity);

            var body = mainPart.Document.Body!;
            if (domainSections.Count > 0)
            {
                AddDomainSections(mainPart, body, employerEntity, domainSections, includeTableOfContents: true);
                body.Append(CreatePageBreak());
            }

            AddConfigurationItemsInventory(body, configurationItems);
            body.Append(CreatePageBreak());

            AddAnnexHeading(body, employerEntity, applications.Count);

            for (var index = 0; index < applications.Count; index++)
            {
                if (index > 0)
                {
                    body.Append(CreatePageBreak());
                }

                var application = applications[index];
                var profile = application.ApplicationProfile;
                var applicationFlows = flows
                    .Where(x => x.SourceCiId == application.Id ||
                                x.TargetCiId == application.Id)
                    .ToList();
                var applicationRelationships = relationships
                    .Where(x => x.SourceCiId == application.Id ||
                                x.TargetCiId == application.Id)
                    .ToList();

                AddApplicationHeading(body, application.Name);
                AddSection(body, "ID interne (= n° de CI)",
                    application.ExternalCiNumber);
                AddSection(body, "Domaine fonctionnel / Support",
                    application.ApplicationDomain);
                AddSection(body, "Description succincte",
                    profile?.ShortDescription ?? application.Label);
                AddSection(body, "Description détaillée",
                    profile?.DetailedDescription);

                AddFunctionalFlows(
                    body,
                    profile?.MainFunctionalProcesses,
                    applicationFlows);
                AddTechnicalFramework(
                    body,
                    profile?.GeneralTechnicalFramework,
                    application.Id,
                    applicationRelationships);
                AddArchitecture(
                    body,
                    profile?.OverallArchitecture,
                    application.Id,
                    applicationRelationships);

                AddSection(body, "Nature de l'application",
                    ApplicationNatureLabel(profile?.ApplicationNature));
                AddSection(body, "Criticité",
                    CriticalityLabel(profile?.ApplicationCriticality));
                AddSection(body, "Propriétaire de l'application",
                    application.OwnerName);
                AddSection(body, "Entité légale propriétaire",
                    profile?.LegalOwnerEntity);
                AddSection(body, "Code source disponible (O/N)",
                    YesNo(profile?.SourceCodeAvailable));
                AddSection(body, "Mode d'hébergement",
                    HostingLabel(profile));
            }

            body.Append(new SectionProperties(
                new HeaderReference
                {
                    Type = HeaderFooterValues.Default,
                    Id = headerId,
                },
                new FooterReference
                {
                    Type = HeaderFooterValues.Default,
                    Id = footerId,
                },
                new DocumentFormat.OpenXml.Wordprocessing.PageSize
                {
                    Width = 11906U,
                    Height = 16838U,
                },
                new PageMargin
                {
                    Top = 900,
                    Right = 1134U,
                    Bottom = 900,
                    Left = 1134U,
                    Header = 360U,
                    Footer = 360U,
                    Gutter = 0U,
                }));

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] BuildPdfDocument(CartographyDocumentContext context)
    {
        return QuestPDF.Fluent.Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(style => style
                    .FontFamily(FontName)
                    .FontSize(9)
                    .FontColor($"#{Dark}"));

                page.Header().Column(header =>
                {
                    header.Item().Text($"Cartographie du SI - {context.EmployerEntity}")
                        .Bold()
                        .FontSize(18)
                        .FontColor($"#{Red}");
                    header.Item().Text("Architecture, rubriques du domaine et inventaire des CI")
                        .FontSize(10)
                        .FontColor($"#{Gray}");
                    header.Item().PaddingTop(6).LineHorizontal(1).LineColor("#E5E7EB");
                });

                page.Content().PaddingVertical(16).Column(column =>
                {
                    if (context.DomainSections.Count > 0)
                    {
                        foreach (var section in context.DomainSections.OrderBy(x => x.SortOrder))
                        {
                            column.Item()
                                .PaddingBottom(10)
                                .Element(container => ComposePdfDomainSection(container, section));
                        }
                    }

                    column.Item()
                        .PaddingTop(context.DomainSections.Count > 0 ? 8 : 0)
                        .Element(container => ComposePdfConfigurationItems(container, context.ConfigurationItems));
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void ComposePdfDomainSection(
        IContainer container,
        DomainDocumentSection section)
    {
        var headingLevel = Math.Clamp(section.HeadingLevel, 1, 3);
        var fontSize = headingLevel switch
        {
            1 => 14,
            2 => 12,
            _ => 10,
        };
        var color = headingLevel switch
        {
            1 => $"#{Red}",
            2 => $"#{Blue}",
            _ => $"#{Dark}",
        };

        container.Column(column =>
        {
            column.Item().Text(section.Title)
                .Bold()
                .FontSize(fontSize)
                .FontColor(color);

            var content = HtmlToPdfText(section.ContentHtml, section.PlainText);
            column.Item()
                .PaddingTop(4)
                .Text(string.IsNullOrWhiteSpace(content) ? "À compléter" : content)
                .FontSize(9)
                .LineHeight(1.25f)
                .FontColor(string.IsNullOrWhiteSpace(content) ? $"#{Gray}" : $"#{Dark}");
        });
    }

    private static void ComposePdfConfigurationItems(
        IContainer container,
        IReadOnlyList<ConfigurationItem> configurationItems)
    {
        container.Column(column =>
        {
            column.Item().Text("CI du domaine")
                .Bold()
                .FontSize(14)
                .FontColor($"#{Red}");
            column.Item().PaddingBottom(8).Text(
                    $"{configurationItems.Count} CI actif(s) rattaché(s) au domaine.")
                .FontSize(9)
                .FontColor($"#{Gray}");

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(2.4f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(1f);
                });

                table.Header(header =>
                {
                    foreach (var title in new[]
                    {
                        "N° CI",
                        "Nom",
                        "Catégorie",
                        "Modèle",
                        "Responsable",
                        "Statut",
                    })
                    {
                        header.Cell()
                            .Background("#EAF4FF")
                            .BorderBottom(1)
                            .BorderColor("#CBD5E1")
                            .Padding(4)
                            .Text(title)
                            .Bold()
                            .FontSize(7);
                    }
                });

                foreach (var item in configurationItems
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Name))
                {
                    AddPdfCell(table, item.ExternalCiNumber);
                    AddPdfCell(table, item.Name);
                    AddPdfCell(table, item.Category);
                    AddPdfCell(table, item.Model);
                    AddPdfCell(table, item.OwnerName ?? item.ResponsibleEmployer);
                    AddPdfCell(table, item.Status);
                }
            });
        });
    }

    private static void AddPdfCell(TableDescriptor table, string? value)
    {
        table.Cell()
            .BorderBottom(0.5f)
            .BorderColor("#E5E7EB")
            .PaddingVertical(3)
            .PaddingHorizontal(4)
            .Text(string.IsNullOrWhiteSpace(value) ? "-" : value)
            .FontSize(6);
    }

    private static string HtmlToPdfText(string? html, string? fallbackText)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return fallbackText?.Trim() ?? string.Empty;
        }

        var prepared = html
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        prepared = Regex.Replace(
            prepared,
            "</(p|div|h1|h2|h3|li|tr)>",
            "\n",
            RegexOptions.IgnoreCase);
        prepared = Regex.Replace(
            prepared,
            "<br\\s*/?>",
            "\n",
            RegexOptions.IgnoreCase);
        prepared = Regex.Replace(
            prepared,
            "<li[^>]*>",
            "• ",
            RegexOptions.IgnoreCase);

        return string.Join(
            Environment.NewLine,
            DecodeAndStripHtml(prepared)
                .Split('\n')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static void AddDomainSections(
        MainDocumentPart mainPart,
        Body body,
        string employerEntity,
        IReadOnlyList<DomainDocumentSection> domainSections,
        bool includeTableOfContents)
    {
        body.Append(CreateParagraph(
            $"Cartographie du SI - {employerEntity}",
            20,
            Red,
            bold: true,
            alignment: JustificationValues.Center,
            spaceAfter: 180));
        body.Append(CreateParagraph(
            "Architecture et cartographie générale du domaine",
            11,
            Gray,
            alignment: JustificationValues.Center,
            spaceAfter: 360));

        if (includeTableOfContents)
        {
            AddTableOfContents(body);
            body.Append(CreatePageBreak());
        }

        foreach (var section in domainSections.OrderBy(x => x.SortOrder))
        {
            var headingLevel = Math.Clamp(section.HeadingLevel, 1, 3);
            body.Append(CreateParagraph(
                section.Title,
                headingLevel == 1 ? 16 : headingLevel == 2 ? 13 : 11,
                headingLevel == 1 ? Red : headingLevel == 2 ? Blue : Dark,
                bold: true,
                keepNext: true,
                spaceBefore: headingLevel == 1 ? 280 : headingLevel == 2 ? 180 : 120,
                spaceAfter: 90,
                styleId: $"Heading{headingLevel}"));

            AddHtmlContent(mainPart, body, section.ContentHtml, section.PlainText);
        }
    }

    private static void AddConfigurationItemsInventory(
        Body body,
        IReadOnlyList<ConfigurationItem> configurationItems)
    {
        body.Append(CreateParagraph(
            "CI du domaine",
            16,
            Red,
            bold: true,
            keepNext: true,
            spaceBefore: 160,
            spaceAfter: 140,
            styleId: "Heading1"));
        body.Append(CreateParagraph(
            $"{configurationItems.Count} CI actif(s) rattaché(s) au domaine.",
            10,
            Gray,
            spaceAfter: 120));

        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "100%", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U },
                    new LeftBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U },
                    new BottomBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U },
                    new RightBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U },
                    new InsideVerticalBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U })));

        AddInventoryRow(
            table,
            ["N° CI", "Nom", "Catégorie", "Modèle", "Responsable", "Statut"],
            isHeader: true);

        foreach (var item in configurationItems
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name))
        {
            AddInventoryRow(
                table,
                [
                    item.ExternalCiNumber,
                    item.Name,
                    item.Category ?? "-",
                    item.Model,
                    item.OwnerName ?? item.ResponsibleEmployer ?? "-",
                    item.Status ?? "-",
                ],
                isHeader: false);
        }

        body.Append(table);
    }

    private static void AddInventoryRow(
        Table table,
        IReadOnlyList<string> values,
        bool isHeader)
    {
        var row = new TableRow();
        foreach (var value in values)
        {
            row.Append(new TableCell(
                new TableCellProperties(
                    new Shading
                    {
                        Val = ShadingPatternValues.Clear,
                        Color = "auto",
                        Fill = isHeader ? "EAF4FF" : "FFFFFF",
                    },
                    new TableCellMargin(
                        new TopMargin { Width = "70", Type = TableWidthUnitValues.Dxa },
                        new StartMargin { Width = "70", Type = TableWidthUnitValues.Dxa },
                        new BottomMargin { Width = "70", Type = TableWidthUnitValues.Dxa },
                        new EndMargin { Width = "70", Type = TableWidthUnitValues.Dxa })),
                CreateParagraph(
                    value,
                    isHeader ? 7 : 6,
                    Dark,
                    bold: isHeader,
                    spaceAfter: 0)));
        }

        table.Append(row);
    }

    private static void AddTableOfContents(Body body)
    {
        body.Append(CreateParagraph(
            "Table des matières",
            16,
            Red,
            bold: true,
            keepNext: true,
            spaceBefore: 160,
            spaceAfter: 140,
            styleId: "TOCHeading"));

        var paragraph = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "160" }));
        paragraph.Append(
            new Run(new FieldChar { FieldCharType = FieldCharValues.Begin, Dirty = true }),
            new Run(new FieldCode(@" TOC \o ""1-3"" \h \z \u ")
            {
                Space = SpaceProcessingModeValues.Preserve,
            }),
            new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
            CreateRun(
                "La table des matières sera générée automatiquement à l'ouverture du document Word.",
                10,
                Gray,
                bold: false),
            new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
        body.Append(paragraph);
    }

    private static void AddHtmlContent(
        MainDocumentPart mainPart,
        Body body,
        string? html,
        string? fallbackText)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            AddTextBlock(body, fallbackText);
            return;
        }

        var normalized = html
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        var blockMatches = Regex.Matches(
            normalized,
            "<table[\\s\\S]*?</table>|<img\\b[^>]*>",
            RegexOptions.IgnoreCase);

        var cursor = 0;
        foreach (Match blockMatch in blockMatches)
        {
            AddHtmlTextFragments(body, normalized[cursor..blockMatch.Index]);
            if (blockMatch.Value.StartsWith("<table", StringComparison.OrdinalIgnoreCase))
            {
                AddHtmlTable(body, blockMatch.Value);
            }
            else
            {
                AddHtmlImage(mainPart, body, blockMatch.Value);
            }

            cursor = blockMatch.Index + blockMatch.Length;
        }

        AddHtmlTextFragments(body, normalized[cursor..]);
    }

    private static void AddHtmlTextFragments(Body body, string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return;
        }

        var prepared = Regex.Replace(
            html,
            "</(p|div|h1|h2|h3|li)>",
            "\n",
            RegexOptions.IgnoreCase);
        prepared = Regex.Replace(
            prepared,
            "<br\\s*/?>",
            "\n",
            RegexOptions.IgnoreCase);
        prepared = Regex.Replace(
            prepared,
            "<li[^>]*>",
            "• ",
            RegexOptions.IgnoreCase);
        var text = DecodeAndStripHtml(prepared);
        var lines = text
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (lines.Count == 0)
        {
            return;
        }

        foreach (var line in lines)
        {
            if (line.StartsWith('•'))
            {
                AddBullet(body, line.TrimStart('•', ' '));
            }
            else
            {
                body.Append(CreateParagraph(line, 10, Dark, spaceAfter: 45));
            }
        }
    }

    private static void AddHtmlTable(Body body, string tableHtml)
    {
        var rowMatches = Regex.Matches(
            tableHtml,
            "<tr[\\s\\S]*?</tr>",
            RegexOptions.IgnoreCase);
        if (rowMatches.Count == 0)
        {
            return;
        }

        var rows = new List<List<string>>();
        foreach (Match rowMatch in rowMatches)
        {
            var cells = Regex.Matches(
                    rowMatch.Value,
                    "<t[dh][^>]*>[\\s\\S]*?</t[dh]>",
                    RegexOptions.IgnoreCase)
                .Select(x => DecodeAndStripHtml(x.Value).Trim())
                .ToList();
            if (cells.Count > 0)
            {
                rows.Add(cells);
            }
        }

        if (rows.Count == 0)
        {
            return;
        }

        var columnCount = rows.Max(x => x.Count);
        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "100%", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U },
                    new LeftBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U },
                    new BottomBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U },
                    new RightBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U },
                    new InsideVerticalBorder { Val = BorderValues.Single, Color = "CBD5E1", Size = 4U })));

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var tableRow = new TableRow();
            for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                var value = columnIndex < rows[rowIndex].Count
                    ? rows[rowIndex][columnIndex]
                    : string.Empty;
                var cell = new TableCell(
                    new TableCellProperties(
                        new TableCellWidth
                        {
                            Width = (5000 / Math.Max(columnCount, 1)).ToString(
                                CultureInfo.InvariantCulture),
                            Type = TableWidthUnitValues.Pct,
                        },
                        new Shading
                        {
                            Val = ShadingPatternValues.Clear,
                            Color = "auto",
                            Fill = rowIndex == 0 ? "EAF4FF" : "FFFFFF",
                        },
                        new TableCellMargin(
                            new TopMargin { Width = "90", Type = TableWidthUnitValues.Dxa },
                            new StartMargin { Width = "90", Type = TableWidthUnitValues.Dxa },
                            new BottomMargin { Width = "90", Type = TableWidthUnitValues.Dxa },
                            new EndMargin { Width = "90", Type = TableWidthUnitValues.Dxa })),
                    CreateParagraph(
                        value,
                        rowIndex == 0 ? 8 : 7,
                        Dark,
                        bold: rowIndex == 0,
                        spaceAfter: 0));
                tableRow.Append(cell);
            }
            table.Append(tableRow);
        }

        body.Append(table);
        body.Append(CreateParagraph(" ", 3, Dark, spaceAfter: 80));
    }

    private static void AddHtmlImage(
        MainDocumentPart mainPart,
        Body body,
        string imageHtml)
    {
        var src = GetHtmlAttribute(imageHtml, "src");
        if (string.IsNullOrWhiteSpace(src))
        {
            return;
        }

        var image = DecodeDataUriImage(src);
        if (image is null)
        {
            body.Append(CreateParagraph(
                "[Image non intégrée : format non pris en charge]",
                9,
                Gray,
                italic: true,
                spaceAfter: 80));
            return;
        }

        var alt = GetHtmlAttribute(imageHtml, "alt");
        var dimensions = GetImageDimensions(image.Value.Content)
            ?? new ImageDimensions(900, 450);
        var (cx, cy) = FitImageToPage(dimensions);

        var imagePart = image.Value.Kind switch
        {
            "png" => mainPart.AddImagePart(ImagePartType.Png),
            "jpeg" => mainPart.AddImagePart(ImagePartType.Jpeg),
            "gif" => mainPart.AddImagePart(ImagePartType.Gif),
            "bmp" => mainPart.AddImagePart(ImagePartType.Bmp),
            _ => null,
        };
        if (imagePart is null)
        {
            return;
        }
        using (var imageStream = new MemoryStream(image.Value.Content))
        {
            imagePart.FeedData(imageStream);
        }

        var relationshipId = mainPart.GetIdOfPart(imagePart);
        var drawing = CreateImageDrawing(
            relationshipId,
            cx,
            cy,
            string.IsNullOrWhiteSpace(alt) ? "Image rubrique cartographie" : alt);

        var paragraph = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { Before = "80", After = "160" }),
            new Run(drawing));
        body.Append(paragraph);
    }

    private static string? GetHtmlAttribute(string html, string attributeName)
    {
        var match = Regex.Match(
            html,
            $@"\b{Regex.Escape(attributeName)}\s*=\s*([""'])(?<value>[\s\S]*?)\1",
            RegexOptions.IgnoreCase);
        return match.Success
            ? WebUtility.HtmlDecode(match.Groups["value"].Value)
            : null;
    }

    private static DecodedImage? DecodeDataUriImage(string src)
    {
        var match = Regex.Match(
            src,
            "^data:(?<mime>image/[a-zA-Z0-9.+-]+);base64,(?<data>[\\s\\S]+)$",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        var kind = match.Groups["mime"].Value.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/jpeg" or "image/jpg" => "jpeg",
            "image/gif" => "gif",
            "image/bmp" => "bmp",
            _ => null,
        };
        if (kind is null)
        {
            return null;
        }

        try
        {
            var base64 = Regex.Replace(match.Groups["data"].Value, "\\s+", "");
            return new DecodedImage(Convert.FromBase64String(base64), kind);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static ImageDimensions? GetImageDimensions(byte[] bytes)
    {
        if (bytes.Length >= 24 &&
            bytes[0] == 0x89 &&
            bytes[1] == 0x50 &&
            bytes[2] == 0x4E &&
            bytes[3] == 0x47)
        {
            return new ImageDimensions(
                ReadBigEndianInt32(bytes, 16),
                ReadBigEndianInt32(bytes, 20));
        }

        if (bytes.Length >= 10 &&
            bytes[0] == 0x47 &&
            bytes[1] == 0x49 &&
            bytes[2] == 0x46)
        {
            return new ImageDimensions(
                bytes[6] | (bytes[7] << 8),
                bytes[8] | (bytes[9] << 8));
        }

        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xD8)
        {
            var index = 2;
            while (index + 8 < bytes.Length)
            {
                if (bytes[index] != 0xFF)
                {
                    index++;
                    continue;
                }

                var marker = bytes[index + 1];
                if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
                {
                    return new ImageDimensions(
                        (bytes[index + 7] << 8) + bytes[index + 8],
                        (bytes[index + 5] << 8) + bytes[index + 6]);
                }

                var length = (bytes[index + 2] << 8) + bytes[index + 3];
                if (length < 2)
                {
                    break;
                }

                index += 2 + length;
            }
        }

        return null;
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset) =>
        (bytes[offset] << 24) +
        (bytes[offset + 1] << 16) +
        (bytes[offset + 2] << 8) +
        bytes[offset + 3];

    private static (long Cx, long Cy) FitImageToPage(ImageDimensions dimensions)
    {
        const long emuPerPixelAt96Dpi = 9525L;
        const long maxWidthEmu = 5_950_000L;
        const long maxHeightEmu = 7_800_000L;

        var width = Math.Max(dimensions.Width, 1) * emuPerPixelAt96Dpi;
        var height = Math.Max(dimensions.Height, 1) * emuPerPixelAt96Dpi;
        var scale = Math.Min(
            1d,
            Math.Min(
                maxWidthEmu / (double)width,
                maxHeightEmu / (double)height));

        return ((long)Math.Round(width * scale), (long)Math.Round(height * scale));
    }

    private static Drawing CreateImageDrawing(
        string relationshipId,
        long cx,
        long cy,
        string name)
    {
        var drawingId = (UInt32Value)(uint)(Math.Abs(relationshipId.GetHashCode()) % int.MaxValue + 1);
        return new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = cx, Cy = cy },
                new DW.EffectExtent
                {
                    LeftEdge = 0L,
                    TopEdge = 0L,
                    RightEdge = 0L,
                    BottomEdge = 0L,
                },
                new DW.DocProperties
                {
                    Id = drawingId,
                    Name = name,
                },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties
                                {
                                    Id = 0U,
                                    Name = name,
                                },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = cx, Cy = cy }),
                                new A.PresetGeometry(
                                    new A.AdjustValueList())
                                {
                                    Preset = A.ShapeTypeValues.Rectangle,
                                })))
                    {
                        Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture",
                    }))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U,
            });
    }

    private static string DecodeAndStripHtml(string html)
    {
        var withoutImages = Regex.Replace(
            html,
            "<img[^>]*(alt=[\"'](?<alt>[^\"']*)[\"'])?[^>]*>",
            " ",
            RegexOptions.IgnoreCase);
        var stripped = Regex.Replace(
            withoutImages,
            "<[^>]+>",
            " ",
            RegexOptions.IgnoreCase);
        return WebUtility.HtmlDecode(stripped)
            .Replace('\u00A0', ' ')
            .Trim();
    }

    private static void AddAnnexHeading(
        Body body,
        string employerEntity,
        int applicationCount)
    {
        body.Append(CreateParagraph(
            "ANNEXES",
            18,
            Red,
            bold: true,
            alignment: JustificationValues.Center,
            spaceAfter: 440,
            styleId: "Heading1"));

        var titleCell = new TableCell(
            new TableCellProperties(
                new TableCellBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 8U },
                    new LeftBorder { Val = BorderValues.Single, Size = 8U },
                    new BottomBorder { Val = BorderValues.Single, Size = 8U },
                    new RightBorder { Val = BorderValues.Single, Size = 8U }),
                new TableCellMargin(
                    new TopMargin { Width = "110", Type = TableWidthUnitValues.Dxa },
                    new StartMargin { Width = "110", Type = TableWidthUnitValues.Dxa },
                    new BottomMargin { Width = "110", Type = TableWidthUnitValues.Dxa },
                    new EndMargin { Width = "110", Type = TableWidthUnitValues.Dxa })),
            CreateParagraph(
                $"Annexe : Description systématique des applications du SI {employerEntity}",
                17,
                Dark,
                bold: true,
                alignment: JustificationValues.Center,
                spaceAfter: 0));
        var titleTable = new Table(
            new TableProperties(
                new TableWidth { Width = "100%", Type = TableWidthUnitValues.Pct }),
            new TableRow(titleCell));
        body.Append(titleTable);
        body.Append(CreateParagraph(
            $"{applicationCount} application(s) métier · Généré le {DateTime.Now:dd/MM/yyyy}",
            9,
            Gray,
            alignment: JustificationValues.Center,
            spaceBefore: 100,
            spaceAfter: 340));
    }

    private static void AddApplicationHeading(Body body, string applicationName)
    {
        body.Append(CreateParagraph(
            $"APPLICATION : {applicationName.ToUpper(CultureInfo.CurrentCulture)}",
            16,
            Red,
            bold: true,
            keepNext: true,
            spaceAfter: 160,
            styleId: "Heading2"));
    }

    private static void AddSection(
        Body body,
        string title,
        string? content,
        bool redHeading = false)
    {
        body.Append(CreateParagraph(
            title,
            13,
            redHeading ? Red : Blue,
            bold: true,
            keepNext: true,
            spaceBefore: 180,
            spaceAfter: 60,
            styleId: "Heading3"));
        AddTextBlock(body, content);
    }

    private static void AddFunctionalFlows(
        Body body,
        string? narrative,
        IReadOnlyList<ApplicationFlow> flows)
    {
        AddSection(body, "Principaux traitements et flux fonctionnels",
            narrative, redHeading: true);
        if (flows.Count == 0)
        {
            return;
        }

        body.Append(CreateParagraph(
            "Flux répertoriés dans la cartographie",
            10,
            Red,
            bold: true,
            keepNext: true,
            spaceBefore: 80,
            spaceAfter: 30,
            styleId: "Heading3"));
        foreach (var flow in flows.Take(30))
        {
            var details = new[] { flow.PatternName, flow.InteractionMode, flow.TechnologyName }
                .Where(x => !string.IsNullOrWhiteSpace(x));
            AddBullet(body,
                $"{flow.SourceName} → {flow.TargetName} — {flow.Name} ({string.Join(", ", details)})");
        }
        if (flows.Count > 30)
        {
            AddBullet(body, $"… {flows.Count - 30} autre(s) flux.");
        }
    }

    private static void AddTechnicalFramework(
        Body body,
        string? narrative,
        int applicationId,
        IReadOnlyList<ApplicationRelationship> relationships)
    {
        var technicalComponents = relationships
            .Select(x => x.Counterpart(applicationId))
            .Where(x => x.Category?.Contains(
                "Application Métier",
                StringComparison.OrdinalIgnoreCase) != true)
            .DistinctBy(x => x.Id)
            .Take(12)
            .ToList();

        AddSection(body, "Cadre technique général (OS, SGBD, etc.)",
            narrative);
        if (technicalComponents.Count == 0)
        {
            return;
        }

        body.Append(CreateParagraph(
            "Composants techniques associés dans la CMDB",
            10,
            Blue,
            bold: true,
            keepNext: true,
            spaceBefore: 80,
            spaceAfter: 30,
            styleId: "Heading3"));
        foreach (var component in technicalComponents)
        {
            AddBullet(body,
                $"{component.Name} ({component.ExternalCiNumber}) — {component.CategoryOrModel}");
        }
    }

    private static void AddArchitecture(
        Body body,
        string? narrative,
        int applicationId,
        IReadOnlyList<ApplicationRelationship> relationships)
    {
        AddSection(body, "Architecture d'ensemble", narrative, redHeading: true);
        if (relationships.Count == 0)
        {
            return;
        }

        body.Append(CreateParagraph(
            "Dépendances et relations CMDB",
            10,
            Red,
            bold: true,
            keepNext: true,
            spaceBefore: 80,
            spaceAfter: 30,
            styleId: "Heading3"));
        foreach (var relationship in relationships.Take(15))
        {
            var counterpart = relationship.Counterpart(applicationId);
            var direction = relationship.SourceCiId == applicationId ? "→" : "←";
            AddBullet(body,
                $"{direction} {counterpart.Name} ({counterpart.ExternalCiNumber}) — {relationship.RelationshipName}");
        }
        if (relationships.Count > 15)
        {
            AddBullet(body, $"… {relationships.Count - 15} autre(s) relation(s).");
        }
    }

    private static void AddTextBlock(Body body, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            body.Append(CreateParagraph(
                "À compléter",
                10,
                Gray,
                italic: true,
                spaceAfter: 50));
            return;
        }

        var lines = content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        foreach (var line in lines)
        {
            body.Append(CreateParagraph(
                string.IsNullOrWhiteSpace(line) ? " " : line.Trim(),
                10,
                Dark,
                spaceAfter: 35));
        }
    }

    private static void AddBullet(Body body, string text)
    {
        var paragraph = CreateParagraph(
            $"•  {text}",
            9,
            Dark,
            spaceAfter: 25);
        paragraph.ParagraphProperties!.Append(
            new Indentation { Left = "360", Hanging = "180" });
        body.Append(paragraph);
    }

    private static Paragraph CreateParagraph(
        string text,
        int size,
        string color,
        bool bold = false,
        bool italic = false,
        JustificationValues? alignment = null,
        bool keepNext = false,
        int spaceBefore = 0,
        int spaceAfter = 80,
        string? styleId = null)
    {
        var paragraphProperties = new ParagraphProperties(
            new SpacingBetweenLines
            {
                Before = spaceBefore.ToString(CultureInfo.InvariantCulture),
                After = spaceAfter.ToString(CultureInfo.InvariantCulture),
                Line = "276",
                LineRule = LineSpacingRuleValues.Auto,
            });
        if (!string.IsNullOrWhiteSpace(styleId))
        {
            paragraphProperties.PrependChild(new ParagraphStyleId { Val = styleId });
        }
        if (alignment.HasValue)
        {
            paragraphProperties.Append(new Justification { Val = alignment });
        }
        if (keepNext)
        {
            paragraphProperties.Append(new KeepNext());
        }

        var runProperties = new RunProperties(
            new RunFonts
            {
                Ascii = FontName,
                HighAnsi = FontName,
                ComplexScript = FontName,
            },
            new DocumentFormat.OpenXml.Wordprocessing.Color { Val = color },
            new FontSize { Val = (size * 2).ToString(CultureInfo.InvariantCulture) },
            new FontSizeComplexScript
            {
                Val = (size * 2).ToString(CultureInfo.InvariantCulture),
            });
        if (bold)
        {
            runProperties.Append(new Bold());
        }
        if (italic)
        {
            runProperties.Append(new Italic());
        }

        return new Paragraph(
            paragraphProperties,
            new Run(
                runProperties,
                new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Paragraph CreatePageBreak() =>
        new(new Run(new Break { Type = BreakValues.Page }));

    private static string AddHeader(MainDocumentPart mainPart)
    {
        var part = mainPart.AddNewPart<HeaderPart>();
        var logoParagraph = TryCreateHeaderLogoParagraph(part)
            ?? CreateParagraph(
                "■  APICIL ÉPARGNE",
                20,
                "ED1C24",
                bold: true,
                spaceAfter: 40);
        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "100%", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new BottomBorder
                    {
                        Val = BorderValues.Single,
                        Color = "E5E7EB",
                        Size = 4U,
                    })),
            new TableRow(
                new TableCell(
                    new TableCellProperties(
                        new TableCellWidth
                        {
                            Width = "100%",
                            Type = TableWidthUnitValues.Pct,
                        }),
                    logoParagraph)));
        part.Header = new Header(table);
        part.Header.Save();
        return mainPart.GetIdOfPart(part);
    }

    private static Paragraph? TryCreateHeaderLogoParagraph(HeaderPart headerPart)
    {
        var logoPath = ResolveHeaderLogoPath();
        if (logoPath is null)
        {
            return null;
        }

        var bytes = File.ReadAllBytes(logoPath);
        var dimensions = GetImageDimensions(bytes) ?? new ImageDimensions(393, 133);
        var (cx, cy) = FitLogoToHeader(dimensions);
        var imagePart = headerPart.AddImagePart(ImagePartType.Png);
        using (var stream = new MemoryStream(bytes))
        {
            imagePart.FeedData(stream);
        }

        var relationshipId = headerPart.GetIdOfPart(imagePart);
        return new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "40" }),
            new Run(CreateImageDrawing(
                relationshipId,
                cx,
                cy,
                "Logo APICIL Épargne")));
    }

    private static string? ResolveHeaderLogoPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "APICIL_EPARGNE.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "APICIL_EPARGNE.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "front", "src", "assets", "images", "APICIL_EPARGNE.png"),
        };

        return candidates
            .Select(Path.GetFullPath)
            .FirstOrDefault(File.Exists);
    }

    private static (long Cx, long Cy) FitLogoToHeader(ImageDimensions dimensions)
    {
        const long emuPerPixelAt96Dpi = 9525L;
        const long maxWidthEmu = 2_600_000L;
        const long maxHeightEmu = 650_000L;

        var width = Math.Max(dimensions.Width, 1) * emuPerPixelAt96Dpi;
        var height = Math.Max(dimensions.Height, 1) * emuPerPixelAt96Dpi;
        var scale = Math.Min(
            1d,
            Math.Min(
                maxWidthEmu / (double)width,
                maxHeightEmu / (double)height));

        return ((long)Math.Round(width * scale), (long)Math.Round(height * scale));
    }

    private static string AddFooter(MainDocumentPart mainPart, string employerEntity)
    {
        var part = mainPart.AddNewPart<FooterPart>();
        var paragraphProperties = new ParagraphProperties(
            new ParagraphBorders(
                new TopBorder
                {
                    Val = BorderValues.Single,
                    Color = Dark,
                    Size = 6U,
                }),
            new Tabs(
                new TabStop
                {
                    Val = TabStopValues.Right,
                    Position = 9000,
                }),
            new SpacingBetweenLines { Before = "50", After = "0" });

        var paragraph = new Paragraph(paragraphProperties);
        paragraph.Append(CreateRun(
            $"Cartographie du SI {employerEntity} - PBE",
            8,
            Dark,
            bold: true));
        paragraph.Append(new Run(new TabChar()));
        paragraph.Append(CreateRun("Page ", 8, Dark, bold: true));
        AppendField(paragraph, "PAGE");
        paragraph.Append(CreateRun(" / ", 8, Dark, bold: true));
        AppendField(paragraph, "NUMPAGES");
        part.Footer = new Footer(paragraph);
        part.Footer.Save();
        return mainPart.GetIdOfPart(part);
    }

    private static Run CreateRun(
        string text,
        int size,
        string color,
        bool bold = false)
    {
        var properties = new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new DocumentFormat.OpenXml.Wordprocessing.Color { Val = color },
            new FontSize { Val = (size * 2).ToString(CultureInfo.InvariantCulture) });
        if (bold)
        {
            properties.Append(new Bold());
        }

        return new Run(
            properties,
            new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    private static void AppendField(Paragraph paragraph, string instruction)
    {
        paragraph.Append(
            new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
            new Run(new FieldCode($" {instruction} ")
            {
                Space = SpaceProcessingModeValues.Preserve,
            }),
            new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
            CreateRun("1", 8, Dark, bold: true),
            new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
    }

    private static void AddSettings(MainDocumentPart mainPart)
    {
        var part = mainPart.AddNewPart<DocumentSettingsPart>();
        part.Settings = new Settings(new UpdateFieldsOnOpen { Val = true });
        part.Settings.Save();
    }

    private static void AddStyles(MainDocumentPart mainPart)
    {
        var part = mainPart.AddNewPart<StyleDefinitionsPart>();
        part.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(
                    new RunPropertiesBaseStyle(
                        new RunFonts
                        {
                            Ascii = FontName,
                            HighAnsi = FontName,
                            ComplexScript = FontName,
                        },
                        new FontSize { Val = "20" },
                        new FontSizeComplexScript { Val = "20" })),
                new ParagraphPropertiesDefault(
                    new ParagraphPropertiesBaseStyle(
                        new SpacingBetweenLines
                        {
                            After = "80",
                            Line = "276",
                            LineRule = LineSpacingRuleValues.Auto,
                        }))),
            new Style(
                new StyleName { Val = "Normal" },
                new BasedOn { Val = "Normal" },
                new UIPriority { Val = 0 },
                new PrimaryStyle())
            {
                Type = StyleValues.Paragraph,
                StyleId = "Normal",
                Default = true,
            },
            CreateHeadingStyle("Heading1", "heading 1", 1, 16, Red, before: 280, after: 90),
            CreateHeadingStyle("Heading2", "heading 2", 2, 13, Blue, before: 180, after: 90),
            CreateHeadingStyle("Heading3", "heading 3", 3, 11, Dark, before: 120, after: 60),
            CreateHeadingStyle("TOCHeading", "TOC Heading", 0, 16, Red, before: 160, after: 140),
            CreateTocStyle("TOC1", "toc 1", 0),
            CreateTocStyle("TOC2", "toc 2", 240),
            CreateTocStyle("TOC3", "toc 3", 480));
        part.Styles.Save();
    }

    private static Style CreateHeadingStyle(
        string styleId,
        string styleName,
        int outlineLevel,
        int size,
        string color,
        int before,
        int after)
    {
        var paragraphProperties = new StyleParagraphProperties(
            new KeepNext(),
            new SpacingBetweenLines
            {
                Before = before.ToString(CultureInfo.InvariantCulture),
                After = after.ToString(CultureInfo.InvariantCulture),
            });
        if (outlineLevel > 0)
        {
            paragraphProperties.Append(
                new OutlineLevel
                {
                    Val = outlineLevel - 1,
                });
        }

        return new Style(
            new StyleName { Val = styleName },
            new BasedOn { Val = "Normal" },
            new NextParagraphStyle { Val = "Normal" },
                new UIPriority { Val = 9 + outlineLevel },
            new PrimaryStyle(),
            paragraphProperties,
            new StyleRunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName },
                new Bold(),
                new DocumentFormat.OpenXml.Wordprocessing.Color { Val = color },
                new FontSize { Val = (size * 2).ToString(CultureInfo.InvariantCulture) },
                new FontSizeComplexScript { Val = (size * 2).ToString(CultureInfo.InvariantCulture) }))
        {
            Type = StyleValues.Paragraph,
            StyleId = styleId,
        };
    }

    private static Style CreateTocStyle(string styleId, string styleName, int leftIndent)
    {
        return new Style(
            new StyleName { Val = styleName },
            new BasedOn { Val = "Normal" },
                new UIPriority { Val = 39 },
            new StyleParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "60" },
                new Indentation
                {
                    Left = leftIndent.ToString(CultureInfo.InvariantCulture),
                },
                new Tabs(
                    new TabStop
                    {
                        Val = TabStopValues.Right,
                        Leader = TabStopLeaderCharValues.Dot,
                        Position = 9350,
                    })),
            new StyleRunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName },
                new FontSize { Val = "20" },
                new FontSizeComplexScript { Val = "20" }))
        {
            Type = StyleValues.Paragraph,
            StyleId = styleId,
        };
    }

    private static string? ApplicationNatureLabel(string? value) => value switch
    {
        "InternalDevelopment" => "Développement interne Apicil",
        "IntegratedPackage" => "Logiciel sur étagère intégré",
        "SaaS" => "SaaS",
        "ItForIt" => "Application IT for IT",
        _ => value,
    };

    private static string? CriticalityLabel(string? value) => value switch
    {
        "Low" => "Faible",
        "Medium" => "Moyenne",
        "High" => "Haute",
        "Critical" => "Critique",
        _ => value,
    };

    private static string? HostingLabel(
        ConfigurationItemApplicationProfile? profile)
    {
        if (profile is null)
        {
            return null;
        }

        var mode = profile.HostingMode switch
        {
            "OnPremise" => "On premise",
            "Cloud" => "Cloud",
            "Hybrid" => "Hybride",
            _ => profile.HostingMode,
        };
        var details = new[] { mode, profile.HostingProvider, profile.CloudServiceModel }
            .Where(x => !string.IsNullOrWhiteSpace(x));
        var label = string.Join(" · ", details);
        return string.IsNullOrWhiteSpace(label) ? null : label;
    }

    private static string? YesNo(bool? value) => value switch
    {
        true => "Oui",
        false => "Non",
        _ => null,
    };

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(invalid.Contains(character) ? '_' : character);
        }

        return builder.ToString().Replace(' ', '_');
    }

    private sealed record ApplicationFlow(
        int SourceCiId,
        int TargetCiId,
        string SourceName,
        string TargetName,
        string Name,
        string PatternName,
        string InteractionMode,
        string? TechnologyName);

    private sealed record DomainDocumentSection(
        string Title,
        int HeadingLevel,
        int SortOrder,
        string? ContentHtml,
        string? PlainText);

    private sealed record CartographyDocumentContext(
        string EmployerEntity,
        IReadOnlyList<ConfigurationItem> ConfigurationItems,
        IReadOnlyList<ConfigurationItem> Applications,
        IReadOnlyList<ApplicationFlow> Flows,
        IReadOnlyList<ApplicationRelationship> Relationships,
        IReadOnlyList<DomainDocumentSection> DomainSections);

    private readonly record struct DecodedImage(
        byte[] Content,
        string Kind);

    private readonly record struct ImageDimensions(
        int Width,
        int Height);

    private sealed record ApplicationRelationship(
        int SourceCiId,
        int TargetCiId,
        string SourceName,
        string TargetName,
        string SourceExternalCiNumber,
        string TargetExternalCiNumber,
        string SourceModel,
        string TargetModel,
        string? SourceCategory,
        string? TargetCategory,
        string RelationshipName)
    {
        public RelatedConfigurationItem Counterpart(int applicationId) =>
            SourceCiId == applicationId
                ? new RelatedConfigurationItem(
                    TargetCiId,
                    TargetName,
                    TargetExternalCiNumber,
                    TargetModel,
                    TargetCategory)
                : new RelatedConfigurationItem(
                    SourceCiId,
                    SourceName,
                    SourceExternalCiNumber,
                    SourceModel,
                    SourceCategory);
    }

    private sealed record RelatedConfigurationItem(
        int Id,
        string Name,
        string ExternalCiNumber,
        string Model,
        string? Category)
    {
        public string CategoryOrModel =>
            string.IsNullOrWhiteSpace(Category) ? Model : Category;
    }
}
