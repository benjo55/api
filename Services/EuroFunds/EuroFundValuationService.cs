using api.Data;
using api.Dtos.EuroFund;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services.EuroFunds
{
    public sealed class EuroFundValuationService : IEuroFundValuationService
    {
        private readonly ApplicationDBContext _context;
        private readonly EuroFundAccrualCalculator _calculator;

        public EuroFundValuationService(ApplicationDBContext context, EuroFundAccrualCalculator calculator)
        {
            _context = context;
            _calculator = calculator;
        }

        public async Task<EuroFundValuationDto> GetValuationAsync(
            int contractId,
            int financialSupportId,
            DateTime valuationDate,
            CancellationToken cancellationToken = default)
        {
            var bookValue = await _context.EuroFundLots
                .Where(l =>
                    l.ContractId == contractId &&
                    l.FinancialSupportId == financialSupportId &&
                    l.ValueDate.Date <= valuationDate.Date)
                .SumAsync(l => l.RemainingAmount, cancellationToken);

            var year = valuationDate.Year;
            var rate = await ResolveProvisionalRateAsync(financialSupportId, year, valuationDate, cancellationToken);
            decimal accrued = 0m;

            if (rate.Rate > 0m)
            {
                var lots = await _context.EuroFundLots
                    .Where(l => l.ContractId == contractId && l.FinancialSupportId == financialSupportId)
                    .ToListAsync(cancellationToken);

                var lotIds = lots.Select(l => l.Id).ToList();
                var movements = await _context.EuroFundLotMovements
                    .Where(m => lotIds.Contains(m.EuroFundLotId) && m.MovementDate.Date < valuationDate.Date)
                    .ToListAsync(cancellationToken);

                accrued = _calculator.Calculate(lots, movements, year, rate.Rate, valuationDate.Date).InterestAmount;
            }

            var lastPb = await _context.EuroFundRevaluations
                .Where(r => r.ContractId == contractId && r.FinancialSupportId == financialSupportId)
                .OrderByDescending(r => r.FinancialYear)
                .Select(r => new { r.FinancialYear, r.InterestAmount, r.FinalServedRate })
                .FirstOrDefaultAsync(cancellationToken);

            return new EuroFundValuationDto
            {
                ContractId = contractId,
                FinancialSupportId = financialSupportId,
                ValuationDate = valuationDate.Date,
                BookValue = Math.Round(bookValue, 7, MidpointRounding.AwayFromZero),
                EstimatedAccruedInterest = accrued,
                EstimatedValue = Math.Round(bookValue + accrued, 7, MidpointRounding.AwayFromZero),
                ProvisionalRate = rate.Rate,
                ProvisionalRateLabel = rate.Label,
                LastParticipationBenefit = lastPb?.InterestAmount,
                LastParticipationBenefitYear = lastPb?.FinancialYear,
                PreviousFinalServedRate = lastPb?.FinalServedRate,
            };
        }

        private async Task<(decimal Rate, string Label)> ResolveProvisionalRateAsync(
            int financialSupportId,
            int year,
            DateTime valuationDate,
            CancellationToken ct)
        {
            var config = await _context.EuroFundConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.FinancialSupportId == financialSupportId, ct);

            if (config == null || config.ProvisionalRateMethod == EuroFundProvisionalRateMethod.None)
                return (0m, "Aucun taux provisoire");

            decimal rate = 0m;
            var label = config.ProvisionalRateMethod.ToString();

            if (config.ProvisionalRateMethod == EuroFundProvisionalRateMethod.FixedRate)
            {
                rate = config.FixedProvisionalRate ?? 0m;
                label = "Taux fixe provisoire";
            }
            else if (config.ProvisionalRateMethod == EuroFundProvisionalRateMethod.TmePercentage)
            {
                var tme = await _context.ReferenceRates
                    .Where(r => r.RateType == ReferenceRateType.Tme && r.RateDate.Date <= valuationDate.Date)
                    .OrderByDescending(r => r.RateDate)
                    .Select(r => (decimal?)r.RateValue)
                    .FirstOrDefaultAsync(ct);

                tme ??= await _context.EuroFundFinancialYears
                    .Where(y => y.FinancialSupportId == financialSupportId && y.Year == year)
                    .Select(y => y.TmeRate)
                    .FirstOrDefaultAsync(ct);

                rate = (tme ?? 0m) * (config.ProvisionalRatePercentage ?? 0m) / 100m;
                label = $"{config.ProvisionalRatePercentage ?? 0m}% TME";
            }
            else if (config.ProvisionalRateMethod == EuroFundProvisionalRateMethod.PreviousFinalRatePercentage)
            {
                var previousRate = await _context.EuroFundFinancialYears
                    .Where(y => y.FinancialSupportId == financialSupportId && y.Year < year && y.FinalServedRate != null)
                    .OrderByDescending(y => y.Year)
                    .Select(y => y.FinalServedRate)
                    .FirstOrDefaultAsync(ct);

                rate = (previousRate ?? 0m) * (config.PreviousFinalRatePercentage ?? 0m) / 100m;
                label = $"{config.PreviousFinalRatePercentage ?? 0m}% taux final precedent";
            }

            if (config.RateFloor.HasValue)
                rate = Math.Max(rate, config.RateFloor.Value);
            if (config.RateCap.HasValue)
                rate = Math.Min(rate, config.RateCap.Value);

            return (Math.Round(rate, 7, MidpointRounding.AwayFromZero), label);
        }
    }
}
