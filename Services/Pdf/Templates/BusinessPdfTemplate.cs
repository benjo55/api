using api.Dtos.Pdf;
using api.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace api.Services.Pdf.Templates
{
    public sealed class BusinessPdfTemplate : IPdfTemplate
    {
        public IReadOnlyCollection<PdfDocumentType> SupportedDocumentTypes { get; } =
            Enum.GetValues<PdfDocumentType>()
                .Where(x => x != PdfDocumentType.ContractSheet)
                .ToArray();

        public byte[] Render(
            GeneratePdfRequestDto request,
            byte[]? logoImage,
            byte[]? qrCodeImage,
            byte[]? chartImage,
            IReadOnlyList<PdfResolvedChartDto> charts)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(header => ComposeHeader(header, request, logoImage, qrCodeImage));
                    page.Content().PaddingVertical(10).Element(content => ComposeContent(content, request, chartImage, charts));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static void ComposeHeader(
            IContainer container,
            GeneratePdfRequestDto request,
            byte[]? logoImage,
            byte[]? qrCodeImage)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    if (logoImage is not null)
                    {
                        column.Item().Width(90).Image(logoImage);
                    }

                    column.Item().Text(request.Title).Bold().FontSize(18).FontColor(Colors.Blue.Darken3);

                    if (!string.IsNullOrWhiteSpace(request.SubTitle))
                    {
                        column.Item().Text(request.SubTitle).FontSize(11).FontColor(Colors.Grey.Darken1);
                    }

                    column.Item().Text($"Type : {GetDocumentTypeLabel(request.DocumentType)}").FontColor(Colors.Grey.Darken2);

                    if (!string.IsNullOrWhiteSpace(request.Reference))
                    {
                        column.Item().Text($"Référence : {request.Reference}").FontColor(Colors.Grey.Darken2);
                    }
                });

                if (qrCodeImage is not null)
                {
                    row.ConstantItem(90).AlignRight().AlignMiddle().Width(75).Height(75).Image(qrCodeImage);
                }
            });
        }

        private static void ComposeContent(
            IContainer container,
            GeneratePdfRequestDto request,
            byte[]? chartImage,
            IReadOnlyList<PdfResolvedChartDto> charts)
        {
            container.Column(column =>
            {
                if (request.Metadata.Count > 0)
                {
                    column.Item().PaddingBottom(8).Element(c => ComposeMetadata(c, request.Metadata));
                }

                foreach (var section in request.Sections)
                {
                    column.Item().PaddingBottom(10).Element(c => ComposeSection(c, section));
                }

                foreach (var table in request.Tables)
                {
                    column.Item().PaddingBottom(16).Element(c => ComposeTable(c, table));
                }

                if (charts.Count > 0)
                {
                    column.Item().PaddingTop(8).Text("Évolution de la VL par support").Bold().FontSize(12);

                    for (var i = 0; i < charts.Count; i += 2)
                    {
                        var leftChart = charts[i];
                        var rightChart = i + 1 < charts.Count ? charts[i + 1] : null;

                        column.Item().PaddingTop(6).EnsureSpace(170).Row(row =>
                        {
                            row.RelativeItem().PaddingRight(4).Element(cell => ComposeChartCell(cell, leftChart));
                            row.RelativeItem().PaddingLeft(4).Element(cell =>
                            {
                                if (rightChart is null)
                                {
                                    cell.MinHeight(1);
                                }
                                else
                                {
                                    ComposeChartCell(cell, rightChart);
                                }
                            });
                        });
                    }
                }
                else if (chartImage is not null)
                {
                    column.Item().PaddingTop(8).Text("Graphique").Bold().FontSize(12);
                    column.Item().PaddingTop(5).Image(chartImage).FitWidth();
                }
            });
        }

        private static void ComposeChartCell(IContainer container, PdfResolvedChartDto chart)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(chartColumn =>
            {
                if (!string.IsNullOrWhiteSpace(chart.Title))
                {
                    chartColumn.Item().Text(chart.Title).SemiBold().FontSize(9);
                }

                chartColumn.Item().PaddingTop(3).Image(chart.Content).FitWidth();
            });
        }

        private static void ComposeMetadata(IContainer container, List<PdfMetadataItemDto> metadata)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                });

                foreach (var item in metadata)
                {
                    table.Cell().PaddingVertical(3).Text(item.Key).SemiBold();
                    table.Cell().PaddingVertical(3).Text(item.Value);
                }
            });
        }

        private static void ComposeSection(IContainer container, PdfSectionDto section)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
            {
                if (!string.IsNullOrWhiteSpace(section.Title))
                {
                    column.Item().Text(section.Title).Bold().FontSize(12);
                }

                column.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(section.Content) ? "-" : section.Content);
            });
        }

        private static void ComposeTable(IContainer container, PdfTableDto table)
        {
            var headers = table.Headers.Count > 0 ? table.Headers : new List<string> { "Valeur" };

            container.Column(column =>
            {
                if (!string.IsNullOrWhiteSpace(table.Title))
                {
                    column.Item().Text(table.Title).Bold().FontSize(12);
                }

                column.Item().PaddingTop(4).Table(grid =>
                {
                    grid.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in headers)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    grid.Header(header =>
                    {
                        foreach (var item in headers)
                        {
                            header.Cell()
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Background(Colors.Blue.Lighten5)
                                .Padding(6)
                                .Text(item)
                                .SemiBold();
                        }
                    });

                    foreach (var row in table.Rows)
                    {
                        for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                        {
                            var value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                            grid.Cell()
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5)
                                .Text(value);
                        }
                    }
                });
            });
        }

        private static string GetDocumentTypeLabel(PdfDocumentType documentType)
        {
            return documentType switch
            {
                PdfDocumentType.ContractSheet => "Fiche contrat",
                PdfDocumentType.SituationStatement => "Relevé de situation",
                PdfDocumentType.OperationNotice => "Avis d'opération",
                PdfDocumentType.OperationsHistory => "Historique des opérations",
                PdfDocumentType.FinancialSupportSheet => "Fiche support financier",
                PdfDocumentType.AssetAllocationReport => "Rapport d'allocation d'actifs",
                PdfDocumentType.PatrimonialReport => "Reporting patrimonial",
                PdfDocumentType.ClientCaseFile => "Dossier client",
                PdfDocumentType.ScreenExport => "Export d'écran",
                PdfDocumentType.RegulatoryDocument => "Document réglementaire",
                PdfDocumentType.PersonalizedLetter => "Courrier personnalisé",
                PdfDocumentType.ChartReport => "Rapport graphique",
                _ => documentType.ToString()
            };
        }
    }
}
