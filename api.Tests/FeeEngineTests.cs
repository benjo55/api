using api.Data;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests;

public class FeeEngineTests : IDisposable
{
    private readonly ApplicationDBContext _db;
    private readonly FeeEngine _engine;

    public FeeEngineTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDBContext(options);
        _engine = new FeeEngine(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task<FeePolicy> AddPolicy(
        FeeCategory category,
        FeeType feeType,
        FeeScope scope,
        decimal rate = 1m,
        bool isOverride = false,
        bool isEnabled = true,
        int priority = 100,
        int? productId = null,
        int? contractId = null,
        int? compartmentId = null,
        int? financialSupportId = null,
        string? supportType = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        DateTime? effectiveDate = null,
        DateTime? endDate = null)
    {
        var policy = new FeePolicy
        {
            Category = category,
            FeeType = feeType,
            Scope = scope,
            Rate = rate,
            FixedAmount = 0m,
            AmountMode = FeeAmountMode.Percentage,
            ApplyOn = FeeApplyOn.Target,
            IsOverride = isOverride,
            IsEnabled = isEnabled,
            Priority = priority,
            ProductId = productId,
            ContractId = contractId,
            CompartmentId = compartmentId,
            FinancialSupportId = financialSupportId,
            SupportType = supportType,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            EffectiveDate = (effectiveDate ?? DateTime.UtcNow.AddDays(-1)).Date,
            EndDate = endDate?.Date,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
        };
        _db.FeePolicies.Add(policy);
        await _db.SaveChangesAsync();
        return policy;
    }

    private FeeResolutionRequest MakeRequest(
        FeeCategory category = FeeCategory.Operation,
        FeeType feeType = FeeType.Entry,
        int? productId = null,
        int? contractId = null,
        decimal? operationAmount = null,
        string? supportType = null)
        => new()
        {
            Category = category,
            FeeType = feeType,
            AsOfDateUtc = DateTime.UtcNow,
            ProductId = productId,
            ContractId = contractId,
            OperationAmount = operationAmount,
            SupportType = supportType,
        };

    // ─── Tests ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_ProductScope_ReturnsMatchingPolicy()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 2m, productId: 1);

        var result = _engine.Resolve(MakeRequest(productId: 1));

        Assert.Single(result);
        Assert.Equal(2m, result[0].Rate);
        Assert.Equal(FeeScope.Product, result[0].Scope);
    }

    [Fact]
    public async Task Resolve_ContractScope_TakesPrecedenceOverProduct()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 2m, productId: 1);
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Contract, rate: 0.5m, contractId: 10, productId: 1);

        var result = _engine.Resolve(MakeRequest(productId: 1, contractId: 10));

        // Both are returned (no override flag); contract scope has higher precedence in ordering
        Assert.Equal(2, result.Count);
        Assert.Equal(FeeScope.Contract, result[0].Scope);
    }

    [Fact]
    public async Task Resolve_ContractOverride_ReplacesProductPolicy()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 2m, productId: 1);
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Contract, rate: 0m, contractId: 10, productId: 1, isOverride: true);

        var result = _engine.Resolve(MakeRequest(productId: 1, contractId: 10));

        // The contract override should suppress the product policy for this FeeType+ApplyOn group
        Assert.Single(result);
        Assert.Equal(FeeScope.Contract, result[0].Scope);
        Assert.Equal(0m, result[0].Rate);
    }

    [Fact]
    public async Task Resolve_DisabledPolicy_NotReturned()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 2m, productId: 1, isEnabled: false);

        var result = _engine.Resolve(MakeRequest(productId: 1));

        Assert.Empty(result);
    }

    [Fact]
    public async Task Resolve_ExpiredPolicy_NotReturned()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 2m, productId: 1,
            effectiveDate: DateTime.UtcNow.AddDays(-30),
            endDate: DateTime.UtcNow.AddDays(-1));

        var result = _engine.Resolve(MakeRequest(productId: 1));

        Assert.Empty(result);
    }

    [Fact]
    public async Task Resolve_FuturePolicy_NotReturned()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 2m, productId: 1,
            effectiveDate: DateTime.UtcNow.AddDays(5));

        var result = _engine.Resolve(MakeRequest(productId: 1));

        Assert.Empty(result);
    }

    [Fact]
    public async Task Resolve_MinAmountCondition_FilteredOut_WhenAmountTooSmall()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 1m, productId: 1,
            minAmount: 50_000m);

        var result = _engine.Resolve(MakeRequest(productId: 1, operationAmount: 10_000m));

        Assert.Empty(result);
    }

    [Fact]
    public async Task Resolve_MinAmountCondition_Matched_WhenAmountAboveThreshold()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 1m, productId: 1,
            minAmount: 50_000m);

        var result = _engine.Resolve(MakeRequest(productId: 1, operationAmount: 60_000m));

        Assert.Single(result);
    }

    [Fact]
    public async Task Resolve_MaxAmountCondition_FilteredOut_WhenAmountTooLarge()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 2m, productId: 1,
            maxAmount: 9_999m);

        var result = _engine.Resolve(MakeRequest(productId: 1, operationAmount: 10_000m));

        Assert.Empty(result);
    }

    [Fact]
    public async Task Resolve_RangeCondition_TieredRates()
    {
        // 2 % pour < 50 000 €, 1 % pour >= 50 000 €
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 2m, productId: 1,
            maxAmount: 49_999.99m, priority: 10);
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 1m, productId: 1,
            minAmount: 50_000m, priority: 20);

        var small = _engine.Resolve(MakeRequest(productId: 1, operationAmount: 10_000m));
        var large = _engine.Resolve(MakeRequest(productId: 1, operationAmount: 80_000m));

        Assert.Single(small);
        Assert.Equal(2m, small[0].Rate);

        Assert.Single(large);
        Assert.Equal(1m, large[0].Rate);
    }

    [Fact]
    public async Task Resolve_SupportType_FiltersCorrectly()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 0.5m, productId: 1,
            supportType: "SCPI");
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 0m, productId: 1,
            supportType: "ETF");

        var scpi = _engine.Resolve(MakeRequest(productId: 1, supportType: "SCPI"));
        var etf = _engine.Resolve(MakeRequest(productId: 1, supportType: "ETF"));
        var other = _engine.Resolve(MakeRequest(productId: 1, supportType: "UC"));

        Assert.Single(scpi);
        Assert.Equal(0.5m, scpi[0].Rate);

        Assert.Single(etf);
        Assert.Equal(0m, etf[0].Rate);

        Assert.Empty(other);
    }

    [Fact]
    public async Task Resolve_Priority_LowerValueWins_WhenBothOverride()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Contract, rate: 1.5m,
            contractId: 10, isOverride: true, priority: 50);
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Contract, rate: 0.8m,
            contractId: 10, isOverride: true, priority: 10);

        var result = _engine.Resolve(MakeRequest(contractId: 10));

        // Both are Contract overrides for the same FeeType+ApplyOn - both returned but ordered by priority
        Assert.Equal(2, result.Count);
        Assert.Equal(0.8m, result[0].Rate); // priority 10 first
        Assert.Equal(1.5m, result[1].Rate);
    }

    [Fact]
    public async Task CalculateOperationFees_ReturnsCorrectAmount()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 2m, productId: 1);

        var lines = _engine.CalculateOperationFees(new OperationFeeCalculationRequest
        {
            ContractId = 99,
            ProductId = 1,
            OperationType = OperationType.InitialPayment,
            OperationDate = DateTime.UtcNow,
            TargetAmount = 10_000m,
            SourceAmount = 0m,
        });

        Assert.Single(lines);
        Assert.Equal(200m, lines[0].FeeAmount);  // 2 % de 10 000 = 200
        Assert.Equal(10_000m, lines[0].BaseAmount);
    }

    [Fact]
    public async Task CalculateOperationFees_ZeroRate_ReturnsNoLine()
    {
        await AddPolicy(FeeCategory.Operation, FeeType.Entry, FeeScope.Product, rate: 0m, productId: 1);

        var lines = _engine.CalculateOperationFees(new OperationFeeCalculationRequest
        {
            ContractId = 99,
            ProductId = 1,
            OperationType = OperationType.InitialPayment,
            OperationDate = DateTime.UtcNow,
            TargetAmount = 10_000m,
            SourceAmount = 0m,
        });

        Assert.Empty(lines);
    }

    [Fact]
    public async Task ResolveManagementFee_ReturnsHighestScopeFirst()
    {
        await AddPolicy(FeeCategory.Management, FeeType.Management, FeeScope.Product, rate: 0.9m,
            productId: 1);
        await AddPolicy(FeeCategory.Management, FeeType.Management, FeeScope.Contract, rate: 0.7m,
            contractId: 10, productId: 1);

        var result = _engine.ResolveManagementFee(new ManagementFeeResolutionRequest
        {
            ContractId = 10,
            ProductId = 1,
            AsOfDateUtc = DateTime.UtcNow,
        });

        Assert.NotNull(result);
        Assert.Equal(0.7m, result!.AnnualRate);
        Assert.Equal("Contract", result.Source);
    }

    [Fact]
    public async Task ResolveManagementFee_ReturnsNull_WhenNoPolicy()
    {
        var result = _engine.ResolveManagementFee(new ManagementFeeResolutionRequest
        {
            ContractId = 1,
            ProductId = 1,
            AsOfDateUtc = DateTime.UtcNow,
        });

        Assert.Null(result);
    }
}
