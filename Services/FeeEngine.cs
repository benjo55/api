using api.Data;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class FeeEngine : IFeeEngine
    {
        private readonly ApplicationDBContext _context;

        public FeeEngine(ApplicationDBContext context)
        {
            _context = context;
        }

        public IReadOnlyList<ResolvedFeePolicy> Resolve(FeeResolutionRequest request)
        {
            var asOf = request.AsOfDateUtc.Date;

            var query = _context.FeePolicies
                .AsNoTracking()
                .Where(p => p.IsEnabled)
                .Where(p => p.EffectiveDate.Date <= asOf && (p.EndDate == null || p.EndDate.Value.Date >= asOf))
                .Where(p => p.Category == request.Category);

            if (request.FeeType.HasValue)
            {
                query = query.Where(p => p.FeeType == request.FeeType.Value);
            }

            query = query.Where(p =>
                (p.Scope == FeeScope.Product && p.ProductId == request.ProductId) ||
                (p.Scope == FeeScope.Contract && p.ContractId == request.ContractId) ||
                (p.Scope == FeeScope.Compartment && p.CompartmentId == request.CompartmentId) ||
                (p.Scope == FeeScope.FinancialSupport && p.FinancialSupportId == request.FinancialSupportId));

            var matched = query
                .Where(p => p.SupportType == null || p.SupportType == string.Empty || p.SupportType == request.SupportType)
                .Where(p => request.OperationAmount == null ||
                            ((p.MinAmount == null || request.OperationAmount >= p.MinAmount) &&
                             (p.MaxAmount == null || request.OperationAmount <= p.MaxAmount)))
                .ToList();

            if (!matched.Any())
            {
                return Array.Empty<ResolvedFeePolicy>();
            }

            var ordered = matched
                .OrderByDescending(p => GetScopePrecedence(p.Scope))
                .ThenBy(p => p.Priority)
                .ThenBy(p => p.Id)
                .ToList();

            var resolved = new List<ResolvedFeePolicy>();

            foreach (var group in ordered.GroupBy(p => new { p.FeeType, p.ApplyOn }))
            {
                var overridePolicies = group.Where(p => p.IsOverride).ToList();
                IEnumerable<FeePolicy> selected = group;

                if (overridePolicies.Any())
                {
                    var highestOverridePrecedence = overridePolicies.Max(p => GetScopePrecedence(p.Scope));
                    selected = overridePolicies.Where(p => GetScopePrecedence(p.Scope) == highestOverridePrecedence);
                }

                foreach (var policy in selected)
                {
                    resolved.Add(ToResolved(policy));
                }
            }

            return resolved;
        }

        public IReadOnlyList<CalculatedFeeLine> CalculateOperationFees(OperationFeeCalculationRequest request)
        {
            var feeType = MapOperationToFeeType(request.OperationType);
            var resolved = Resolve(new FeeResolutionRequest
            {
                Category = FeeCategory.Operation,
                FeeType = feeType,
                AsOfDateUtc = request.OperationDate,
                ProductId = request.ProductId,
                ContractId = request.ContractId,
                CompartmentId = request.CompartmentId,
                FinancialSupportId = request.FinancialSupportId,
                SupportType = request.SupportType,
                OperationAmount = request.SourceAmount > 0m
                    ? request.SourceAmount
                    : request.TargetAmount
            });

            var lines = new List<CalculatedFeeLine>();
            foreach (var policy in resolved)
            {
                var baseAmount = policy.ApplyOn == FeeApplyOn.Source
                    ? request.SourceAmount
                    : request.TargetAmount;

                if (baseAmount <= 0m)
                {
                    continue;
                }

                var feeAmount = policy.AmountMode == FeeAmountMode.Percentage
                    ? OperationEngineService.NumericPolicy.RoundMoney(baseAmount * policy.Rate / 100m)
                    : policy.FixedAmount;

                if (feeAmount <= 0m)
                {
                    continue;
                }

                lines.Add(new CalculatedFeeLine
                {
                    PolicyId = policy.PolicyId,
                    BaseAmount = baseAmount,
                    FeeAmount = feeAmount,
                    AmountMode = policy.AmountMode,
                    ApplyOn = policy.ApplyOn,
                    Rate = policy.Rate,
                    FixedAmount = policy.FixedAmount,
                    Source = policy.Source
                });
            }

            return lines;
        }

        public ResolvedManagementFeePolicy? ResolveManagementFee(ManagementFeeResolutionRequest request)
        {
            var resolved = Resolve(new FeeResolutionRequest
            {
                Category = FeeCategory.Management,
                FeeType = FeeType.Management,
                AsOfDateUtc = request.AsOfDateUtc,
                ProductId = request.ProductId,
                ContractId = request.ContractId,
                CompartmentId = request.CompartmentId,
                FinancialSupportId = request.FinancialSupportId,
                SupportType = request.SupportType
            });

            var first = resolved
                .OrderByDescending(x => GetScopePrecedence(x.Scope))
                .ThenBy(x => x.Priority)
                .FirstOrDefault();

            if (first == null)
            {
                return null;
            }

            return new ResolvedManagementFeePolicy
            {
                AnnualRate = ToAnnualRatePercent(first.Rate, first.RateBase ?? ManagementFeeRateBase.Annual),
                RateBase = first.RateBase ?? ManagementFeeRateBase.Annual,
                Frequency = first.Frequency ?? ManagementFeeFrequency.Monthly,
                ProrataMethod = first.ProrataMethod ?? ManagementFeeProrataMethod.Periodic,
                PostingMode = first.PostingMode ?? ManagementFeePostingMode.UnitCancellation,
                EffectiveDate = first.EffectiveDate.Date,
                EndDate = first.EndDate?.Date,
                Source = first.Source
            };
        }

        private static ResolvedFeePolicy ToResolved(FeePolicy policy)
        {
            return new ResolvedFeePolicy
            {
                PolicyId = policy.Id,
                Category = policy.Category,
                FeeType = policy.FeeType,
                Scope = policy.Scope,
                AmountMode = policy.AmountMode,
                ApplyOn = policy.ApplyOn,
                Rate = policy.Rate,
                FixedAmount = policy.FixedAmount,
                MinAmount = policy.MinAmount,
                MaxAmount = policy.MaxAmount,
                Priority = policy.Priority,
                IsOverride = policy.IsOverride,
                EffectiveDate = policy.EffectiveDate,
                EndDate = policy.EndDate,
                Frequency = policy.Frequency,
                RateBase = policy.RateBase,
                ProrataMethod = policy.ProrataMethod,
                PostingMode = policy.PostingMode,
                Source = policy.Scope.ToString()
            };
        }

        private static decimal ToAnnualRatePercent(decimal ratePercent, ManagementFeeRateBase rateBase)
        {
            if (ratePercent <= 0m)
            {
                return 0m;
            }

            var periodicRate = (double)(ratePercent / 100m);
            var periodsPerYear = rateBase switch
            {
                ManagementFeeRateBase.Monthly => 12,
                ManagementFeeRateBase.Quarterly => 4,
                ManagementFeeRateBase.SemiAnnual => 2,
                _ => 1,
            };

            var annualRate = Math.Pow(1d + periodicRate, periodsPerYear) - 1d;
            return (decimal)annualRate * 100m;
        }

        private static int GetScopePrecedence(FeeScope scope)
        {
            return scope switch
            {
                FeeScope.Contract => 400,
                FeeScope.Compartment => 300,
                FeeScope.FinancialSupport => 250,
                FeeScope.Product => 100,
                _ => 0
            };
        }

        private static FeeType MapOperationToFeeType(OperationType operationType)
        {
            return operationType switch
            {
                OperationType.InitialPayment or OperationType.FreePayment or OperationType.ScheduledPayment => FeeType.Entry,
                OperationType.Arbitrage or OperationType.ScheduledArbitrage => FeeType.Arbitrage,
                OperationType.PartialWithdrawal or OperationType.TotalWithdrawal or OperationType.ScheduledWithdrawal => FeeType.Withdrawal,
                _ => FeeType.Other
            };
        }
    }
}
