using api.Interfaces.Documents;
using api.Services.Documents.Models;
using api.Services.Documents.Theming;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace api.Services.Documents.Renderers
{
    public sealed class ContractSituationQuestPdfRenderer : IDocumentRenderer
    {
        public Task<RenderedDocument> RenderAsync(
            object model,
            DocumentDefinition definition,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            var document = (ContractSituationDocumentModel)model;
            var theme = DocumentTheme.Default;

            var bytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(style => style
                        .FontFamily(theme.FontFamily)
                        .FontSize(9)
                        .FontColor(theme.TextColor));

                    page.Header().Column(header =>
                    {
                        header.Item().Text("Situation du contrat")
                            .Bold()
                            .FontSize(18)
                            .FontColor(theme.PrimaryColor);
                        header.Item().Text($"{document.ContractNumber} - {document.HolderName}")
                            .FontSize(10)
                            .FontColor(theme.MutedColor);
                        header.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingVertical(14).Column(column =>
                    {
                        column.Spacing(12);
                        ComposeIdentity(column, document);
                        ComposeFinancialSummary(column, document);
                        ComposeSupports(column, document);
                        ComposeOperations(column, document);
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
                $"Situation_contrat_{document.ContractNumber}_{document.AsOfDate:yyyyMMdd}.pdf",
                new Dictionary<string, string>
                {
                    ["contractId"] = document.ContractId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["contractNumber"] = document.ContractNumber,
                    ["asOfDate"] = document.AsOfDate.ToString("yyyy-MM-dd")
                }));
        }

        private static void ComposeIdentity(ColumnDescriptor column, ContractSituationDocumentModel document)
        {
            column.Item().Text("Identité").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
            DocumentRendererHelpers.ComposeKeyValueTable(column, new[]
            {
                ("Contrat", $"{document.ContractNumber} - {document.ContractLabel}"),
                ("Titulaire", document.HolderName),
                ("Produit", document.ProductName),
                ("Assureur", document.InsurerName),
                ("Date de situation", DocumentRendererHelpers.Date(document.AsOfDate))
            });
        }

        private static void ComposeFinancialSummary(ColumnDescriptor column, ContractSituationDocumentModel document)
        {
            column.Item().Text("Synthèse financière").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
            DocumentRendererHelpers.ComposeKeyValueTable(column, new[]
            {
                ("Valeur actuelle", DocumentRendererHelpers.Money(document.CurrentValue, document.Currency)),
                ("Versements", DocumentRendererHelpers.Money(document.TotalPayments, document.Currency)),
                ("Rachats", DocumentRendererHelpers.Money(document.TotalWithdrawals, document.Currency)),
                ("Net investi", DocumentRendererHelpers.Money(document.NetInvested, document.Currency)),
                ("Performance", DocumentRendererHelpers.Percent(document.PerformancePercent))
            });
        }

        private static void ComposeSupports(ColumnDescriptor column, ContractSituationDocumentModel document)
        {
            column.Item().Text("Allocation par support").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.5f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                });

                foreach (var title in new[] { "Support", "Poche", "Investi", "Valorisation", "Parts", "Allocation", "Date VL" })
                {
                    table.Cell().Element(DocumentRendererHelpers.HeaderCell).Text(title).Bold().FontSize(7);
                }

                foreach (var support in document.Supports)
                {
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(support.SupportName).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(support.Compartment).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).AlignRight().Text(DocumentRendererHelpers.Money(support.InvestedAmount, document.Currency)).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).AlignRight().Text(DocumentRendererHelpers.Money(support.CurrentAmount, document.Currency)).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).AlignRight().Text(support.CurrentShares.ToString("N4")).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).AlignRight().Text(DocumentRendererHelpers.Percent(support.AllocationPercentage)).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(DocumentRendererHelpers.Date(support.LastValuationDate)).FontSize(6);
                }
            });
        }

        private static void ComposeOperations(ColumnDescriptor column, ContractSituationDocumentModel document)
        {
            column.Item().Text("Opérations récentes").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                });

                foreach (var title in new[] { "Date", "Type", "Statut", "Montant", "Exécution" })
                {
                    table.Cell().Element(DocumentRendererHelpers.HeaderCell).Text(title).Bold().FontSize(7);
                }

                foreach (var operation in document.RecentOperations)
                {
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(DocumentRendererHelpers.Date(operation.OperationDate)).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(operation.Type).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(operation.Status).FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).AlignRight().Text(operation.Amount.HasValue ? DocumentRendererHelpers.Money(operation.Amount.Value, operation.Currency) : "-").FontSize(6);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(DocumentRendererHelpers.Date(operation.ExecutionDate)).FontSize(6);
                }
            });
        }
    }
}
