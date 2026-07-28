namespace api.Models
{
    public sealed class OrganizationBankAccount
    {
        public int Id { get; set; }
        public int BeneficiaryOrganizationId { get; set; }
        public BeneficiaryOrganization BeneficiaryOrganization { get; set; } = null!;
        public string AccountHolder { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public string CountryCode { get; set; } = "FR";
        public string Currency { get; set; } = "EUR";
        public string EncryptedIban { get; set; } = string.Empty;
        public string IbanLastFour { get; set; } = string.Empty;
        public string EncryptedBic { get; set; } = string.Empty;
        public string BicLastFour { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime? ValidTo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public byte[]? RowVersion { get; set; }
    }
}
