using api.Dtos.EuroFund;
using api.Models;

namespace api.Services.EuroFunds
{
    public sealed class EuroFundAccrualCalculator
    {
        public EuroFundCalculationResult Calculate(
            IEnumerable<EuroFundLot> lots,
            IEnumerable<EuroFundLotMovement> movements,
            int year,
            decimal annualRate,
            DateTime? asOf = null)
        {
            var periodStart = new DateTime(year, 1, 1);
            var yearEndExclusive = new DateTime(year + 1, 1, 1);
            var periodEnd = asOf?.Date is { } asOfDate && asOfDate > periodStart && asOfDate < yearEndExclusive
                ? asOfDate
                : yearEndExclusive;

            var yearBasis = DateTime.IsLeapYear(year) ? 366 : 365;
            var details = new List<EuroFundRevaluationDetailDto>();
            decimal rawInterest = 0m;
            decimal weightedExposure = 0m;
            decimal bookValueAtEnd = 0m;

            var movementsByLot = movements
                .GroupBy(m => m.EuroFundLotId)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.MovementDate).ThenBy(m => m.Id).ToList());

            foreach (var lot in lots.OrderBy(l => l.ValueDate).ThenBy(l => l.Id))
            {
                movementsByLot.TryGetValue(lot.Id, out var lotMovements);
                lotMovements ??= new List<EuroFundLotMovement>();

                var openingAmount = lotMovements
                    .Where(m => m.MovementDate.Date < periodStart)
                    .Sum(m => m.Amount);

                if (!lotMovements.Any() && lot.ValueDate.Date < periodStart)
                    openingAmount = lot.InitialAmount;

                var inYearEvents = lotMovements
                    .Where(m => m.MovementDate.Date >= periodStart && m.MovementDate.Date < periodEnd)
                    .GroupBy(m => m.MovementDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new { Date = g.Key, Amount = g.Sum(m => m.Amount) })
                    .ToList();

                var balance = openingAmount;
                var cursor = periodStart;
                var applicableRate = annualRate + (lot.BonusRate ?? 0m);

                foreach (var evt in inYearEvents)
                {
                    AddSegment(lot.Id, cursor, evt.Date, balance, annualRate, lot.BonusRate ?? 0m, applicableRate, yearBasis, details, ref rawInterest, ref weightedExposure);
                    balance += evt.Amount;
                    if (balance < 0m && balance > -0.0000001m)
                        balance = 0m;
                    cursor = evt.Date;
                }

                AddSegment(lot.Id, cursor, periodEnd, balance, annualRate, lot.BonusRate ?? 0m, applicableRate, yearBasis, details, ref rawInterest, ref weightedExposure);
                bookValueAtEnd += Math.Max(0m, balance);
            }

            return new EuroFundCalculationResult(
                Math.Round(bookValueAtEnd, 7, MidpointRounding.AwayFromZero),
                Math.Round(weightedExposure, 7, MidpointRounding.AwayFromZero),
                Math.Round(rawInterest, 2, MidpointRounding.AwayFromZero),
                yearBasis,
                details);
        }

        private static void AddSegment(
            int lotId,
            DateTime start,
            DateTime end,
            decimal openingAmount,
            decimal baseRate,
            decimal bonusRate,
            decimal applicableRate,
            int yearBasis,
            List<EuroFundRevaluationDetailDto> details,
            ref decimal rawInterest,
            ref decimal weightedExposure)
        {
            var days = (end.Date - start.Date).Days;
            if (days <= 0 || openingAmount <= 0m)
                return;

            var exposure = openingAmount * days / yearBasis;
            var interest = openingAmount * applicableRate / 100m * days / yearBasis;
            rawInterest += interest;
            weightedExposure += exposure;

            details.Add(new EuroFundRevaluationDetailDto
            {
                LotId = lotId,
                PeriodStart = start.Date,
                PeriodEnd = end.Date,
                OpeningAmount = Math.Round(openingAmount, 7, MidpointRounding.AwayFromZero),
                BaseRate = baseRate,
                BonusRate = bonusRate,
                ApplicableRate = applicableRate,
                DayCount = days,
                YearBasis = yearBasis,
                InterestAmount = Math.Round(interest, 7, MidpointRounding.AwayFromZero),
            });
        }
    }

    public sealed record EuroFundCalculationResult(
        decimal BookValue,
        decimal WeightedExposure,
        decimal InterestAmount,
        int YearBasis,
        List<EuroFundRevaluationDetailDto> Details);
}
