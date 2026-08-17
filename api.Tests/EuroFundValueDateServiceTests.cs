using api.Data;
using api.Models;
using api.Models.Enum;
using api.Services.EuroFunds;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests;

public class EuroFundValueDateServiceTests
{
    [Fact]
    public void ComputeValueDate_NextBusinessDay_FromFriday_ReturnsMonday()
    {
        var service = new EuroFundValueDateService();
        var settings = new EuroFundConfiguration
        {
            ValueDateRule = EuroFundValueDateRule.NextBusinessDay,
        };

        var result = service.ComputeValueDate(new DateTime(2026, 8, 14), settings);

        Assert.Equal(new DateTime(2026, 8, 17), result);
    }

    [Fact]
    public async Task ApplyOperationAsync_EuroFundPayment_CreatesOneLotWithValueDate()
    {
        await using var context = CreateContext();
        var contract = new Contract { Id = 1, ContractNumber = "EURO-001", ContractLabel = "Euro" };
        var compartment = new Compartment { Id = 1, ContractId = 1, Contract = contract, Label = "Default" };
        var support = new FinancialSupport
        {
            Id = 1,
            Code = "EURO",
            Label = "Fonds euro",
            SupportNature = FinancialSupportNature.EuroFund,
        };
        var operation = new Operation
        {
            Id = 1,
            ContractId = 1,
            Contract = contract,
            Type = OperationType.FreePayment,
            Status = OperationStatus.Executed,
            OperationDate = new DateTime(2026, 8, 14),
            Amount = 9_900m,
            Allocations =
            {
                new OperationSupportAllocation
                {
                    OperationId = 1,
                    SupportId = 1,
                    Support = support,
                    CompartmentId = 1,
                    Compartment = compartment,
                    Amount = 9_900m,
                    Flow = OperationFlow.Target,
                },
            },
        };

        context.AddRange(contract, compartment, support, operation);
        context.EuroFundConfigurations.Add(new EuroFundConfiguration
        {
            FinancialSupportId = 1,
            ValueDateRule = EuroFundValueDateRule.NextBusinessDay,
        });
        await context.SaveChangesAsync();

        var service = new EuroFundLotService(new EuroFundValueDateService());
        await service.ApplyOperationAsync(operation, context);
        await service.ApplyOperationAsync(operation, context);
        await context.SaveChangesAsync();

        var lot = await context.EuroFundLots.SingleAsync();
        var movement = await context.EuroFundLotMovements.SingleAsync();

        Assert.Equal(9_900m, lot.InitialAmount);
        Assert.Equal(9_900m, lot.RemainingAmount);
        Assert.Equal(new DateTime(2026, 8, 17), lot.ValueDate);
        Assert.Equal(new DateTime(2026, 8, 17), movement.MovementDate);
    }

    private static ApplicationDBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDBContext(options);
    }
}
