using api.Data;
using api.Models;
using api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests;

public class CostBasisServiceTests
{
    [Fact]
    public async Task RebuildAsync_ReplaysPaymentsWithdrawalsArbitragesAndFees()
    {
        await using var context = CreateContext();
        var contract = new Contract { Id = 1, ContractNumber = "CB-001", ContractLabel = "Cost basis" };
        var source = new FinancialSupport { Id = 1, Code = "SRC", Label = "Source" };
        var target = new FinancialSupport { Id = 2, Code = "TGT", Label = "Target" };
        var compartment = new Compartment { Id = 1, ContractId = 1, Contract = contract, Label = "Global" };

        context.AddRange(contract, source, target, compartment);
        context.Operations.AddRange(
            Operation(1, OperationType.FreePayment, Allocation(source, compartment, 100m, 10m)),
            Operation(2, OperationType.PartialWithdrawal, Allocation(source, compartment, 30m, 2m)),
            Operation(3, OperationType.Arbitrage,
                Allocation(source, compartment, 45m, 3m, OperationFlow.Source),
                Allocation(target, compartment, 45m, 5m, OperationFlow.Target)),
            Operation(4, OperationType.ManagementFee, Allocation(source, compartment, 10m, 1m)));

        context.FinancialSupportAllocations.AddRange(
            new FinancialSupportAllocation
            {
                Contract = contract,
                ContractId = 1,
                Support = source,
                SupportId = 1,
                Compartment = compartment,
                CompartmentId = 1,
                CurrentShares = 4m,
            },
            new FinancialSupportAllocation
            {
                Contract = contract,
                ContractId = 1,
                Support = target,
                SupportId = 2,
                Compartment = compartment,
                CompartmentId = 1,
                CurrentShares = 5m,
            });
        context.ContractSupportHoldings.AddRange(
            new ContractSupportHolding { Contract = contract, ContractId = 1, Support = source, SupportId = 1, CompartmentId = 1, TotalShares = 4m },
            new ContractSupportHolding { Contract = contract, ContractId = 1, Support = target, SupportId = 2, CompartmentId = 1, TotalShares = 5m });
        await context.SaveChangesAsync();

        var result = await new CostBasisService(context).RebuildAsync(1);

        var sourceFsa = await context.FinancialSupportAllocations.SingleAsync(f => f.SupportId == 1);
        var targetFsa = await context.FinancialSupportAllocations.SingleAsync(f => f.SupportId == 2);
        var sourceHolding = await context.ContractSupportHoldings.SingleAsync(h => h.SupportId == 1);
        var targetHolding = await context.ContractSupportHoldings.SingleAsync(h => h.SupportId == 2);

        Assert.Equal(40m, sourceFsa.InvestedAmount);
        Assert.Equal(45m, targetFsa.InvestedAmount);
        Assert.Equal(10m, sourceHolding.Pru);
        Assert.Equal(9m, targetHolding.Pru);
        Assert.Equal(85m, result.TotalRemainingCostBasis);
        Assert.Equal(0m, result.MaximumShareDelta);
    }

    private static Operation Operation(int id, OperationType type, params OperationSupportAllocation[] allocations) =>
        new()
        {
            Id = id,
            ContractId = 1,
            Type = type,
            Status = OperationStatus.Executed,
            OperationDate = new DateTime(2026, 1, id),
            ExecutionDate = new DateTime(2026, 1, id),
            Amount = type == OperationType.Arbitrage ? 45m : allocations.Sum(a => a.Amount ?? 0m),
            Allocations = allocations,
        };

    private static OperationSupportAllocation Allocation(
        FinancialSupport support,
        Compartment compartment,
        decimal amount,
        decimal shares,
        OperationFlow? flow = null) =>
        new()
        {
            Support = support,
            SupportId = support.Id,
            Compartment = compartment,
            CompartmentId = compartment.Id,
            Amount = amount,
            Shares = shares,
            Flow = flow,
        };

    private static ApplicationDBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDBContext(options);
    }
}
