using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace api.Services.Documents.Renderers
{
    internal static class DocumentRendererHelpers
    {
        public static string Money(decimal value, string currency) =>
            value.ToString("N2", CultureInfo.GetCultureInfo("fr-FR")) +
            (string.IsNullOrWhiteSpace(currency) ? string.Empty : $" {currency}");

        public static string Percent(decimal? value) =>
            value.HasValue ? value.Value.ToString("0.##", CultureInfo.GetCultureInfo("fr-FR")) + " %" : "-";

        public static string Date(DateTime? value) =>
            value.HasValue ? value.Value.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("fr-FR")) : "-";

        public static IContainer HeaderCell(IContainer container) =>
            container
                .Background(Colors.Blue.Lighten5)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(4)
                .PaddingHorizontal(5);

        public static IContainer BodyCell(IContainer container) =>
            container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(3)
                .PaddingHorizontal(5);

        public static void ComposeKeyValueTable(
            ColumnDescriptor column,
            IEnumerable<(string Label, string Value)> rows)
        {
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(2.1f);
                });

                foreach (var row in rows)
                {
                    table.Cell().Element(HeaderCell).Text(row.Label).Bold().FontSize(8);
                    table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(row.Value) ? "-" : row.Value).FontSize(8);
                }
            });
        }
    }
}
