using api.Data;
using api.Dtos.EuroFund;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.EuroFunds
{
    public sealed class EuroFundRevaluationService : IEuroFundRevaluationService
    {
        private readonly ApplicationDBContext _context;
        private readonly IOperationApplier _operationApplier;
        private readonly IContractValuationService _valuationService;
        private readonly EuroFundAccrualCalculator _calculator;
        private readonly ILogger<EuroFundRevaluationService> _logger;

        public EuroFundRevaluationService(
            ApplicationDBContext context,
            IOperationApplier operationApplier,
            IContractValuationService valuationService,
            EuroFundAccrualCalculator calculator,
            ILogger<EuroFundRevaluationService> logger)
        {
            _context = context;
            _operationApplier = operationApplier;
            _valuationService = valuationService;
            _calculator = calculator;
            _logger = logger;
        }

        public async Task<List<EuroFundSummaryDto>> GetEuroFundsAsync(CancellationToken cancellationToken = default)
        {
            var funds = await _context.FinancialSupports
                .AsNoTracking()
                .Where(s => s.SupportNature == FinancialSupportNature.EuroFund)
                .OrderBy(s => s.Label)
                .ToListAsync(cancellationToken);

            var configs = await _context.EuroFundConfigurations
                .AsNoTracking()
                .Where(c => funds.Select(f => f.Id).Contains(c.FinancialSupportId))
                .ToDictionaryAsync(c => c.FinancialSupportId, cancellationToken);

            return funds.Select(f => MapFund(f, configs.GetValueOrDefault(f.Id))).ToList();
        }

        public async Task<EuroFundSummaryDto?> GetEuroFundAsync(int financialSupportId, CancellationToken cancellationToken = default)
        {
            var fund = await _context.FinancialSupports
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == financialSupportId && s.SupportNature == FinancialSupportNature.EuroFund, cancellationToken);
            if (fund == null)
                return null;

            var config = await _context.EuroFundConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.FinancialSupportId == financialSupportId, cancellationToken);

            return MapFund(fund, config);
        }

        public async Task<EuroFundConfigurationDto> UpsertConfigurationAsync(int financialSupportId, EuroFundConfigurationDto dto, CancellationToken cancellationToken = default)
        {
            var support = await _context.FinancialSupports.FindAsync([financialSupportId], cancellationToken)
                ?? throw new InvalidOperationException($"Support {financialSupportId} introuvable.");
            support.SupportNature = FinancialSupportNature.EuroFund;
            support.IsCapitalGuaranteed = true;

            var config = await _context.EuroFundConfigurations
                .FirstOrDefaultAsync(c => c.FinancialSupportId == financialSupportId, cancellationToken);

            if (config == null)
            {
                config = new EuroFundConfiguration { FinancialSupportId = financialSupportId };
                _context.EuroFundConfigurations.Add(config);
            }

            ApplyConfiguration(config, dto);
            config.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return MapConfiguration(config);
        }

        public async Task<List<EuroFundFinancialYearDto>> GetFinancialYearsAsync(int financialSupportId, CancellationToken cancellationToken = default)
        {
            return await _context.EuroFundFinancialYears
                .AsNoTracking()
                .Where(y => y.FinancialSupportId == financialSupportId)
                .OrderByDescending(y => y.Year)
                .Select(y => MapYear(y))
                .ToListAsync(cancellationToken);
        }

        public async Task<EuroFundFinancialYearDto> UpsertFinancialYearAsync(int financialSupportId, int year, EuroFundFinancialYearDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _context.EuroFundFinancialYears
                .FirstOrDefaultAsync(y => y.FinancialSupportId == financialSupportId && y.Year == year, cancellationToken);

            if (entity == null)
            {
                entity = new EuroFundFinancialYear { FinancialSupportId = financialSupportId, Year = year };
                _context.EuroFundFinancialYears.Add(entity);
            }
            else if (entity.Status == EuroFundFinancialYearStatus.Finalized)
            {
                throw new InvalidOperationException("Exercice fonds euros deja finalise.");
            }

            entity.TmeRate = dto.TmeRate;
            entity.AssetYield = dto.AssetYield;
            entity.OpeningPpbReserve = dto.OpeningPpbReserve;
            entity.PpbAllocation = dto.PpbAllocation;
            entity.PpbRelease = dto.PpbRelease;
            entity.ClosingPpbReserve = dto.ClosingPpbReserve;
            entity.FinalServedRate = dto.FinalServedRate;
            entity.RateNature = dto.RateNature;
            entity.Status = dto.Status;
            entity.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return MapYear(entity);
        }

        public async Task<ReferenceRateDto> AddReferenceRateAsync(ReferenceRateDto dto, CancellationToken cancellationToken = default)
        {
            var entity = new ReferenceRate
            {
                RateType = dto.RateType,
                RateDate = dto.RateDate.Date,
                RateValue = dto.RateValue,
                Source = dto.Source,
                CreatedDate = DateTime.UtcNow,
            };
            _context.ReferenceRates.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return dto;
        }

        public async Task<EuroFundPreviewDto> PreviewAsync(int financialSupportId, int year, DateTime? asOf = null, CancellationToken cancellationToken = default)
        {
            var fy = await _context.EuroFundFinancialYears
                .AsNoTracking()
                .FirstOrDefaultAsync(y => y.FinancialSupportId == financialSupportId && y.Year == year, cancellationToken);

            var errors = new List<string>();
            var rate = fy?.FinalServedRate ?? fy?.TmeRate ?? 0m;
            var usesFinalRate = fy?.FinalServedRate != null;
            if (rate <= 0m)
                errors.Add("Aucun taux final servi ou TME n'est renseigne pour l'exercice.");

            var lots = await _context.EuroFundLots
                .AsNoTracking()
                .Where(l => l.FinancialSupportId == financialSupportId && l.ValueDate.Year <= year)
                .ToListAsync(cancellationToken);

            var lotIds = lots.Select(l => l.Id).ToList();
            var movements = await _context.EuroFundLotMovements
                .AsNoTracking()
                .Where(m => lotIds.Contains(m.EuroFundLotId) && m.MovementDate.Year <= year)
                .ToListAsync(cancellationToken);

            var contractIds = lots.Select(l => l.ContractId).Distinct().ToList();
            var contracts = await _context.Contracts
                .AsNoTracking()
                .Where(c => contractIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellationToken);

            var result = new EuroFundPreviewDto
            {
                FinancialSupportId = financialSupportId,
                FinancialYear = year,
                AppliedRate = rate,
                UsesFinalRate = usesFinalRate,
                Errors = errors,
            };

            foreach (var contractGroup in lots.GroupBy(l => l.ContractId))
            {
                var contractLots = contractGroup.ToList();
                var contractLotIds = contractLots.Select(l => l.Id).ToHashSet();
                var calc = _calculator.Calculate(
                    contractLots,
                    movements.Where(m => contractLotIds.Contains(m.EuroFundLotId)),
                    year,
                    rate,
                    asOf);

                result.Contracts.Add(new EuroFundContractPreviewDto
                {
                    ContractId = contractGroup.Key,
                    ContractNumber = contracts.GetValueOrDefault(contractGroup.Key)?.ContractNumber ?? string.Empty,
                    BookValue = calc.BookValue,
                    WeightedExposure = calc.WeightedExposure,
                    ParticipationBenefit = calc.InterestAmount,
                    Details = calc.Details,
                });
            }

            result.ContractCount = result.Contracts.Count;
            result.TotalBookValue = Math.Round(result.Contracts.Sum(c => c.BookValue), 7, MidpointRounding.AwayFromZero);
            result.TotalParticipationBenefit = Math.Round(result.Contracts.Sum(c => c.ParticipationBenefit), 2, MidpointRounding.AwayFromZero);
            result.AverageParticipationBenefit = result.ContractCount > 0
                ? Math.Round(result.TotalParticipationBenefit / result.ContractCount, 2, MidpointRounding.AwayFromZero)
                : 0m;

            return result;
        }

        public async Task<EuroFundPreviewDto> FinalizeAsync(int financialSupportId, int year, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var fy = await _context.EuroFundFinancialYears
                .FirstOrDefaultAsync(y => y.FinancialSupportId == financialSupportId && y.Year == year, cancellationToken)
                ?? throw new InvalidOperationException("Exercice fonds euros introuvable.");

            if (fy.FinalServedRate == null || fy.FinalServedRate <= 0m)
                throw new InvalidOperationException("Le taux final servi est obligatoire avant finalisation.");

            if (fy.Status == EuroFundFinancialYearStatus.Finalized)
            {
                await transaction.RollbackAsync(cancellationToken);
                return await PreviewAsync(financialSupportId, year, cancellationToken: cancellationToken);
            }

            var preview = await PreviewAsync(financialSupportId, year, cancellationToken: cancellationToken);
            if (preview.Errors.Any())
                throw new InvalidOperationException(string.Join(" | ", preview.Errors));

            var support = await _context.FinancialSupports.FindAsync([financialSupportId], cancellationToken)
                ?? throw new InvalidOperationException("Support fonds euros introuvable.");

            var config = await _context.EuroFundConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.FinancialSupportId == financialSupportId, cancellationToken);
            var creditDate = ResolveCreditDate(config, year);

            foreach (var row in preview.Contracts.Where(c => c.ParticipationBenefit > 0m))
            {
                var alreadyExists = await _context.EuroFundRevaluations
                    .AnyAsync(r => r.ContractId == row.ContractId &&
                                   r.FinancialSupportId == financialSupportId &&
                                   r.FinancialYear == year, cancellationToken);
                if (alreadyExists)
                    continue;

                var contract = await _context.Contracts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == row.ContractId, cancellationToken);

                var compartmentId = await _context.FinancialSupportAllocations
                    .Where(f => f.ContractId == row.ContractId && f.SupportId == financialSupportId)
                    .OrderByDescending(f => f.CurrentAmount)
                    .Select(f => (int?)f.CompartmentId)
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? await _context.Compartments
                        .Where(c => c.ContractId == row.ContractId)
                        .OrderByDescending(c => c.IsDefault)
                        .ThenBy(c => c.Id)
                        .Select(c => (int?)c.Id)
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new InvalidOperationException($"Poche introuvable pour contrat {row.ContractId}.");

                var operation = new Operation
                {
                    ContractId = row.ContractId,
                    Type = OperationType.ParticipationBenefit,
                    Status = OperationStatus.Executed,
                    OperationDate = creditDate,
                    ExecutionDate = creditDate,
                    Amount = row.ParticipationBenefit,
                    RequestedAmount = row.ParticipationBenefit,
                    ExecutedAmount = row.ParticipationBenefit,
                    Currency = contract?.Currency ?? support.Currency,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    Allocations =
                    {
                        new OperationSupportAllocation
                        {
                            SupportId = financialSupportId,
                            CompartmentId = compartmentId,
                            Amount = row.ParticipationBenefit,
                            Shares = row.ParticipationBenefit,
                            NavAtOperation = 1m,
                            NavDateAtOperation = creditDate,
                            Flow = OperationFlow.Target,
                        }
                    }
                };

                _context.Operations.Add(operation);
                await _context.SaveChangesAsync(cancellationToken);

                await _operationApplier.ApplyAsync(operation, _context, cancellationToken);

                var revaluation = new EuroFundRevaluation
                {
                    Operation = operation,
                    ContractId = row.ContractId,
                    FinancialSupportId = financialSupportId,
                    FinancialYear = year,
                    FinalServedRate = fy.FinalServedRate.Value,
                    BookValueBeforeCredit = row.BookValue,
                    WeightedExposure = row.WeightedExposure,
                    InterestAmount = row.ParticipationBenefit,
                    YearBasis = DateTime.IsLeapYear(year) ? 366 : 365,
                    ComputedAt = DateTime.UtcNow,
                    Details = row.Details.Select(d => new EuroFundRevaluationDetail
                    {
                        EuroFundLotId = d.LotId,
                        PeriodStart = d.PeriodStart,
                        PeriodEnd = d.PeriodEnd,
                        OpeningAmount = d.OpeningAmount,
                        BaseRate = d.BaseRate,
                        BonusRate = d.BonusRate,
                        ApplicableRate = d.ApplicableRate,
                        DayCount = d.DayCount,
                        YearBasis = d.YearBasis,
                        InterestAmount = d.InterestAmount,
                    }).ToList(),
                };
                _context.EuroFundRevaluations.Add(revaluation);

                _logger.LogInformation(
                    "PB fonds euros calculee fund={FundId} year={Year} contract={ContractId} op={OperationId} rate={Rate} interest={Interest}",
                    financialSupportId,
                    year,
                    row.ContractId,
                    operation.Id,
                    fy.FinalServedRate.Value,
                    row.ParticipationBenefit);
            }

            fy.Status = EuroFundFinancialYearStatus.Finalized;
            fy.FinalizedAt = DateTime.UtcNow;
            fy.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            foreach (var contractId in preview.Contracts.Select(c => c.ContractId).Distinct())
                await _valuationService.ComputeContractValueAsync(contractId);

            return await PreviewAsync(financialSupportId, year, cancellationToken: cancellationToken);
        }

        private static DateTime ResolveCreditDate(EuroFundConfiguration? config, int year)
        {
            var month = Math.Clamp(config?.AnnualCreditMonth ?? 12, 1, 12);
            var maxDay = DateTime.DaysInMonth(year, month);
            var day = Math.Clamp(config?.AnnualCreditDay ?? 31, 1, maxDay);
            return new DateTime(year, month, day);
        }

        private static EuroFundSummaryDto MapFund(FinancialSupport fund, EuroFundConfiguration? config) =>
            new()
            {
                Id = fund.Id,
                Code = fund.Code,
                Label = fund.Label,
                Currency = fund.Currency,
                Configuration = config == null
                    ? new EuroFundConfigurationDto { FinancialSupportId = fund.Id }
                    : MapConfiguration(config),
            };

        private static EuroFundConfigurationDto MapConfiguration(EuroFundConfiguration config) =>
            new()
            {
                FinancialSupportId = config.FinancialSupportId,
                AccrualMethod = config.AccrualMethod,
                AnnualCreditMonth = config.AnnualCreditMonth,
                AnnualCreditDay = config.AnnualCreditDay,
                ProvisionalRateMethod = config.ProvisionalRateMethod,
                ProvisionalRatePercentage = config.ProvisionalRatePercentage,
                FixedProvisionalRate = config.FixedProvisionalRate,
                PreviousFinalRatePercentage = config.PreviousFinalRatePercentage,
                EarlyExitRateMethod = config.EarlyExitRateMethod,
                LotConsumptionMethod = config.LotConsumptionMethod,
                RateNature = config.RateNature,
                ManagementFeeTreatment = config.ManagementFeeTreatment,
                MinimumGuaranteedRate = config.MinimumGuaranteedRate,
                RateFloor = config.RateFloor,
                RateCap = config.RateCap,
            };

        private static void ApplyConfiguration(EuroFundConfiguration config, EuroFundConfigurationDto dto)
        {
            config.AccrualMethod = dto.AccrualMethod;
            config.AnnualCreditMonth = dto.AnnualCreditMonth;
            config.AnnualCreditDay = dto.AnnualCreditDay;
            config.ProvisionalRateMethod = dto.ProvisionalRateMethod;
            config.ProvisionalRatePercentage = dto.ProvisionalRatePercentage;
            config.FixedProvisionalRate = dto.FixedProvisionalRate;
            config.PreviousFinalRatePercentage = dto.PreviousFinalRatePercentage;
            config.EarlyExitRateMethod = dto.EarlyExitRateMethod;
            config.LotConsumptionMethod = dto.LotConsumptionMethod;
            config.RateNature = dto.RateNature;
            config.ManagementFeeTreatment = dto.ManagementFeeTreatment;
            config.MinimumGuaranteedRate = dto.MinimumGuaranteedRate;
            config.RateFloor = dto.RateFloor;
            config.RateCap = dto.RateCap;
        }

        private static EuroFundFinancialYearDto MapYear(EuroFundFinancialYear y) =>
            new()
            {
                FinancialSupportId = y.FinancialSupportId,
                Year = y.Year,
                TmeRate = y.TmeRate,
                AssetYield = y.AssetYield,
                OpeningPpbReserve = y.OpeningPpbReserve,
                PpbAllocation = y.PpbAllocation,
                PpbRelease = y.PpbRelease,
                ClosingPpbReserve = y.ClosingPpbReserve,
                FinalServedRate = y.FinalServedRate,
                RateNature = y.RateNature,
                Status = y.Status,
            };
    }
}
