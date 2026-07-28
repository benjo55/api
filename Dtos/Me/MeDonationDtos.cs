using api.Models;
using api.Models.Enum;

namespace api.Dtos.Me
{
    public sealed record MeDonationListItemDto(
        string PublicId,
        string Reference,
        DateTime DonationDate,
        decimal Amount,
        string Currency,
        DonationStatus Status,
        DateTime? PaymentConfirmedAt,
        bool ReceiptAvailable,
        string? ReceiptNumber,
        TaxReceiptStatus? ReceiptStatus,
        DateTime? ReceiptGeneratedAt,
        DateTime? ReceiptSentAt);

    public sealed record MeDonationDetailDto(
        string PublicId,
        string Reference,
        DateTime DonationDate,
        decimal Amount,
        string Currency,
        DonationStatus Status,
        DateTime? PaymentConfirmedAt,
        DonationLegacyLinkStatus LegacyLinkStatus,
        string DonorFirstName,
        string DonorLastName,
        string DonorEmail,
        string DonorAddressLine1,
        string? DonorAddressLine2,
        string DonorPostalCode,
        string DonorCity,
        string DonorCountry,
        string OrganizationName,
        MeDonationReceiptInfoDto? Receipt);

    public sealed record MeDonationReceiptInfoDto(
        int ReceiptId,
        string ReceiptNumber,
        TaxReceiptStatus Status,
        DateTime? GeneratedAt,
        DateTime? SentAt,
        string? SentToEmail,
        TaxReceiptEmailStatus? LastEmailStatus);

    public sealed record MeDonationReceiptResendResultDto(
        string PublicId,
        int ReceiptId,
        string ReceiptNumber,
        TaxReceiptEmailStatus DeliveryStatus,
        DateTime? SentAt,
        string RecipientEmail);
}
