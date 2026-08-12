using api.Data;
using api.Models;
using api.Models.Enum;
using api.Repository;
using api.Services.EuroFunds;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests;

public class OperationApplierTests
{
    [Theory]
    [InlineData(OperationType.InterestPayment)]
    [InlineData(OperationType.CouponDetachment)]
    public async Task ApplyAsync_PositionIncreaseOperations_CreateHoldingAndAllocation(OperationType operationType)
    {
        await using var context = CreateContext();
        var contract = new Contract { Id = 1, ContractNumber = "OPA-001", ContractLabel = "Applier" };
        var support = new FinancialSupport { Id = 1, Code = "SUP", Label = "Support" };
        var compartment = new Compartment { Id = 1, ContractId = 1, Contract = contract, Label = "Default" };
        var operation = new Operation
        {
            Id = 1,
            ContractId = 1,
            Contract = contract,
            Type = operationType,
            Status = OperationStatus.Executed,
            OperationDate = new DateTime(2026, 8, 12),
            Amount = 125m,
            Allocations =
            {
                new OperationSupportAllocation
                {
                    OperationId = 1,
                    SupportId = 1,
                    Support = support,
                    CompartmentId = 1,
                    Compartment = compartment,
                    Amount = 125m,
                    Shares = 10m,
                    Flow = OperationFlow.Target,
                },
            },
        };

        context.AddRange(contract, support, compartment, operation);
        await context.SaveChangesAsync();

        await new OperationApplier().ApplyAsync(operation, context);
        await context.SaveChangesAsync();

        var fsa = await context.FinancialSupportAllocations.SingleAsync();
        var holding = await context.ContractSupportHoldings.SingleAsync();

        Assert.Equal(10m, fsa.CurrentShares);
        Assert.Equal(125m, fsa.InvestedAmount);
        Assert.Equal(10m, holding.TotalShares);
        Assert.Equal(125m, holding.TotalInvested);
        Assert.Equal(12.5m, holding.Pru);
    }

    [Fact]
    public async Task ApplyAsync_ParticipationBenefitOnEuroFund_CreatesCapitalizedLot()
    {
        await using var context = CreateContext();
        var contract = new Contract { Id = 1, ContractNumber = "OPA-PB", ContractLabel = "PB" };
        var support = new FinancialSupport
        {
            Id = 1,
            Code = "EURO",
            Label = "Fonds euro",
            SupportNature = FinancialSupportNature.EuroFund
        };
        var compartment = new Compartment { Id = 1, ContractId = 1, Contract = contract, Label = "Default" };
        var operation = new Operation
        {
            Id = 1,
            ContractId = 1,
            Contract = contract,
            Type = OperationType.ParticipationBenefit,
            Status = OperationStatus.Executed,
            OperationDate = new DateTime(2026, 12, 31),
            Amount = 3_000m,
            Allocations =
            {
                new OperationSupportAllocation
                {
                    OperationId = 1,
                    SupportId = 1,
                    Support = support,
                    CompartmentId = 1,
                    Compartment = compartment,
                    Amount = 3_000m,
                    Flow = OperationFlow.Target,
                },
            },
        };

        context.AddRange(contract, support, compartment, operation);
        await context.SaveChangesAsync();

        await new OperationApplier(new EuroFundLotService()).ApplyAsync(operation, context);
        await context.SaveChangesAsync();

        var lot = await context.EuroFundLots.SingleAsync();
        var movement = await context.EuroFundLotMovements.SingleAsync();
        var holding = await context.ContractSupportHoldings.SingleAsync();

        Assert.Equal(3_000m, lot.InitialAmount);
        Assert.Equal(3_000m, lot.RemainingAmount);
        Assert.Equal(EuroFundLotMovementType.ProfitParticipation, movement.MovementType);
        Assert.Equal(3_000m, holding.TotalShares);
        Assert.Equal(1m, holding.Pru);
    }

    private static ApplicationDBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDBContext(options);
    }
}
