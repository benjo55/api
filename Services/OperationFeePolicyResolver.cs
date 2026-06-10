using api.Data;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class OperationFeePolicyResolver : IOperationFeePolicyResolver
    {
        private readonly ApplicationDBContext _context;
        private readonly IFeeEngine _feeEngine;

        public OperationFeePolicyResolver(ApplicationDBContext context, IFeeEngine feeEngine)
        {
            _context = context;
            _feeEngine = feeEngine;
        }

        public List<ResolvedOperationFee> ResolveOperationFees(Contract contract, FinancialSupport? support, OperationType opType, DateTime asOfDateUtc)
        {
            var result = new List<ResolvedOperationFee>();
            var mappedFeeType = MapOperationToLegacyFeeType(opType);

            var resolvedByEngine = _feeEngine.Resolve(new FeeResolutionRequest
            {
                Category = FeeCategory.Operation,
                FeeType = MapOperationToUnifiedFeeType(opType),
                AsOfDateUtc = asOfDateUtc,
                ProductId = contract.ProductId,
                ContractId = contract.Id,
                FinancialSupportId = support?.Id,
                SupportType = support?.SupportType
            });

            if (resolvedByEngine.Any())
            {
                result.AddRange(resolvedByEngine.Select(p => new ResolvedOperationFee
                {
                    FeeType = mappedFeeType,
                    Mode = p.AmountMode,
                    Rate = p.Rate,
                    FixedAmount = p.FixedAmount,
                    ApplyOn = p.ApplyOn,
                    Source = p.Source
                }));

                return result;
            }

            // Legacy fallback during transition.
            if (support?.FeeDetails != null)
            {
                var feeTypeName = mappedFeeType.ToString();
                var supportFees = support.FeeDetails
                    .Where(f => string.Equals(f.FeeType, feeTypeName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var sf in supportFees)
                {
                    result.Add(new ResolvedOperationFee
                    {
                        FeeType = mappedFeeType,
                        Mode = FeeAmountMode.Percentage,
                        Rate = sf.Rate,
                        FixedAmount = 0m,
                        ApplyOn = FeeApplyOn.Target,
                        Source = "SupportOverride"
                    });
                }
            }

            if (contract?.ProductId != null)
            {
                var prodId = contract.ProductId;
                var policies = _context.Set<ProductOperationFeePolicy>()
                    .Where(p => p.ProductId == prodId && p.IsEnabled && p.FeeType == mappedFeeType && p.EffectiveDate <= asOfDateUtc && (p.EndDate == null || p.EndDate >= asOfDateUtc))
                    .AsNoTracking()
                    .ToList();

                foreach (var p in policies)
                {
                    result.Add(new ResolvedOperationFee
                    {
                        FeeType = p.FeeType,
                        Mode = p.Mode,
                        Rate = p.Rate,
                        FixedAmount = p.FixedAmount,
                        ApplyOn = p.ApplyOn,
                        Source = "ProductPolicy"
                    });
                }
            }

            return result;
        }

        private static OperationFeeType MapOperationToLegacyFeeType(OperationType opType)
        {
            return opType switch
            {
                OperationType.InitialPayment or OperationType.FreePayment or OperationType.ScheduledPayment => OperationFeeType.Entry,
                OperationType.Arbitrage or OperationType.ScheduledArbitrage => OperationFeeType.Arbitrage,
                OperationType.PartialWithdrawal or OperationType.TotalWithdrawal or OperationType.ScheduledWithdrawal => OperationFeeType.Withdrawal,
                _ => OperationFeeType.Other
            };
        }

        private static FeeType MapOperationToUnifiedFeeType(OperationType opType)
        {
            return opType switch
            {
                OperationType.InitialPayment or OperationType.FreePayment or OperationType.ScheduledPayment => FeeType.Entry,
                OperationType.Arbitrage or OperationType.ScheduledArbitrage => FeeType.Arbitrage,
                OperationType.PartialWithdrawal or OperationType.TotalWithdrawal or OperationType.ScheduledWithdrawal => FeeType.Withdrawal,
                _ => FeeType.Other
            };
        }
    }
}
