using api.Models;
using api.Models.Enum;
using api.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace api.Tests;

[TestClass]
public class ManagementFeePolicyResolverTests
{
    private readonly ManagementFeePolicyResolver _resolver = new();

    [TestMethod]
    public void Resolve_UsesSupportOverrideBeforeProductAndLegacy()
    {
        var asOfDate = new DateTime(2026, 05, 13);
        var contract = CreateContract(
            legacyRate: 0.90m,
            productRate: 0.80m,
            productEnabled: true,
            productEffectiveDate: new DateTime(2026, 01, 01));
        var support = CreateSupport(
            overrideRate: 0.70m,
            overrideEnabled: true,
            overrideEffectiveDate: new DateTime(2026, 02, 01));

        var resolved = _resolver.Resolve(contract, support, asOfDate);

        Assert.IsNotNull(resolved);
        Assert.AreEqual(0.70m, resolved!.AnnualRate);
        Assert.AreEqual("SupportOverride", resolved.Source);
        Assert.AreEqual(ManagementFeeFrequency.Monthly, resolved.Frequency);
        Assert.AreEqual(ManagementFeeProrataMethod.Periodic, resolved.ProrataMethod);
        Assert.AreEqual(ManagementFeePostingMode.UnitCancellation, resolved.PostingMode);
    }

    [TestMethod]
    public void Resolve_FallsBackToProductPolicyWhenNoSupportOverride()
    {
        var asOfDate = new DateTime(2026, 05, 13);
        var contract = CreateContract(
            legacyRate: 0.90m,
            productRate: 0.80m,
            productEnabled: true,
            productEffectiveDate: new DateTime(2026, 01, 01),
            productFrequency: ManagementFeeFrequency.Quarterly,
            productProrataMethod: ManagementFeeProrataMethod.Actual365,
            productPostingMode: ManagementFeePostingMode.NetServedYield);
        var support = CreateSupport();

        var resolved = _resolver.Resolve(contract, support, asOfDate);

        Assert.IsNotNull(resolved);
        Assert.AreEqual(0.80m, resolved!.AnnualRate);
        Assert.AreEqual("ProductPolicy", resolved.Source);
        Assert.AreEqual(ManagementFeeFrequency.Quarterly, resolved.Frequency);
        Assert.AreEqual(ManagementFeeProrataMethod.Actual365, resolved.ProrataMethod);
        Assert.AreEqual(ManagementFeePostingMode.NetServedYield, resolved.PostingMode);
    }

    [TestMethod]
    public void Resolve_FallsBackToLegacyContractWhenNoOverrideOrProductPolicy()
    {
        var asOfDate = new DateTime(2026, 05, 13);
        var contract = CreateContract(legacyRate: 0.90m);
        var support = CreateSupport();

        var resolved = _resolver.Resolve(contract, support, asOfDate);

        Assert.IsNotNull(resolved);
        Assert.AreEqual(0.90m, resolved!.AnnualRate);
        Assert.AreEqual("LegacyContract", resolved.Source);
        Assert.AreEqual(contract.DateEffect.Date, resolved.EffectiveDate);
    }

    [TestMethod]
    public void Resolve_IgnoresExpiredOrDisabledSupportOverride()
    {
        var asOfDate = new DateTime(2026, 05, 13);
        var contract = CreateContract(
            legacyRate: 0.90m,
            productRate: 0.80m,
            productEnabled: true,
            productEffectiveDate: new DateTime(2026, 01, 01));
        var support = CreateSupport(
            overrideRate: 0.70m,
            overrideEnabled: true,
            overrideEffectiveDate: new DateTime(2026, 01, 01),
            overrideEndDate: new DateTime(2026, 03, 31));

        var resolved = _resolver.Resolve(contract, support, asOfDate);

        Assert.IsNotNull(resolved);
        Assert.AreEqual(0.80m, resolved!.AnnualRate);
        Assert.AreEqual("ProductPolicy", resolved.Source);
    }

    private static Contract CreateContract(
        decimal? legacyRate = null,
        decimal? productRate = null,
        bool productEnabled = false,
        DateTime? productEffectiveDate = null,
        DateTime? productEndDate = null,
        ManagementFeeFrequency productFrequency = ManagementFeeFrequency.Monthly,
        ManagementFeeProrataMethod productProrataMethod = ManagementFeeProrataMethod.Periodic,
        ManagementFeePostingMode productPostingMode = ManagementFeePostingMode.UnitCancellation)
    {
        return new Contract
        {
            DateEffect = new DateTime(2025, 01, 01),
            ManagementFeesRate = legacyRate,
            Product = productRate.HasValue
                ? new Product
                {
                    ManagementFeePolicy = new ProductManagementFeePolicy
                    {
                        AnnualRate = productRate.Value,
                        Frequency = productFrequency,
                        ProrataMethod = productProrataMethod,
                        PostingMode = productPostingMode,
                        EffectiveDate = productEffectiveDate ?? new DateTime(2025, 01, 01),
                        EndDate = productEndDate,
                        IsEnabled = productEnabled,
                    }
                }
                : null,
        };
    }

    private static FinancialSupport CreateSupport(
        decimal? overrideRate = null,
        bool overrideEnabled = false,
        DateTime? overrideEffectiveDate = null,
        DateTime? overrideEndDate = null)
    {
        return new FinancialSupport
        {
            ContractManagementFeeOverrideEnabled = overrideEnabled,
            ContractManagementFeeOverrideRate = overrideRate,
            ContractManagementFeeOverrideFrequency = ManagementFeeFrequency.Monthly,
            ContractManagementFeeOverrideProrataMethod = ManagementFeeProrataMethod.Periodic,
            ContractManagementFeeOverridePostingMode = ManagementFeePostingMode.UnitCancellation,
            ContractManagementFeeOverrideEffectiveDate = overrideEffectiveDate,
            ContractManagementFeeOverrideEndDate = overrideEndDate,
        };
    }
}