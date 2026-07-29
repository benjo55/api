using api.Dtos.TaxReceipts;
using api.Models;

namespace api.Services.TaxReceipts
{
    internal static class TaxReceiptMappings
    {
        public static DonorDto ToDto(this Donor donor) =>
            new(
                donor.Id,
                donor.DonorType,
                donor.Title,
                donor.LastName,
                donor.FirstName,
                donor.CompanyName,
                donor.BirthDate,
                donor.Email,
                donor.Phone,
                donor.AddressLine1,
                donor.AddressGeoJson,
                donor.AddressLine2,
                donor.StreetNumber,
                donor.StreetName,
                donor.PostalCode,
                donor.City,
                donor.CountryCode,
                donor.Notes,
                donor.IsArchived,
                donor.FullName,
                donor.FullAddress,
                donor.Donations.Count,
                donor.Donations.Where(x => !x.IsCancelled).Sum(x => x.Amount),
                donor.Donations.Where(x => !x.IsCancelled).OrderByDescending(x => x.DonationDate).Select(x => (DateTime?)x.DonationDate).FirstOrDefault());

        public static DonationDto ToDto(this Donation donation) =>
            new(
                donation.Id,
                donation.PublicId,
                donation.OrganizationId,
                donation.DonorId,
                donation.Donor?.FullName ?? string.Empty,
                donation.DonationDate,
                donation.Amount,
                donation.Currency,
                donation.DonationForm,
                donation.OtherFormDescription,
                donation.DonationNature,
                donation.OtherNatureDescription,
                donation.PaymentMethod,
                donation.TaxRegime,
                donation.Article200Amount,
                donation.Article978Amount,
                donation.Purpose,
                donation.Reference,
                donation.ExternalReference,
                donation.Comments,
                donation.Status,
                donation.IsCancelled);

        public static BeneficiaryOrganizationDto ToDto(this BeneficiaryOrganization organization) =>
            new(
                organization.Id,
                organization.Name,
                organization.IdentifierType,
                organization.Identifier,
                organization.StreetNumber,
                organization.StreetName,
                organization.AddressGeoJson,
                organization.AddressLine2,
                organization.PostalCode,
                organization.City,
                organization.CountryCode,
                organization.Purpose,
                organization.OrganizationCategory,
                organization.OrganizationSubCategory,
                organization.OtherCategoryDescription,
                organization.RecognitionDecreeDate,
                organization.RecognitionOfficialJournalDate,
                organization.ApprovalDate,
                organization.IsDonationEnabled,
                organization.IsEligibleForTaxReceipt,
                organization.HelloAssoOrganizationSlug,
                organization.IsHelloAssoPaymentEnabled,
                organization.IsBankTransferEnabled,
                organization.IsPayPalEnabled,
                organization.PayPalMerchantAlias,
                organization.IsActive);

        public static TaxReceiptDto ToDto(this TaxReceipt receipt) =>
            new(
                receipt.Id,
                receipt.DonationId,
                receipt.BeneficiaryOrganizationId,
                receipt.ReceiptNumber,
                receipt.CerfaCode,
                receipt.CerfaVersion,
                receipt.Status,
                receipt.GeneratedFileName,
                receipt.PdfHash,
                receipt.GeneratedAt,
                receipt.GeneratedBy,
                receipt.SentAt,
                receipt.SentToEmail,
                receipt.LastEmailStatus,
                receipt.Donation?.Donor?.FullName ?? string.Empty,
                receipt.Donation?.Amount ?? 0m,
                receipt.Donation?.DonationDate ?? default);

        public static TaxReceiptEmailHistoryDto ToDto(this TaxReceiptEmailHistory history) =>
            new(
                history.Id,
                history.TaxReceiptId,
                history.RecipientEmail,
                history.Subject,
                history.Status,
                history.SentAt,
                history.ErrorMessage,
                history.RetryCount,
                history.CreatedAt);
    }
}
