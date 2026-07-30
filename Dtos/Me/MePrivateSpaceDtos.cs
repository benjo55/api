namespace api.Dtos.Me
{
    public sealed record MePrivateSpaceDto(
        MePersonAccessDto? Person,
        MePrivateSpaceMetricsDto Metrics,
        IReadOnlyList<MeContractSummaryDto> Contracts,
        IReadOnlyList<MeOperationSummaryDto> RecentOperations,
        IReadOnlyList<MeDocumentSummaryDto> RecentDocuments,
        MeDonationSummaryDto Donations,
        IReadOnlyList<MeNewsItemDto> Alerts);

    public sealed record MePersonAccessDto(
        int Id,
        string FirstName,
        string LastName,
        string FullName,
        string? Email,
        string? PhoneNumber,
        DateTime? BirthDate,
        string Role,
        string Status);

    public sealed record MePrivateSpaceMetricsDto(
        int ContractCount,
        decimal TotalCurrentValue,
        decimal TotalPaidPremiums,
        decimal NetInvested,
        int AlertCount,
        int DocumentCount,
        DateTime? LastOperationDate);

    public sealed record MeContractSummaryDto(
        int Id,
        string ContractNumber,
        string ContractLabel,
        string ContractType,
        string Status,
        string Currency,
        decimal CurrentValue,
        decimal TotalPaidPremiums,
        decimal NetInvested,
        decimal? PerformancePercent,
        DateTime DateEffect,
        DateTime? DateMaturity,
        string? ProductName,
        bool HasAlert,
        int DocumentCount,
        int OperationCount);

    public sealed record MeOperationSummaryDto(
        int Id,
        int ContractId,
        string ContractNumber,
        string Type,
        string Status,
        DateTime OperationDate,
        DateTime? ExecutionDate,
        decimal? Amount,
        string Currency);

    public sealed record MeDocumentSummaryDto(
        int Id,
        int? ContractId,
        string? ContractNumber,
        string FileName,
        string FileType,
        DateTime UploadedAt,
        string Url);
}
