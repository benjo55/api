namespace api.Services.Documents.Models
{
    public sealed record ContractSituationDocumentModel(
        int ContractId,
        string ContractNumber,
        string ContractLabel,
        string HolderName,
        string ProductName,
        string InsurerName,
        DateTime AsOfDate,
        string Currency,
        decimal CurrentValue,
        decimal TotalPayments,
        decimal TotalWithdrawals,
        decimal NetInvested,
        decimal? PerformancePercent,
        IReadOnlyList<ContractSituationSupportLine> Supports,
        IReadOnlyList<ContractSituationOperationLine> RecentOperations);

    public sealed record ContractSituationSupportLine(
        string SupportName,
        string Compartment,
        decimal InvestedAmount,
        decimal CurrentAmount,
        decimal CurrentShares,
        decimal AllocationPercentage,
        decimal? LastValuationAmount,
        DateTime? LastValuationDate,
        decimal? PerformancePercent);

    public sealed record ContractSituationOperationLine(
        DateTime OperationDate,
        DateTime? ExecutionDate,
        string Type,
        string Status,
        decimal? Amount,
        string Currency);
}
