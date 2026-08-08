using api.Interfaces.Documents;
using api.Services.Documents.Models;
using api.Services.Documents.Theming;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace api.Services.Documents.Renderers
{
    public sealed class InformationSystemCartographyQuestPdfRenderer : IDocumentRenderer
    {
        private const string DocumentFontFamily = "Century Gothic";
        private const int BodyFontSize = 11;
        private const int HorizontalMargin = 60;
        private const int VerticalMargin = 30;

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
                    page.Size(PageSizes.A3);
                    page.MarginHorizontal(HorizontalMargin);
                    page.MarginVertical(VerticalMargin);
                    page.DefaultTextStyle(style => style
                        .FontFamily(DocumentFontFamily)
                        .FontSize(BodyFontSize)
                        .FontColor(theme.TextColor));

                    page.Header().Column(header =>
                    {
                        header.Item().Text($"Cartographie du SI - {document.EmployerEntity}")
                            .Bold()
                            .FontSize(20)
                            .FontColor(theme.PrimaryColor);
                        header.Item().Text($"Classification : {document.Classification} - Date : {document.AsOfDate:dd/MM/yyyy}")
                            .FontSize(BodyFontSize)
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
                        text.DefaultTextStyle(x => x.FontSize(9).FontColor(theme.MutedColor));
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
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(2.1f);
                });

                foreach (var row in new[]
                {
                    ("Applications", document.Applications.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    ("CI actifs", document.ConfigurationItems.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    ("Flux applicatifs", document.Flows.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    ("Périmètre", document.EmployerEntity)
                })
                {
                    table.Cell().Element(DocumentRendererHelpers.HeaderCell).Text(row.Item1).Bold().FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(row.Item2).FontSize(BodyFontSize);
                }
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
                        .FontSize(section.HeadingLevel == 1 ? 14 : 12)
                        .FontColor(section.HeadingLevel == 1 ? Colors.Red.Darken1 : Colors.Blue.Darken2);
                    ComposeRichSectionContent(sectionColumn, section);
                });
            }
        }

        private static void ComposeRichSectionContent(
            ColumnDescriptor sectionColumn,
            CartographyDomainSectionModel section)
        {
            var blocks = ParseRichContentBlocks(section.ContentHtml, section.Content).ToList();
            if (blocks.Count == 0)
            {
                sectionColumn.Item().Text("À compléter")
                    .FontSize(BodyFontSize)
                    .LineHeight(1.25f)
                    .FontColor(Colors.Grey.Darken1);
                return;
            }

            foreach (var block in blocks)
            {
                if (block.Text is not null)
                {
                    sectionColumn.Item().Text(block.Text)
                        .FontSize(BodyFontSize)
                        .LineHeight(1.25f)
                        .FontColor(Colors.Grey.Darken4);
                    continue;
                }

                if (block.ImageBytes is not null)
                {
                    var imageContainer = sectionColumn.Item()
                        .PaddingTop(5)
                        .PaddingBottom(5)
                        .MaxWidth(block.WidthPoints ?? 720);
                    imageContainer.Image(block.ImageBytes).FitWidth();
                    continue;
                }

                if (block.Svg is not null)
                {
                    var svgContainer = sectionColumn.Item()
                        .PaddingTop(5)
                        .PaddingBottom(5)
                        .MaxWidth(block.WidthPoints ?? 720);
                    svgContainer.Svg(block.Svg).FitWidth();
                }
            }
        }

        private static IEnumerable<RichContentBlock> ParseRichContentBlocks(
            string? html,
            string fallbackText)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                var fallback = fallbackText.Trim();
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    yield return RichContentBlock.ForText(fallback);
                }

                yield break;
            }

            var imageMatches = Regex.Matches(html, "<img\\b[^>]*>", RegexOptions.IgnoreCase);
            var cursor = 0;
            var emitted = false;

            foreach (Match imageMatch in imageMatches)
            {
                foreach (var textBlock in ParseTextBlocks(html[cursor..imageMatch.Index]))
                {
                    emitted = true;
                    yield return RichContentBlock.ForText(textBlock);
                }

                var imageTag = imageMatch.Value;
                var src = ReadHtmlAttribute(imageTag, "src");
                var width = ParseImageWidthPoints(
                    ReadHtmlAttribute(imageTag, "width") ??
                    ReadStyleWidth(ReadHtmlAttribute(imageTag, "style")));

                if (TryReadDataImage(src, out var imageBytes, out var svg))
                {
                    emitted = true;
                    yield return svg is null
                        ? RichContentBlock.ForImage(imageBytes!, width)
                        : RichContentBlock.ForSvg(svg, width);
                }

                cursor = imageMatch.Index + imageMatch.Length;
            }

            foreach (var textBlock in ParseTextBlocks(html[cursor..]))
            {
                emitted = true;
                yield return RichContentBlock.ForText(textBlock);
            }

            if (!emitted)
            {
                var fallback = fallbackText.Trim();
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    yield return RichContentBlock.ForText(fallback);
                }
            }
        }

        private static IEnumerable<string> ParseTextBlocks(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                yield break;
            }

            var normalized = html
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n');

            var listMatches = Regex.Matches(
                normalized,
                "<ul\\b[^>]*>[\\s\\S]*?</ul>",
                RegexOptions.IgnoreCase);
            var cursor = 0;

            foreach (Match listMatch in listMatches)
            {
                foreach (var line in ParsePlainTextBlocks(normalized[cursor..listMatch.Index]))
                {
                    yield return line;
                }

                foreach (var line in ParseListItemBlocks(listMatch.Value))
                {
                    yield return line;
                }

                cursor = listMatch.Index + listMatch.Length;
            }

            foreach (var line in ParsePlainTextBlocks(normalized[cursor..]))
            {
                yield return line;
            }
        }

        private static IEnumerable<string> ParsePlainTextBlocks(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                yield break;
            }

            var prepared = html;
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
                "- ",
                RegexOptions.IgnoreCase);
            prepared = Regex.Replace(prepared, "<[^>]+>", string.Empty, RegexOptions.IgnoreCase);

            foreach (var line in WebUtility.HtmlDecode(prepared)
                .Split('\n')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                yield return line;
            }
        }

        private static IEnumerable<string> ParseListItemBlocks(string listHtml)
        {
            var marker = ReadListBulletMarker(listHtml);
            var itemMatches = Regex.Matches(
                listHtml,
                "<li\\b[^>]*>(?<content>[\\s\\S]*?)</li>",
                RegexOptions.IgnoreCase);

            foreach (Match itemMatch in itemMatches)
            {
                var text = StripInlineHtml(itemMatch.Groups["content"].Value);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    yield return $"{marker} {text}";
                }
            }
        }

        private static string ReadListBulletMarker(string listHtml)
        {
            var openingTag = Regex.Match(listHtml, "^\\s*<ul\\b[^>]*>", RegexOptions.IgnoreCase).Value;
            var style = ReadHtmlAttribute(openingTag, "data-bullet-style");
            if (!string.IsNullOrWhiteSpace(style))
            {
                return BulletMarkerFromStyle(style);
            }

            var marker = ReadHtmlAttribute(openingTag, "data-bullet-marker");
            if (!string.IsNullOrWhiteSpace(marker))
            {
                return marker.Trim();
            }

            return BulletMarkerFromStyle(ReadListStyleType(ReadHtmlAttribute(openingTag, "style")));
        }

        private static string? ReadListStyleType(string? style)
        {
            if (string.IsNullOrWhiteSpace(style))
            {
                return null;
            }

            var match = Regex.Match(
                style,
                "(?:^|;)\\s*list-style-type\\s*:\\s*(?<value>[^;]+)",
                RegexOptions.IgnoreCase);

            return match.Success ? match.Groups["value"].Value.Trim() : null;
        }

        private static string BulletMarkerFromStyle(string? style)
        {
            var normalized = (style ?? string.Empty)
                .Replace("\"", string.Empty, StringComparison.Ordinal)
                .Replace("'", string.Empty, StringComparison.Ordinal)
                .Trim()
                .ToLowerInvariant();

            return normalized switch
            {
                "circle" => "◦",
                "square" => "▪",
                "filledsquare" or "filled-square" or "■" => "■",
                "hollowsquare" or "hollow-square" or "□" => "□",
                "diamond" or "◆" => "◆",
                "hollowdiamond" or "hollow-diamond" or "◇" => "◇",
                "dash" or "-" or "–" => "–",
                "plus" or "+" => "+",
                "cross" or "✚" => "✚",
                "check" or "✓" => "✓",
                "arrow" or "→" => "→",
                "triangle" or "►" => "►",
                "star" or "★" => "★",
                _ => "•",
            };
        }

        private static string StripInlineHtml(string html)
        {
            var prepared = Regex.Replace(
                html,
                "</(p|div|h1|h2|h3)>",
                " ",
                RegexOptions.IgnoreCase);
            prepared = Regex.Replace(
                prepared,
                "<br\\s*/?>",
                " ",
                RegexOptions.IgnoreCase);
            prepared = Regex.Replace(prepared, "<[^>]+>", string.Empty, RegexOptions.IgnoreCase);

            return Regex.Replace(WebUtility.HtmlDecode(prepared), "\\s+", " ").Trim();
        }

        private static string? ReadHtmlAttribute(string tag, string name)
        {
            var match = Regex.Match(
                tag,
                "\\b" + Regex.Escape(name) + "\\s*=\\s*(?:\"(?<value>[^\"]*)\"|'(?<value>[^']*)'|(?<value>[^\\s>]+))",
                RegexOptions.IgnoreCase);

            return match.Success
                ? WebUtility.HtmlDecode(match.Groups["value"].Value)
                : null;
        }

        private static string? ReadStyleWidth(string? style)
        {
            if (string.IsNullOrWhiteSpace(style))
            {
                return null;
            }

            var match = Regex.Match(style, "(?:^|;)\\s*width\\s*:\\s*(?<value>[^;]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["value"].Value.Trim() : null;
        }

        private static float? ParseImageWidthPoints(string? width)
        {
            if (string.IsNullOrWhiteSpace(width) || width.Contains('%', StringComparison.Ordinal))
            {
                return null;
            }

            var match = Regex.Match(width, "(?<value>[0-9]+(?:[\\.,][0-9]+)?)");
            if (!match.Success ||
                !double.TryParse(
                    match.Groups["value"].Value.Replace(',', '.'),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var pixels))
            {
                return null;
            }

            return (float)Math.Clamp(pixels * 0.75d, 90d, 720d);
        }

        private static bool TryReadDataImage(
            string? src,
            out byte[]? imageBytes,
            out string? svg)
        {
            imageBytes = null;
            svg = null;

            if (string.IsNullOrWhiteSpace(src))
            {
                return false;
            }

            var match = Regex.Match(
                src,
                "^data:(?<mime>image/[^;]+);base64,(?<data>.+)$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success)
            {
                return false;
            }

            try
            {
                var bytes = Convert.FromBase64String(match.Groups["data"].Value);
                var mime = match.Groups["mime"].Value;
                if (mime.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase))
                {
                    svg = System.Text.Encoding.UTF8.GetString(bytes);
                }
                else
                {
                    imageBytes = bytes;
                }

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static void ComposeApplications(ColumnDescriptor column, InformationSystemCartographyDocumentModel document)
        {
            if (document.Applications.Count == 0)
            {
                return;
            }

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
                    table.Cell().Element(DocumentRendererHelpers.HeaderCell).Text(title).Bold().FontSize(BodyFontSize);
                }

                foreach (var app in document.Applications)
                {
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(app.ExternalCiNumber).FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(app.Name).FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(app.Domain ?? "-").FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(app.Owner ?? "-").FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(LabelCriticality(app.Criticality)).FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(app.Description ?? "-").FontSize(BodyFontSize);
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
                    table.Cell().Element(DocumentRendererHelpers.HeaderCell).Text(title).Bold().FontSize(BodyFontSize);
                }

                foreach (var flow in document.Flows.Take(80))
                {
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(flow.SourceName).FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(flow.TargetName).FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(flow.Name).FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(flow.InteractionMode).FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(flow.TechnologyName ?? flow.PatternName).FontSize(BodyFontSize);
                }
            });
        }

        private static void ComposeConfigurationItems(ColumnDescriptor column, InformationSystemCartographyDocumentModel document)
        {
            if (document.ConfigurationItems.Count == 0)
            {
                return;
            }

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
                    table.Cell().Element(DocumentRendererHelpers.HeaderCell).Text(title).Bold().FontSize(BodyFontSize);
                }

                foreach (var item in document.ConfigurationItems)
                {
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.ExternalCiNumber).FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.Name).FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.Category ?? "-").FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.Model ?? "-").FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.Status ?? "-").FontSize(BodyFontSize);
                    table.Cell().Element(DocumentRendererHelpers.BodyCell).Text(item.OwnerName ?? "-").FontSize(BodyFontSize);
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

        private sealed record RichContentBlock(
            string? Text,
            byte[]? ImageBytes,
            string? Svg,
            float? WidthPoints)
        {
            public static RichContentBlock ForText(string text) => new(text, null, null, null);

            public static RichContentBlock ForImage(byte[] imageBytes, float? widthPoints) =>
                new(null, imageBytes, null, widthPoints);

            public static RichContentBlock ForSvg(string svg, float? widthPoints) =>
                new(null, null, svg, widthPoints);
        }
    }
}
