using api.Models.Enum;

namespace api.Models
{
    public class BeneficiaryOrganization
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public BeneficiaryIdentifierType IdentifierType { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public string? RnaNumber { get; set; }
        public string? Siret { get; set; }
        public string? StreetNumber { get; set; }
        public string StreetName { get; set; } = string.Empty;
        public string? AddressGeoJson { get; set; }
        public string? AddressLine2 { get; set; }
        public string? Address { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string CountryCode { get; set; } = "FR";
        public string? Email { get; set; }
        public bool IsEligibleForTaxReceipt { get; set; } = true;
        public string? FiscalArticle { get; set; }
        public string? HelloAssoOrganizationSlug { get; set; }
        public bool IsHelloAssoPaymentEnabled { get; set; }
        public string? HelloAssoEnvironment { get; set; }
        public string? HelloAssoCredentialKey { get; set; }
        public DateTime? HelloAssoConnectionLastCheckedAt { get; set; }
        public string? HelloAssoConnectionStatus { get; set; }
        public string? HelloAssoConnectionError { get; set; }
        public bool IsBankTransferEnabled { get; set; }
        public bool IsPayPalEnabled { get; set; }
        public string? PayPalMerchantAlias { get; set; }
        public string? PayPalMerchantId { get; set; }
        public string? PayPalEnvironment { get; set; }
        public string? PayPalCredentialKey { get; set; }
        public bool IsDonationEnabled { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public BeneficiaryOrganizationCategory OrganizationCategory { get; set; }
        public BeneficiaryOrganizationSubCategory OrganizationSubCategory { get; set; } = BeneficiaryOrganizationSubCategory.None;
        public string? OtherCategoryDescription { get; set; }
        public DateTime? RecognitionDecreeDate { get; set; }
        public DateTime? RecognitionOfficialJournalDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TaxReceipt> TaxReceipts { get; set; } = new List<TaxReceipt>();
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public ICollection<OrganizationBankAccount> BankAccounts { get; set; } = new List<OrganizationBankAccount>();
    }
}
