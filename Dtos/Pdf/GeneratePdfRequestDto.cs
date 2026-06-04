using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Pdf
{
    public sealed class GeneratePdfRequestDto
    {
        [Required]
        public PdfDocumentType DocumentType { get; set; }

        [StringLength(160)]
        public string FileName { get; set; } = "document";

        [StringLength(200)]
        public string Title { get; set; } = "Document";

        [StringLength(280)]
        public string? SubTitle { get; set; }

        [StringLength(120)]
        public string? Reference { get; set; }

        public string? QrCodeContent { get; set; }
        public string? LogoBase64 { get; set; }
        public string? LogoUrl { get; set; }
        public string? ChartBase64 { get; set; }
        public string? ChartUrl { get; set; }
        public List<PdfChartDto> Charts { get; set; } = new();

        public List<PdfMetadataItemDto> Metadata { get; set; } = new();
        public List<PdfSectionDto> Sections { get; set; } = new();
        public List<PdfTableDto> Tables { get; set; } = new();
    }

    public sealed class PdfChartDto
    {
        [StringLength(160)]
        public string? Title { get; set; }

        public string? Base64 { get; set; }
        public string? Url { get; set; }
    }

    public sealed class PdfResolvedChartDto
    {
        public string? Title { get; set; }
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }

    public sealed class PdfMetadataItemDto
    {
        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [StringLength(500)]
        public string Value { get; set; } = string.Empty;
    }

    public sealed class PdfSectionDto
    {
        [StringLength(160)]
        public string? Title { get; set; }

        public string Content { get; set; } = string.Empty;
    }

    public sealed class PdfTableDto
    {
        [StringLength(160)]
        public string? Title { get; set; }

        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
    }
}
