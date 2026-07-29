using api.Models.Enum;

namespace api.Dtos.TaxReceipts
{
    public record DonorDto(
        int Id,
        DonorType DonorType,
        string? Title,
        string LastName,
        string FirstName,
        string? CompanyName,
        DateTime? BirthDate,
        string? Email,
        string? Phone,
        string AddressLine1,
        string? AddressGeoJson,
        string? AddressLine2,
        string? StreetNumber,
        string StreetName,
        string PostalCode,
        string City,
        string CountryCode,
        string? Notes,
        bool IsArchived,
        string FullName,
        string FullAddress,
        int DonationCount,
        decimal TotalDonations,
        DateTime? LastDonationDate);

    public record SaveDonorDto(
        DonorType DonorType,
        string? Title,
        string LastName,
        string FirstName,
        string? CompanyName,
        DateTime? BirthDate,
        string? Email,
        string? Phone,
        string AddressLine1,
        string? AddressGeoJson,
        string? AddressLine2,
        string? StreetNumber,
        string StreetName,
        string PostalCode,
        string City,
        string CountryCode,
        string? Notes);

    public record DonationDto(
        int Id,
        string PublicId,
        int OrganizationId,
        int DonorId,
        string DonorFullName,
        DateTime DonationDate,
        decimal Amount,
        string Currency,
        DonationForm DonationForm,
        string? OtherFormDescription,
        DonationNature DonationNature,
        string? OtherNatureDescription,
        DonationPaymentMethod? PaymentMethod,
        DonationTaxRegime TaxRegime,
        decimal? Article200Amount,
        decimal? Article978Amount,
        string? Purpose,
        string? Reference,
        string? ExternalReference,
        string? Comments,
        DonationStatus Status,
        bool IsCancelled);

    public record SaveDonationDto(
        int? OrganizationId,
        int DonorId,
        DateTime DonationDate,
        decimal Amount,
        string? Currency,
        DonationForm DonationForm,
        string? OtherFormDescription,
        DonationNature DonationNature,
        string? OtherNatureDescription,
        DonationPaymentMethod? PaymentMethod,
        DonationTaxRegime TaxRegime,
        decimal? Article200Amount,
        decimal? Article978Amount,
        string? Purpose,
        string? Reference,
        string? ExternalReference,
        string? Comments);

    public record BeneficiaryOrganizationDto(
        int Id,
        string Name,
        BeneficiaryIdentifierType IdentifierType,
        string Identifier,
        string? StreetNumber,
        string StreetName,
        string? AddressGeoJson,
        string? AddressLine2,
        string PostalCode,
        string City,
        string CountryCode,
        string Purpose,
        BeneficiaryOrganizationCategory OrganizationCategory,
        BeneficiaryOrganizationSubCategory OrganizationSubCategory,
        string? OtherCategoryDescription,
        DateTime? RecognitionDecreeDate,
        DateTime? RecognitionOfficialJournalDate,
        DateTime? ApprovalDate,
        bool IsDonationEnabled,
        bool IsEligibleForTaxReceipt,
        string? HelloAssoOrganizationSlug,
        bool IsHelloAssoPaymentEnabled,
        bool IsBankTransferEnabled,
        bool IsPayPalEnabled,
        string? PayPalMerchantAlias,
        bool IsActive);

    public record SaveBeneficiaryOrganizationDto(
        string Name,
        BeneficiaryIdentifierType IdentifierType,
        string Identifier,
        string? StreetNumber,
        string StreetName,
        string? AddressGeoJson,
        string? AddressLine2,
        string PostalCode,
        string City,
        string CountryCode,
        string Purpose,
        BeneficiaryOrganizationCategory OrganizationCategory,
        BeneficiaryOrganizationSubCategory OrganizationSubCategory,
        string? OtherCategoryDescription,
        DateTime? RecognitionDecreeDate,
        DateTime? RecognitionOfficialJournalDate,
        DateTime? ApprovalDate,
        bool IsDonationEnabled,
        bool IsEligibleForTaxReceipt,
        string? HelloAssoOrganizationSlug,
        bool IsHelloAssoPaymentEnabled,
        bool IsBankTransferEnabled,
        bool IsPayPalEnabled,
        string? PayPalMerchantAlias,
        bool IsActive);

    public record TaxReceiptDto(
        int Id,
        int DonationId,
        int BeneficiaryOrganizationId,
        string ReceiptNumber,
        string CerfaCode,
        string CerfaVersion,
        TaxReceiptStatus Status,
        string? GeneratedFileName,
        string? PdfHash,
        DateTime? GeneratedAt,
        string? GeneratedBy,
        DateTime? SentAt,
        string? SentToEmail,
        TaxReceiptEmailStatus? LastEmailStatus,
        string DonorFullName,
        decimal DonationAmount,
        DateTime DonationDate);

    public record CreateTaxReceiptDto(
        int BeneficiaryOrganizationId,
        string? CerfaCode,
        string? CerfaVersion,
        string? GenerationRequestKey);

    public record SendTaxReceiptEmailDto(
        string? RecipientEmail,
        string? Subject,
        string? Body);

    public record TaxReceiptEmailHistoryDto(
        int Id,
        int TaxReceiptId,
        string RecipientEmail,
        string Subject,
        TaxReceiptEmailStatus Status,
        DateTime? SentAt,
        string? ErrorMessage,
        int RetryCount,
        DateTime CreatedAt);

    public record TaxReceiptEmailSendResultDto(
        bool Success,
        int ReceiptId,
        string ReceiptNumber,
        TaxReceiptStatus Status,
        TaxReceiptEmailStatus EmailStatus,
        DateTime? SentAt,
        string RecipientEmail,
        string Message,
        TaxReceiptEmailHistoryDto History);

    public record TaxReceiptGenerationResultDto(
        TaxReceiptDto Receipt,
        string DownloadUrl);
}
