using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Pdf
{
    public sealed class GenerateClientCaseFileRequestDto
    {
        [Range(1, int.MaxValue)]
        public int ContractId { get; set; }

        [StringLength(160)]
        public string FileName { get; set; } = "dossier-client";

        public bool IncludeContractSheet { get; set; } = true;
        public bool IncludeSituationStatement { get; set; } = true;
        public bool IncludeOperationsHistory { get; set; } = true;
        public bool IncludeAssetAllocationReport { get; set; } = true;

        public string? LogoBase64 { get; set; }
        public string? LogoUrl { get; set; }

        public List<MergePdfPartDto> AdditionalDocuments { get; set; } = new();
    }
}
