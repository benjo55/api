using api.Models.Enum;

namespace api.Models
{
    public class Donation
    {
        public int Id { get; set; }
        public string PublicId { get; set; } = Guid.NewGuid().ToString("N");
        public string? Reference { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public DonationDonorSnapshot? DonorSnapshot { get; set; }
        public int OrganizationId { get; set; }
        public BeneficiaryOrganization Organization { get; set; } = null!;
        public int DonorId { get; set; }
        public Donor Donor { get; set; } = null!;
        public DateTime DonationDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EUR";
        public DonationForm DonationForm { get; set; }
        public string? OtherFormDescription { get; set; }
        public DonationNature DonationNature { get; set; }
        public string? OtherNatureDescription { get; set; }
        public DonationPaymentMethod? PaymentMethod { get; set; }
        public DonationTaxRegime TaxRegime { get; set; }
        public decimal? Article200Amount { get; set; }
        public decimal? Article978Amount { get; set; }
        public string? Purpose { get; set; }
        public string? ExternalReference { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DonationStatus Status { get; set; } = DonationStatus.Draft;
        public bool IsCancelled { get; set; }
        public DonationLegacyLinkStatus LegacyDonationLinkStatus { get; set; } = DonationLegacyLinkStatus.NotRequired;
        public DateTime? PaymentConfirmedAt { get; set; }
        public PaymentProvider? ConfirmedPaymentProvider { get; set; }
        public DateTime? PostPaymentProcessedAt { get; set; }
        public string? PostPaymentProcessingError { get; set; }
        public byte[]? RowVersion { get; set; }

        public ICollection<TaxReceipt> TaxReceipts { get; set; } = new List<TaxReceipt>();
        public ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();
    }
}
