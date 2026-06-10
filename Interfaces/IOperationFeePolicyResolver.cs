using api.Models;
using api.Models.Enum;

namespace api.Interfaces
{
    public sealed class ResolvedOperationFee
    {
        public OperationFeeType FeeType { get; init; }
        public FeeAmountMode Mode { get; init; }
        public decimal Rate { get; init; }
        public decimal FixedAmount { get; init; }
        public FeeApplyOn ApplyOn { get; init; }
        public string Source { get; init; } = string.Empty;
    }

    public interface IOperationFeePolicyResolver
    {
        /// <summary>
        /// Resolve applicable operation fee policies for a given contract/support/operation type at a given date.
        /// Returns zero or more resolved fee rules (support overrides first, then product-level rules).
        /// </summary>
        List<ResolvedOperationFee> ResolveOperationFees(Contract contract, FinancialSupport? support, OperationType opType, DateTime asOfDateUtc);
    }
}
