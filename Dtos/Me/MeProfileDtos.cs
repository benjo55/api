using api.Models;
using api.Models.Enum;

namespace api.Dtos.Me
{
    public sealed record MeAccountDto(
        int Id,
        string Username,
        string FirstName,
        string LastName,
        string Email,
        bool EmailConfirmed,
        UserStatus Status,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    public sealed record MeProfileDto(
        int Id,
        string FirstName,
        string LastName,
        string? Phone,
        string AddressLine1,
        string? AddressLine2,
        string PostalCode,
        string City,
        string CountryCode,
        string? CompanyName,
        string FormattedAddress,
        bool IsComplete,
        int CompletionPercentage,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record SaveMeProfileDto(
        string FirstName,
        string LastName,
        string? Phone,
        string AddressLine1,
        string? AddressLine2,
        string PostalCode,
        string City,
        string CountryCode,
        string? CompanyName);

    public sealed record MeDashboardDto(
        MeAccountDto Account,
        MeProfileDto Profile,
        MeDonationSummaryDto Donations,
        IReadOnlyList<MeDonationListItemDto> RecentDonations,
        IReadOnlyList<MeActivityItemDto> RecentActivity,
        IReadOnlyList<MeNewsItemDto> NewsFeed,
        IReadOnlyList<MeFinancialFeedItemDto> FinancialFeed);

    public sealed record MeDonationSummaryDto(
        int DonationCount,
        decimal ConfirmedTotalAmount,
        int AvailableDocumentCount,
        DateTime? LastDonationDate);

    public sealed record MeActivityItemDto(
        DateTime Date,
        string Type,
        string Description,
        string Status,
        string? ActionUrl);

    public sealed record MeNewsItemDto(
        DateTime Date,
        string Title,
        string Description,
        string Tone,
        string? ActionLabel,
        string? ActionUrl);

    public sealed record MeFinancialFeedItemDto(
        DateTime Date,
        string Label,
        decimal Amount,
        string Currency,
        DonationStatus Status,
        string? OrganizationName,
        string? ActionUrl);

    public sealed record DonationOrganizationOptionDto(
        int Id,
        string Name,
        string Purpose,
        bool IsEligibleForTaxReceipt);

    public sealed record CreateMeDonationIntentDto(
        int OrganizationId,
        decimal Amount,
        string? Purpose,
        string? Comment,
        bool ConfirmInformationAccuracy);

    public sealed record MeDonationIntentCreatedDto(
        MeDonationListItemDto Donation,
        string Message);
}
