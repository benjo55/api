using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Pdf
{
    public sealed class GenerateAssetAllocationReportRequestDto
    {
        [Range(1, int.MaxValue)]
        public int ContractId { get; set; }

        [StringLength(160)]
        public string FileName { get; set; } = "allocation-actifs";

        public string? LogoBase64 { get; set; }
        public string? LogoUrl { get; set; }
    }
}
