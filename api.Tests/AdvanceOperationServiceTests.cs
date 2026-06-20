using api.Data;
using api.Models;
using api.Models.Enum;
using api.Repository;
using api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests;

public class AdvanceOperationServiceTests
{
    [Fact]
    public async Task ApplyDisbursement_CreatesMovementAndActivatesAdvance()
    {
        await using var context = CreateContext();
        var contract = new Contract
        {
            Id = 1,
            ContractNumber = "TEST-001",
            ContractLabel = "Test"
        };
        var support = new FinancialSupport
        {
            Id = 1,
            Code = "EURO",
            Label = "Fonds Euro",
            SupportNature = FinancialSupportNature.EuroFund
        };
        var advance = new Advance
        {
            Contract = contract,
            ContractId = contract.Id,
            AdvanceNumber = "AV-TEST-001",
            RequestedAmount = 500m,
            ApprovedAmount = 500m,
            OutstandingCapital = 0m,
            InterestRate = 4m,
            DurationMonths = 36,
            Status = AdvanceStatus.Approved
        };
        context.AddRange(
            advance,
            new FinancialSupportAllocation
            {
                ContractId = contract.Id,
                Support = support,
                SupportId = support.Id,
                CompartmentId = 1,
                AllocationPercentage = 100m,
                CurrentAmount = 1_000m
            });
        await context.SaveChangesAsync();

        var operation = new Operation
        {
            ContractId = contract.Id,
            Type = OperationType.Advance,
            Status = OperationStatus.Pending,
            OperationDate = DateTime.UtcNow,
            Amount = 500m,
            Currency = "EUR",
            AdvanceDetail = new AdvanceDetail
            {
                AdvanceId = advance.Id,
                TransactionType = AdvanceTransactionType.Disbursement,
                Amount = 500m,
                InterestRate = advance.InterestRate,
                MaturityDate = DateTime.UtcNow.AddMonths(36)
            }
        };
        var service = new AdvanceOperationService(context, new AdvanceRepository(context));

        await service.ValidateForCreationAsync(operation);
        context.Add(operation);
        await context.SaveChangesAsync();
        await service.ApplyAsync(operation);
        await context.SaveChangesAsync();

        Assert.Equal(500m, advance.OutstandingCapital);
        Assert.Equal(AdvanceStatus.Active, advance.Status);
        Assert.Equal(
            AdvanceTransactionType.Disbursement,
            Assert.Single(context.AdvanceTransactions).Type);
    }

    [Fact]
    public async Task ApplyRepayment_UpdatesOutstandingAndIsIdempotent()
    {
        await using var context = CreateContext();
        var advance = await SeedActiveAdvanceAsync(context, 1_000m);
        var operation = await SeedRepaymentAsync(
            context,
            advance,
            400m,
            AdvanceTransactionType.PartialRepayment);
        var service = new AdvanceOperationService(context, new AdvanceRepository(context));

        await service.ApplyAsync(operation);
        await context.SaveChangesAsync();
        await service.ApplyAsync(operation);
        await context.SaveChangesAsync();

        Assert.Equal(600m, advance.OutstandingCapital);
        Assert.Equal(AdvanceStatus.Active, advance.Status);
        var movement = Assert.Single(context.AdvanceTransactions);
        Assert.Equal(operation.Id, movement.OperationId);
        Assert.Equal(AdvanceTransactionType.PartialRepayment, movement.Type);
    }

    [Fact]
    public async Task ApplyTotalRepayment_SettlesAdvance()
    {
        await using var context = CreateContext();
        var advance = await SeedActiveAdvanceAsync(context, 750m);
        var operation = await SeedRepaymentAsync(
            context,
            advance,
            750m,
            AdvanceTransactionType.TotalRepayment);
        var service = new AdvanceOperationService(context, new AdvanceRepository(context));

        await service.ApplyAsync(operation);
        await context.SaveChangesAsync();

        Assert.Equal(0m, advance.OutstandingCapital);
        Assert.Equal(AdvanceStatus.Settled, advance.Status);
        Assert.Equal(
            AdvanceTransactionType.TotalRepayment,
            Assert.Single(context.AdvanceTransactions).Type);
    }

    private static ApplicationDBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDBContext(options);
    }

    private static async Task<Advance> SeedActiveAdvanceAsync(
        ApplicationDBContext context,
        decimal outstanding)
    {
        var contract = new Contract
        {
            Id = 1,
            ContractNumber = "TEST-001",
            ContractLabel = "Test"
        };
        var advance = new Advance
        {
            Contract = contract,
            ContractId = contract.Id,
            AdvanceNumber = "AV-TEST-001",
            RequestedAmount = outstanding,
            ApprovedAmount = outstanding,
            OutstandingCapital = outstanding,
            InterestRate = 4m,
            DurationMonths = 36,
            Status = AdvanceStatus.Active
        };

        context.Add(advance);
        await context.SaveChangesAsync();
        return advance;
    }

    private static async Task<Operation> SeedRepaymentAsync(
        ApplicationDBContext context,
        Advance advance,
        decimal amount,
        AdvanceTransactionType transactionType)
    {
        var operation = new Operation
        {
            ContractId = advance.ContractId,
            Type = OperationType.AdvanceRepayment,
            Status = OperationStatus.Pending,
            OperationDate = DateTime.UtcNow,
            Amount = amount,
            Currency = "EUR",
            AdvanceDetail = new AdvanceDetail
            {
                AdvanceId = advance.Id,
                TransactionType = transactionType,
                Amount = amount,
                InterestRate = advance.InterestRate,
                MaturityDate = advance.MaturityDate ?? DateTime.UtcNow
            }
        };

        context.Add(operation);
        await context.SaveChangesAsync();
        return operation;
    }
}
