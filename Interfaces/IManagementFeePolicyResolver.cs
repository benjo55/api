using api.Models;

namespace api.Interfaces
{
    public sealed class ResolvedManagementFeePolicy
    {
        public decimal AnnualRate { get; init; }
        public Models.Enum.ManagementFeeRateBase RateBase { get; init; } = Models.Enum.ManagementFeeRateBase.Annual;
        public string Source { get; init; } = string.Empty;
        public Models.Enum.ManagementFeeFrequency Frequency { get; init; }
        public Models.Enum.ManagementFeeProrataMethod ProrataMethod { get; init; }
        public Models.Enum.ManagementFeePostingMode PostingMode { get; init; }
        public DateTime EffectiveDate { get; init; }
        public DateTime? EndDate { get; init; }
    }

    public interface IManagementFeePolicyResolver
    {
        ResolvedManagementFeePolicy? Resolve(Contract contract, FinancialSupport support, DateTime asOfDateUtc);
    }
}