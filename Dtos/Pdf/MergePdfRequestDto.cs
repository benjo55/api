using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Pdf
{
    public sealed class MergePdfRequestDto
    {
        [StringLength(160)]
        public string FileName { get; set; } = "merged-document";

        public List<MergePdfPartDto> Documents { get; set; } = new();
    }

    public sealed class MergePdfPartDto
    {
        [StringLength(160)]
        public string? FileName { get; set; }

        [Required]
        public string Base64Content { get; set; } = string.Empty;
    }
}
