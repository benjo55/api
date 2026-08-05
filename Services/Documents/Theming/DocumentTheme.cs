using QuestPDF.Helpers;

namespace api.Services.Documents.Theming
{
    public sealed record DocumentTheme(
        string BrandName,
        string PrimaryColor,
        string AccentColor,
        string TextColor,
        string MutedColor,
        string TableHeaderBackground,
        string FontFamily)
    {
        public static DocumentTheme Default { get; } = new(
            "Financial Life",
            Colors.Blue.Darken3,
            Colors.Red.Darken1,
            Colors.Grey.Darken4,
            Colors.Grey.Darken1,
            Colors.Blue.Lighten5,
            "Arial");
    }
}
