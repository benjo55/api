using api.Models.Enum;

namespace api.Models
{
    public class TaxReceipt
    {
        public int Id { get; set; }
        public int DonationId { get; set; }
        public Donation Donation { get; set; } = null!;
        public int BeneficiaryOrganizationId { get; set; }
        public BeneficiaryOrganization BeneficiaryOrganization { get; set; } = null!;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string CerfaCode { get; set; } = "2041-RD";
        public string CerfaVersion { get; set; } = "11580*05";
        public TaxReceiptStatus Status { get; set; } = TaxReceiptStatus.Draft;
        public string? GenerationRequestKey { get; set; }
        public string? GeneratedFileName { get; set; }
        public int? DocumentArtifactId { get; set; }
        public DocumentArtifact? DocumentArtifact { get; set; }
        public string? PdfHash { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public string? GeneratedBy { get; set; }
        public DateTime? SentAt { get; set; }
        public string? SentToEmail { get; set; }
        public TaxReceiptEmailStatus? LastEmailStatus { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public int? ReplacementReceiptId { get; set; }
        public TaxReceipt? ReplacementReceipt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TaxReceiptEmailHistory> EmailHistory { get; set; } = new List<TaxReceiptEmailHistory>();
    }
}
