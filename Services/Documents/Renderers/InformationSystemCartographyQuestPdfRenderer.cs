using api.Interfaces.Documents;
using api.Services.Documents.Models;
using api.Services.Documents.Theming;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace api.Services.Documents.Renderers
{
    public sealed class InformationSystemCartographyQuestPdfRenderer : IDocumentRenderer
    {
        public Task<RenderedDocument> RenderAsync(
            object model,
            DocumentDefinition definition,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            var document = (InformationSystemCartographyDocumentModel)model;
            var theme = DocumentTheme.Default;

            var bytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A3.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(style => style
                        .FontFamily(theme.FontFamily)
                        .FontSize(8)
                        .FontColor(theme.TextColor));

                    page.Header().Column(header =>
                    {
                        header.Item().Text($"Cartographie du SI - {document.EmployerEntity}")
                            .Bold()
                            .FontSize(20)
                            .FontColor(theme.PrimaryColor);
                        header.Item().Text($"Classification : {document.Classification} - Date : {document.AsOfDate:dd/MM/yyyy}")
                            .FontSize(9)
                            .FontColor(theme.MutedColor);
                        header.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingVertical(12).Column(column =>
                    {
                        column.Spacing(10);
                        ComposeSummary(column, document);
                        ComposeSections(column, document);
                        ComposeApplications(column, document);
                        ComposeFlows(column, document);
                        ComposeConfigurationItems(column, document);
                    });

                    page.Footer().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8).FontColor(theme.MutedColor));
                        text.Span($"{theme.BrandName} - {definition.TemplateVersion} - Page ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();

            return Task.FromResult(new RenderedDocument(
                new MemoryStream(bytes),
                "application/pdf",
                $"Cartographie_SI_{Sanitize(document.EmployerEntity)}_{document.AsOfDate:yyyyMMdd}.pdf",
                new Dictionary<string, string>
                {
                    ["employerEntity"] = document.EmployerEntity,
                    ["asOfDate"] = document.AsOfDate.ToString("yyyy-MM-dd"),
                    ["classification"] = document.Classification
                }));
        }

        private static void ComposeSummary(ColumnDescriptor column, InformationSystemCartographyDocumentModel document)
        {
            column.Item().Text("Synthèse").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
            DocumentRendererHelpers.ComposeKeyValueTable(column, new[]
            {
                ("Applications", document.Applications.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                ("CI actifs", document.ConfigurationItems.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                ("Flux applicatifs", document.Flows.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                ("Périmètre", document.EmployerEntity)
            });
        }

        private static void ComposeSections(ColumnDescriptor column, InformationSystemCartographyDocumentModel document)
        {
            if (document.Sections.Count == 0)
            {
                return;
            }

            column.Item().Text("Description et périmètre").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
            foreach (var section in document.Sections.OrderBy(x => x.SortOrder))
            {
                column.Item().PaddingBottom(4).Column(sectionColumn =>
                {
                    sectionColumn.Item().Text(section.Title)
                        .Bold()
                        .FontSize(section.HeadingLevel == 1 ? 11 : 9)
                        .FontColor(section.HeadingLevel == 1 ? Colors.Red.Darken1 : Colors.Blue.Darken2);
                    sectionColumn.Item().Text(string.IsNullOrWhiteSpace(section.Content) ? "À compléter" : section.Content)
                        .FontSize(7)
                        .LineHeight(1.25f)
                        .FontColor(string.IsNullOrWhiteSpace(section.Content) ? Colors.Grey.Darken1 : Colors.Grey.Darken4);
                });
            }
        }

        private static void ComposeApplications(ColumnDescriptor column, InformationSystemCartographyDocumentModel document)
        {
            column.Item().Text("Applications métier").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(2.4f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(2.4f);
                });

                foreach (var title in new[] { "N° CI", "Application", "Domaine", "Propriétaire", "Criticité", "Description" })
                {
                    table.Cell().Element(DocumentRendererHelpers.HeaderCell).Text(title).Bold().FontSize(7);
                }

                foreach (var app in document.Applications)
                {
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(app.ExternalCiNumber).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(app.Name).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(app.Domain ?? "-").FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(app.Owner ?? "-").FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(LabelCriticality(app.Criticality)).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(app.Description ?? "-").FontSize(6);
                }
            });
        }

        private static void ComposeFlows(ColumnDescriptor column, InformationSystemCartographyDocumentModel document)
        {
            if (document.Flows.Count == 0)
            {
                return;
            }

            column.Item().Text("Flux applicatifs").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.3f);
                });

                foreach (var title in new[] { "Source", "Cible", "Flux", "Mode", "Technologie" })
                {
                    table.Cell().Element(DocumentRendererHelpers.HeaderCell).Text(title).Bold().FontSize(7);
                }

                foreach (var flow in document.Flows.Take(80))
                {
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(flow.SourceName).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(flow.TargetName).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(flow.Name).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(flow.InteractionMode).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(flow.TechnologyName ?? flow.PatternName).FontSize(6);
                }
            });
        }

        private static void ComposeConfigurationItems(ColumnDescriptor column, InformationSystemCartographyDocumentModel document)
        {
            column.Item().Text("Inventaire des CI").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.6f);
                });

                foreach (var title in new[] { "N° CI", "Nom", "Catégorie", "Modèle", "Statut", "Responsable" })
                {
                    table.Cell().Element(DocumentRendererHelpers.HeaderCell).Text(title).Bold().FontSize(7);
                }

                foreach (var item in document.ConfigurationItems)
                {
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.ExternalCiNumber).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.Name).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.Category ?? "-").FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.Model ?? "-").FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.Status ?? "-").FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.OwnerName ?? "-").FontSize(6);
                }
            });
        }

        private static string LabelCriticality(string? value) => value switch
        {
            "Low" => "Faible",
            "Medium" => "Moyenne",
            "High" => "Haute",
            "Critical" => "Critique",
            _ => string.IsNullOrWhiteSpace(value) ? "-" : value
        };

        private static string Sanitize(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace(' ', '_');
        }
    }
}
