using api.Models;
using api.Models.Enum;

namespace api.Interfaces
{
    public sealed class FeeResolutionRequest
    {
        public FeeCategory Category { get; init; }
        public FeeType? FeeType { get; init; }
        public DateTime AsOfDateUtc { get; init; }
        public decimal? OperationAmount { get; init; }
        public int? ProductId { get; init; }
        public int? ContractId { get; init; }
        public int? CompartmentId { get; init; }
        public int? FinancialSupportId { get; init; }
        public string? SupportType { get; init; }
    }

    public sealed class ResolvedFeePolicy
    {
        public int PolicyId { get; init; }
        public FeeCategory Category { get; init; }
        public FeeType FeeType { get; init; }
        public FeeScope Scope { get; init; }
        public FeeAmountMode AmountMode { get; init; }
        public FeeApplyOn ApplyOn { get; init; }
        public decimal Rate { get; init; }
        public decimal FixedAmount { get; init; }
        public decimal? MinAmount { get; init; }
        public decimal? MaxAmount { get; init; }
        public int Priority { get; init; }
        public bool IsOverride { get; init; }
        public DateTime EffectiveDate { get; init; }
        public DateTime? EndDate { get; init; }
        public ManagementFeeFrequency? Frequency { get; init; }
        public ManagementFeeRateBase? RateBase { get; init; }
        public ManagementFeeProrataMethod? ProrataMethod { get; init; }
        public ManagementFeePostingMode? PostingMode { get; init; }
        public string Source { get; init; } = string.Empty;
    }

    public sealed class OperationFeeCalculationRequest
    {
        public int ContractId { get; init; }
        public int? ProductId { get; init; }
        public int? FinancialSupportId { get; init; }
        public int? CompartmentId { get; init; }
        public string? SupportType { get; init; }
        public OperationType OperationType { get; init; }
        public DateTime OperationDate { get; init; }
        public decimal SourceAmount { get; init; }
        public decimal TargetAmount { get; init; }
    }

    public sealed class CalculatedFeeLine
    {
        public int PolicyId { get; init; }
        public decimal BaseAmount { get; init; }
        public decimal FeeAmount { get; init; }
        public FeeAmountMode AmountMode { get; init; }
        public FeeApplyOn ApplyOn { get; init; }
        public decimal Rate { get; init; }
        public decimal FixedAmount { get; init; }
        public string Source { get; init; } = string.Empty;
    }

    public sealed class ManagementFeeResolutionRequest
    {
        public int ContractId { get; init; }
        public int? ProductId { get; init; }
        public int? FinancialSupportId { get; init; }
        public int? CompartmentId { get; init; }
        public string? SupportType { get; init; }
        public DateTime AsOfDateUtc { get; init; }
    }

    public interface IFeeEngine
    {
        IReadOnlyList<ResolvedFeePolicy> Resolve(FeeResolutionRequest request);
        IReadOnlyList<CalculatedFeeLine> CalculateOperationFees(OperationFeeCalculationRequest request);
        ResolvedManagementFeePolicy? ResolveManagementFee(ManagementFeeResolutionRequest request);
    }
}
