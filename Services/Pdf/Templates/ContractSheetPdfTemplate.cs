using api.Dtos.Pdf;
using api.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace api.Services.Pdf.Templates
{
    public sealed class ContractSheetPdfTemplate : IPdfTemplate
    {
        public IReadOnlyCollection<PdfDocumentType> SupportedDocumentTypes { get; } =
            new[] { PdfDocumentType.ContractSheet };

        public byte[] Render(
            GeneratePdfRequestDto request,
            byte[]? logoImage,
            byte[]? qrCodeImage,
            byte[]? chartImage,
            IReadOnlyList<PdfResolvedChartDto> charts)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(22);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(container => ComposeHeader(container, request, logoImage, qrCodeImage));
                    page.Content().PaddingTop(10).Element(container => ComposeContent(container, request, chartImage, charts));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Fiche contrat - page ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static void ComposeHeader(IContainer container, GeneratePdfRequestDto request, byte[]? logoImage, byte[]? qrCodeImage)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        if (logoImage is not null)
                        {
                            left.Item().Width(100).Image(logoImage);
                        }

                        left.Item().PaddingTop(4).Text(request.Title).Bold().FontSize(18).FontColor(Colors.Blue.Darken3);

                        if (!string.IsNullOrWhiteSpace(request.SubTitle))
                        {
                            left.Item().Text(request.SubTitle).FontSize(11).FontColor(Colors.Grey.Darken2);
                        }

                        if (!string.IsNullOrWhiteSpace(request.Reference))
                        {
                            left.Item().Text($"Reference interne: {request.Reference}").FontColor(Colors.Grey.Darken2);
                        }
                    });

                    if (qrCodeImage is not null)
                    {
                        row.ConstantItem(82).Height(82).Image(qrCodeImage);
                    }
                });

                column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
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
                    column.Item().Element(c => ComposeIdentityCard(c, request.Metadata));
                    column.Item().PaddingTop(12);
                }

                if (request.Sections.Count > 0)
                {
                    foreach (var section in request.Sections)
                    {
                        column.Item().PaddingBottom(10).Element(c => ComposeSection(c, section));
                    }
                }

                if (request.Tables.Count > 0)
                {
                    foreach (var table in request.Tables)
                    {
                        column.Item().PaddingBottom(14).Element(c => ComposeTable(c, table));
                    }
                }

                if (charts.Count > 0)
                {
                    var firstLeftChart = charts[0];
                    var firstRightChart = charts.Count > 1 ? charts[1] : null;

                    column.Item().PaddingTop(8).ShowEntire().Column(section =>
                    {
                        section.Item().Text("Graphiques correspondants").Bold().FontSize(11);
                        section.Item().PaddingTop(6).Element(rowContainer => ComposeChartRow(rowContainer, firstLeftChart, firstRightChart));
                    });

                    for (var i = 2; i < charts.Count; i += 2)
                    {
                        var leftChart = charts[i];
                        var rightChart = i + 1 < charts.Count ? charts[i + 1] : null;
                        column.Item().PaddingTop(6).Element(rowContainer => ComposeChartRow(rowContainer, leftChart, rightChart));
                    }
                }
                else if (chartImage is not null)
                {
                    column.Item().PaddingTop(8).Text("Graphique").Bold().FontSize(11);
                    column.Item().PaddingTop(4).Image(chartImage).FitWidth();
                }
            });
        }

        private static void ComposeChartCell(IContainer container, PdfResolvedChartDto chart)
        {
            container.ShowEntire().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(chartColumn =>
            {
                if (!string.IsNullOrWhiteSpace(chart.Title))
                {
                    chartColumn.Item().Text(chart.Title).SemiBold().FontSize(9);
                }

                chartColumn.Item().PaddingTop(3).Image(chart.Content).FitWidth();
            });
        }

        private static void ComposeChartRow(IContainer container, PdfResolvedChartDto leftChart, PdfResolvedChartDto? rightChart)
        {
            container.Row(row =>
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

        private static void ComposeIdentityCard(IContainer container, List<PdfMetadataItemDto> metadata)
        {
            container.Border(1).BorderColor(Colors.Blue.Lighten2).Background(Colors.Blue.Lighten5).Padding(12).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                for (var i = 0; i < metadata.Count; i += 2)
                {
                    var first = metadata[i];
                    var second = i + 1 < metadata.Count ? metadata[i + 1] : null;

                    table.Cell().Element(c => ComposeKeyValue(c, first.Key, first.Value));
                    table.Cell().Element(c =>
                    {
                        if (second is null)
                        {
                            c.Text(string.Empty);
                        }
                        else
                        {
                            ComposeKeyValue(c, second.Key, second.Value);
                        }
                    });
                }
            });
        }

        private static void ComposeKeyValue(IContainer container, string key, string value)
        {
            container.PaddingBottom(6).Column(column =>
            {
                column.Item().Text(string.IsNullOrWhiteSpace(key) ? "-" : key).SemiBold().FontSize(9).FontColor(Colors.Grey.Darken2);
                column.Item().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).FontSize(10);
            });
        }

        private static void ComposeSection(IContainer container, PdfSectionDto section)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
            {
                if (!string.IsNullOrWhiteSpace(section.Title))
                {
                    column.Item().Text(section.Title).Bold().FontSize(11);
                }

                column.Item().PaddingTop(3).Text(string.IsNullOrWhiteSpace(section.Content) ? "-" : section.Content);
            });
        }

        private static void ComposeTable(IContainer container, PdfTableDto table)
        {
            var headers = table.Headers.Count > 0 ? table.Headers : new List<string> { "Valeur" };

            container.Column(column =>
            {
                if (!string.IsNullOrWhiteSpace(table.Title))
                {
                    column.Item().Text(table.Title).Bold().FontSize(11);
                }

                column.Item().PaddingTop(5).Table(grid =>
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
                                .Background(Colors.Blue.Lighten4)
                                .BorderBottom(1)
                                .BorderColor(Colors.Blue.Lighten1)
                                .Padding(6)
                                .Text(item)
                                .SemiBold();
                        }
                    });

                    foreach (var row in table.Rows)
                    {
                        for (var i = 0; i < headers.Count; i++)
                        {
                            var value = i < row.Count ? row[i] : string.Empty;
                            grid.Cell()
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5)
                                .Text(string.IsNullOrWhiteSpace(value) ? "-" : value);
                        }
                    }
                });
            });
        }
    }
}
