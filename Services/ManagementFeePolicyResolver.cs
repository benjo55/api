using api.Interfaces;
using api.Models;
using api.Models.Enum;

namespace api.Services
{
    public class ManagementFeePolicyResolver : IManagementFeePolicyResolver
    {
        private readonly IFeeEngine _feeEngine;

        public ManagementFeePolicyResolver(IFeeEngine feeEngine)
        {
            _feeEngine = feeEngine;
        }

        public ResolvedManagementFeePolicy? Resolve(Contract contract, FinancialSupport support, DateTime asOfDateUtc)
        {
            var resolvedByEngine = _feeEngine.ResolveManagementFee(new ManagementFeeResolutionRequest
            {
                ContractId = contract.Id,
                ProductId = contract.ProductId,
                FinancialSupportId = support.Id,
                SupportType = support.SupportType,
                AsOfDateUtc = asOfDateUtc
            });

            if (resolvedByEngine != null)
            {
                return resolvedByEngine;
            }

            if (support.ContractManagementFeeOverrideEnabled &&
                support.ContractManagementFeeOverrideRate is decimal supportRate &&
                supportRate > 0m)
            {
                var effectiveDate = support.ContractManagementFeeOverrideEffectiveDate?.Date ?? asOfDateUtc.Date;
                var endDate = support.ContractManagementFeeOverrideEndDate?.Date;

                if (effectiveDate <= asOfDateUtc.Date && (endDate == null || endDate >= asOfDateUtc.Date))
                {
                    return new ResolvedManagementFeePolicy
                    {
                        AnnualRate = supportRate,
                        RateBase = ManagementFeeRateBase.Annual,
                        Frequency = support.ContractManagementFeeOverrideFrequency ?? ManagementFeeFrequency.Monthly,
                        ProrataMethod = support.ContractManagementFeeOverrideProrataMethod ?? ManagementFeeProrataMethod.Periodic,
                        PostingMode = support.ContractManagementFeeOverridePostingMode
                            ?? ManagementFeePostingMode.UnitCancellation,
                        EffectiveDate = effectiveDate,
                        EndDate = endDate,
                        Source = "SupportOverride"
                    };
                }
            }

            var productPolicy = contract.Product?.ManagementFeePolicy;
            if (productPolicy != null &&
                productPolicy.IsEnabled &&
                productPolicy.AnnualRate > 0m &&
                productPolicy.EffectiveDate.Date <= asOfDateUtc.Date &&
                (productPolicy.EndDate == null || productPolicy.EndDate.Value.Date >= asOfDateUtc.Date))
            {
                return new ResolvedManagementFeePolicy
                {
                    AnnualRate = productPolicy.AnnualRate,
                    RateBase = ManagementFeeRateBase.Annual,
                    Frequency = productPolicy.Frequency,
                    ProrataMethod = productPolicy.ProrataMethod,
                    PostingMode = productPolicy.PostingMode,
                    EffectiveDate = productPolicy.EffectiveDate.Date,
                    EndDate = productPolicy.EndDate?.Date,
                    Source = "ProductPolicy"
                };
            }

            if (contract.ManagementFeesRate is decimal legacyRate && legacyRate > 0m)
            {
                return new ResolvedManagementFeePolicy
                {
                    AnnualRate = legacyRate,
                    RateBase = ManagementFeeRateBase.Annual,
                    Frequency = ManagementFeeFrequency.Monthly,
                    ProrataMethod = ManagementFeeProrataMethod.Periodic,
                    PostingMode = ManagementFeePostingMode.UnitCancellation,
                    EffectiveDate = contract.DateEffect.Date,
                    Source = "LegacyContract"
                };
            }

            return null;
        }
    }
}