using api.Models.Enum;

namespace api.Models
{
    public class DocumentLayoutTemplate
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PageFormat { get; set; } = "A4";
        public decimal MarginTopMm { get; set; } = 18;
        public decimal MarginRightMm { get; set; } = 16;
        public decimal MarginBottomMm { get; set; } = 18;
        public decimal MarginLeftMm { get; set; } = 16;
        public string Css { get; set; } = string.Empty;
        public string? HeaderHtml { get; set; }
        public string? FooterHtml { get; set; }
        public int TemplateVersion { get; set; } = 1;
        public DocumentRevisionStatus Status { get; set; } = DocumentRevisionStatus.Published;
        public bool IsActive { get; set; } = true;
        public byte[] RowVersion { get; set; } = [];
    }
}
