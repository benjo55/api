namespace api.Services.TaxReceipts
{
    public sealed class CerfaTemplateMapping
    {
        public string CerfaCode { get; set; } = string.Empty;
        public string CerfaVersion { get; set; } = string.Empty;
        public string TemplateFile { get; set; } = string.Empty;
        public Dictionary<string, CerfaTextFieldMapping> TextFields { get; set; } = new();
        public Dictionary<string, CerfaCheckboxFieldMapping> Checkboxes { get; set; } = new();
    }

    public sealed class CerfaTextFieldMapping
    {
        public int Page { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double FontSize { get; set; } = 9;
        public double Width { get; set; } = 220;
        public bool Uppercase { get; set; }
        public string? Align { get; set; }
    }

    public sealed class CerfaCheckboxFieldMapping
    {
        public int Page { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Size { get; set; } = 8;
    }
}
