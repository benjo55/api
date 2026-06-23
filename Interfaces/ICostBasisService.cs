namespace api.Interfaces
{
    public interface ICostBasisService
    {
        Task<CostBasisRebuildResult> RebuildAsync(int contractId);
    }

    public sealed record CostBasisRebuildResult(
        int ContractId,
        int PositionCount,
        decimal TotalRemainingCostBasis,
        decimal MaximumShareDelta);
}
