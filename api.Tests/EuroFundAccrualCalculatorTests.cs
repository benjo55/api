using api.Models;
using api.Services.EuroFunds;
using Xunit;

namespace api.Tests;

public class EuroFundAccrualCalculatorTests
{
    [Fact]
    public void FullYearExposure_ComputesAnnualInterest()
    {
        var result = Calculate(2026, 3m, Lot(1, 100_000m, new DateTime(2026, 1, 1)));

        Assert.Equal(365, result.YearBasis);
        Assert.Equal(100_000m, result.WeightedExposure);
        Assert.Equal(3_000m, result.InterestAmount);
    }

    [Fact]
    public void SeveralPayments_RebuildsExpectedSegments()
    {
        var lot = Lot(1, 100_000m, new DateTime(2026, 1, 1));
        var movements = Movements(
            Movement(lot, 100_000m, new DateTime(2026, 1, 1)),
            Movement(lot, 20_000m, new DateTime(2026, 4, 1)),
            Movement(lot, 30_000m, new DateTime(2026, 9, 1)));

        var result = new EuroFundAccrualCalculator().Calculate([lot], movements, 2026, 3m);

        Assert.Collection(result.Details,
            d =>
            {
                Assert.Equal(new DateTime(2026, 1, 1), d.PeriodStart);
                Assert.Equal(new DateTime(2026, 4, 1), d.PeriodEnd);
                Assert.Equal(90, d.DayCount);
                Assert.Equal(100_000m, d.OpeningAmount);
            },
            d =>
            {
                Assert.Equal(new DateTime(2026, 4, 1), d.PeriodStart);
                Assert.Equal(new DateTime(2026, 9, 1), d.PeriodEnd);
                Assert.Equal(153, d.DayCount);
                Assert.Equal(120_000m, d.OpeningAmount);
            },
            d =>
            {
                Assert.Equal(new DateTime(2026, 9, 1), d.PeriodStart);
                Assert.Equal(new DateTime(2027, 1, 1), d.PeriodEnd);
                Assert.Equal(122, d.DayCount);
                Assert.Equal(150_000m, d.OpeningAmount);
            });
        Assert.Equal(3_752.88m, result.InterestAmount);
    }

    [Fact]
    public void PartialWithdrawal_ReducesExposureFromValueDate()
    {
        var lot = Lot(1, 100_000m, new DateTime(2026, 1, 1));
        var result = Calculate(2026, 3m, lot,
            Movement(lot, 100_000m, new DateTime(2026, 1, 1)),
            Movement(lot, -40_000m, new DateTime(2026, 7, 1)));

        Assert.Contains(result.Details, d => d.OpeningAmount == 60_000m && d.PeriodStart == new DateTime(2026, 7, 1));
        Assert.True(result.InterestAmount < 3_000m);
    }

    [Fact]
    public void IncomingArbitrage_IncreasesExposureFromValueDate()
    {
        var lot = Lot(1, 50_000m, new DateTime(2026, 1, 1));
        var result = Calculate(2026, 3m, lot,
            Movement(lot, 50_000m, new DateTime(2026, 1, 1)),
            Movement(lot, 25_000m, new DateTime(2026, 6, 1)));

        Assert.Contains(result.Details, d => d.OpeningAmount == 75_000m && d.PeriodStart == new DateTime(2026, 6, 1));
    }

    [Fact]
    public void OutgoingArbitrage_DecreasesExposureFromValueDate()
    {
        var lot = Lot(1, 50_000m, new DateTime(2026, 1, 1));
        var result = Calculate(2026, 3m, lot,
            Movement(lot, 50_000m, new DateTime(2026, 1, 1)),
            Movement(lot, -15_000m, new DateTime(2026, 10, 1)));

        Assert.Contains(result.Details, d => d.OpeningAmount == 35_000m && d.PeriodStart == new DateTime(2026, 10, 1));
    }

    [Fact]
    public void Bonus_AppliesPerLot()
    {
        var oldLot = Lot(1, 100_000m, new DateTime(2026, 1, 1));
        var bonusLot = Lot(2, 50_000m, new DateTime(2026, 4, 1), bonusRate: 1m);

        var result = new EuroFundAccrualCalculator().Calculate(
            [oldLot, bonusLot],
            Movements(
                Movement(oldLot, 100_000m, new DateTime(2026, 1, 1)),
                Movement(bonusLot, 50_000m, new DateTime(2026, 4, 1))),
            2026,
            3m);

        Assert.Contains(result.Details, d => d.LotId == 2 && d.ApplicableRate == 4m);
        Assert.Equal(
            Math.Round(100_000m * 3m / 100m + 50_000m * 4m / 100m * 275m / 365m, 2, MidpointRounding.AwayFromZero),
            result.InterestAmount);
    }

    [Fact]
    public void LeapYear_Uses366DayBasis()
    {
        var result = Calculate(2028, 3m, Lot(1, 100_000m, new DateTime(2028, 1, 1)));

        Assert.Equal(366, result.YearBasis);
        Assert.Equal(3_000m, result.InterestAmount);
    }

    [Fact]
    public void ProfitParticipationCapitalized_ProducesInterestNextYear()
    {
        var lot = Lot(1, 100_000m, new DateTime(2026, 1, 1));
        var pbLot = Lot(2, 3_000m, new DateTime(2026, 12, 31));

        var result2027 = new EuroFundAccrualCalculator().Calculate(
            [lot, pbLot],
            Movements(
                Movement(lot, 100_000m, new DateTime(2026, 1, 1)),
                Movement(pbLot, 3_000m, new DateTime(2026, 12, 31))),
            2027,
            3m);

        Assert.Equal(103_000m, result2027.BookValue);
        Assert.Equal(3_090m, result2027.InterestAmount);
    }

    [Fact]
    public void LaterPayment_NeverProducesMoreInterestThanEarlierPayment()
    {
        var early = Calculate(2026, 3m, Lot(1, 10_000m, new DateTime(2026, 2, 1))).InterestAmount;
        var late = Calculate(2026, 3m, Lot(1, 10_000m, new DateTime(2026, 11, 1))).InterestAmount;

        Assert.True(early > late);
    }

    [Fact]
    public void DetailsSum_EqualsContractInterest()
    {
        var result = Calculate(2026, 3m, Lot(1, 100_000m, new DateTime(2026, 1, 1)),
            Movement(Lot(1, 100_000m, new DateTime(2026, 1, 1)), 100_000m, new DateTime(2026, 1, 1)));

        Assert.Equal(
            result.InterestAmount,
            Math.Round(result.Details.Sum(d => d.InterestAmount), 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void ProvisionalAsOf_DoesNotMutateBookValue()
    {
        var lot = Lot(1, 100_000m, new DateTime(2026, 1, 1));
        var result = Calculate(2026, 2.38m, lot, asOf: new DateTime(2026, 9, 15));

        Assert.Equal(100_000m, lot.RemainingAmount);
        Assert.Equal(100_000m, result.BookValue);
        Assert.True(result.InterestAmount > 0m);
    }

    private static EuroFundCalculationResult Calculate(
        int year,
        decimal rate,
        EuroFundLot lot,
        params EuroFundLotMovement[] movements) =>
        Calculate(year, rate, lot, null, movements);

    private static EuroFundCalculationResult Calculate(
        int year,
        decimal rate,
        EuroFundLot lot,
        DateTime? asOf = null,
        params EuroFundLotMovement[] movements)
    {
        var rows = movements.Length == 0
            ? Movements(Movement(lot, lot.InitialAmount, lot.ValueDate))
            : movements.ToList();

        return new EuroFundAccrualCalculator().Calculate([lot], rows, year, rate, asOf);
    }

    private static EuroFundLot Lot(int id, decimal amount, DateTime valueDate, decimal? bonusRate = null) =>
        new()
        {
            Id = id,
            ContractId = 1,
            FinancialSupportId = 1,
            InitialAmount = amount,
            RemainingAmount = amount,
            ValueDate = valueDate,
            BonusRate = bonusRate,
        };

    private static EuroFundLotMovement Movement(EuroFundLot lot, decimal amount, DateTime date) =>
        new()
        {
            EuroFundLotId = lot.Id,
            ContractId = lot.ContractId,
            FinancialSupportId = lot.FinancialSupportId,
            Amount = amount,
            MovementDate = date,
            MovementType = amount >= 0m ? EuroFundLotMovementType.In : EuroFundLotMovementType.Out,
        };

    private static List<EuroFundLotMovement> Movements(params EuroFundLotMovement[] rows) => rows.ToList();
}
