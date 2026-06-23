using api.Data;
using api.Dtos.Contract;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Interfaces
{
    public sealed class ContractAuditService : IContractAuditService
    {
        private const decimal MoneyTolerance = 0.01m;
        private const decimal PrecisionTolerance = 0.0000001m;
        private readonly ApplicationDBContext _context;
        private readonly ILogger<ContractAuditService> _logger;

        public ContractAuditService(
            ApplicationDBContext context,
            ILogger<ContractAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ContractReconciliationDto> GetReconciliationAsync(int contractId)
        {
            var contract = await _context.Contracts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractId)
                ?? throw new KeyNotFoundException($"Contrat {contractId} introuvable.");

            var operations = await _context.Operations
                .AsNoTracking()
                .Where(o => o.ContractId == contractId)
                .Include(o => o.Allocations)
                .ToListAsync();
            var fsas = await _context.FinancialSupportAllocations
                .AsNoTracking()
                .Where(f => f.ContractId == contractId)
                .Include(f => f.Support)
                .ToListAsync();
            var holdings = await _context.ContractSupportHoldings
                .AsNoTracking()
                .Where(h => h.ContractId == contractId)
                .ToListAsync();
            var advances = await _context.Advances
                .AsNoTracking()
                .Where(a => a.ContractId == contractId)
                .Include(a => a.Transactions)
                .ToListAsync();
            var feeApplications = await _context.ContractSupportFeeApplications
                .AsNoTracking()
                .Where(f => f.ContractId == contractId)
                .ToListAsync();

            var executed = operations.Where(o => o.Status == OperationStatus.Executed).ToList();
            var pending = operations.Where(o => o.Status == OperationStatus.Pending).ToList();
            var paymentTypes = new[] { OperationType.InitialPayment, OperationType.FreePayment, OperationType.ScheduledPayment };
            var withdrawalTypes = new[] { OperationType.PartialWithdrawal, OperationType.TotalWithdrawal, OperationType.ScheduledWithdrawal };
            var feeTypes = new[] { OperationType.ManagementFee, OperationType.OperationFee };

            var contributionsExecuted = executed.Where(o => paymentTypes.Contains(o.Type)).Sum(o => o.ExecutedAmount ?? o.Amount ?? 0m);
            var contributionsPending = pending.Where(o => paymentTypes.Contains(o.Type)).Sum(o => o.RequestedAmount ?? o.Amount ?? 0m);
            var withdrawalsExecuted = executed.Where(o => withdrawalTypes.Contains(o.Type)).Sum(o => o.ExecutedAmount ?? o.Amount ?? 0m);
            var withdrawalsPending = pending.Where(o => withdrawalTypes.Contains(o.Type)).Sum(o => o.RequestedAmount ?? o.Amount ?? 0m);
            var feesExecuted = executed.Where(o => feeTypes.Contains(o.Type)).Sum(o => o.ExecutedAmount ?? o.Amount ?? 0m);
            var netExternalCash = contributionsExecuted - withdrawalsExecuted;
            var currentFromPositions = fsas.Sum(f => f.CurrentAmount);
            var remainingCostBasis = fsas.Sum(f => f.InvestedAmount);
            var simpleGain = contract.CurrentValue - netExternalCash;

            var dto = new ContractReconciliationDto
            {
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber,
                GeneratedAt = DateTime.UtcNow,
                Metrics = new ContractReconciliationMetricsDto
                {
                    ContributionsExecuted = contributionsExecuted,
                    ContributionsPending = contributionsPending,
                    WithdrawalsExecuted = withdrawalsExecuted,
                    WithdrawalsPending = withdrawalsPending,
                    FeesExecuted = feesExecuted,
                    NetExternalCash = netExternalCash,
                    SettledMovementBalance = netExternalCash - feesExecuted,
                    CurrentValueStored = contract.CurrentValue,
                    CurrentValueFromPositions = currentFromPositions,
                    RemainingCostBasis = remainingCostBasis,
                    SimpleGain = simpleGain,
                    SimplePerformancePercent = netExternalCash > 0m ? simpleGain / netExternalCash * 100m : 0m,
                },
            };

            AddCheck(dto, "NET_EXTERNAL_CASH", "Flux net externe", netExternalCash, contract.NetInvested, MoneyTolerance);
            AddCheck(dto, "CURRENT_VALUE", "Valeur contrat / positions", currentFromPositions, contract.CurrentValue, MoneyTolerance);
            AddCheck(dto, "HOLDING_SHARES", "Parts holdings / poches", fsas.Sum(f => f.CurrentShares), holdings.Sum(h => h.TotalShares), PrecisionTolerance);
            AddCheck(dto, "HOLDING_COST_BASIS", "Coût holdings / poches", remainingCostBasis, holdings.Sum(h => h.TotalInvested), PrecisionTolerance);

            var executedFeeOperations = executed.Where(o => feeTypes.Contains(o.Type)).ToList();
            AddCheck(dto, "FEE_APPLICATION_COUNT", "Frais / ventilations poche-support", executedFeeOperations.Count, feeApplications.Count, 0m);
            AddCheck(dto, "FEE_APPLICATION_TOTAL", "Montant frais / ventilations", feesExecuted, feeApplications.Sum(f => f.FeeAmount), MoneyTolerance);

            foreach (var application in feeApplications)
            {
                var feeOperation = executedFeeOperations.FirstOrDefault(o => o.Id == application.FeeOperationId);
                AddCheck(
                    dto,
                    $"FEE_OPERATION_{application.FeeOperationId}",
                    $"Frais #{application.FeeOperationId} / ventilation",
                    feeOperation?.ExecutedAmount ?? feeOperation?.Amount ?? 0m,
                    application.FeeAmount,
                    MoneyTolerance);
                AddCheck(
                    dto,
                    $"FEE_SHARES_{application.FeeOperationId}",
                    $"Frais #{application.FeeOperationId} / parts × VL",
                    application.FeeAmount,
                    application.FeeShares * application.NavUsed,
                    MoneyTolerance);

                var hasAuditMetadata = application.BaseAmount > 0m &&
                    application.FeeMode.HasValue &&
                    (application.FeeNature != ContractSupportFeeNature.ManagementFee ||
                     application.AppliedRate > 0m &&
                     application.AccrualStartDate.HasValue &&
                     application.AccrualEndDate.HasValue &&
                     application.AccruedDays > 0);
                AddCheck(
                    dto,
                    $"FEE_AUDIT_{application.FeeOperationId}",
                    $"Traçabilité frais #{application.FeeOperationId}",
                    1m,
                    hasAuditMetadata ? 1m : 0m,
                    0m);
            }

            dto.Supports = fsas
                .GroupBy(f => new { f.SupportId, f.Support!.Label, f.Support.ISIN, f.Support.LastValuationAmount, f.Support.LastValuationDate })
                .Select(group =>
                {
                    var shares = group.Sum(f => f.CurrentShares);
                    var basis = group.Sum(f => f.InvestedAmount);
                    var current = group.Sum(f => f.CurrentAmount);
                    var nav = group.Key.LastValuationAmount ?? 0m;
                    var pru = shares > 0m ? basis / shares : 0m;
                    return new SupportReconciliationDto
                    {
                        SupportId = group.Key.SupportId,
                        Label = group.Key.Label ?? string.Empty,
                        Isin = group.Key.ISIN ?? string.Empty,
                        Shares = shares,
                        RemainingCostBasis = basis,
                        Pru = pru,
                        Nav = nav,
                        NavDate = group.Key.LastValuationDate,
                        CurrentValue = current,
                        RecalculatedValue = shares * nav,
                        UnrealizedGain = current - basis,
                        PricePerformancePercent = pru > 0m ? (nav - pru) / pru * 100m : 0m,
                    };
                })
                .OrderByDescending(s => s.CurrentValue)
                .ToList();

            foreach (var support in dto.Supports)
                AddCheck(dto, $"SUPPORT_VALUE_{support.SupportId}", $"Valeur {support.Label}", support.RecalculatedValue, support.CurrentValue, MoneyTolerance);

            dto.Arbitrages = executed
                .Where(o => o.Type is OperationType.Arbitrage or OperationType.ScheduledArbitrage)
                .Select(o =>
                {
                    var source = o.Allocations.Where(a => a.Flow == OperationFlow.Source).Sum(a => a.Amount ?? 0m);
                    var target = o.Allocations.Where(a => a.Flow == OperationFlow.Target).Sum(a => a.Amount ?? 0m);
                    return new ArbitrageReconciliationDto
                    {
                        OperationId = o.Id,
                        OperationDate = o.OperationDate,
                        RequestedAmount = o.RequestedAmount ?? o.Amount ?? 0m,
                        ExecutedAmount = o.ExecutedAmount ?? o.Amount ?? 0m,
                        SourceAmount = source,
                        TargetAmount = target,
                        BalanceDelta = source - target,
                    };
                })
                .ToList();

            foreach (var arbitrage in dto.Arbitrages)
            {
                AddCheck(dto, $"ARBITRAGE_BALANCE_{arbitrage.OperationId}", $"Équilibre arbitrage #{arbitrage.OperationId}", arbitrage.SourceAmount, arbitrage.TargetAmount, MoneyTolerance);
                AddCheck(dto, $"ARBITRAGE_AMOUNT_{arbitrage.OperationId}", $"Montant exécuté arbitrage #{arbitrage.OperationId}", arbitrage.SourceAmount, arbitrage.ExecutedAmount, MoneyTolerance);
            }

            dto.Advances = new AdvanceReconciliationDto
            {
                Requested = advances.Sum(a => a.RequestedAmount),
                Approved = advances.Sum(a => a.ApprovedAmount ?? 0m),
                Disbursed = advances.SelectMany(a => a.Transactions).Where(t => t.Type == AdvanceTransactionType.Disbursement).Sum(t => t.Amount),
                PrincipalRepaid = advances.SelectMany(a => a.Transactions).Where(t => t.Type is AdvanceTransactionType.PartialRepayment or AdvanceTransactionType.TotalRepayment).Sum(t => t.Amount),
                OutstandingPrincipal = advances.Sum(a => a.OutstandingCapital),
                AccruedInterest = 0m,
            };
            AddCheck(dto, "ADVANCE_OUTSTANDING", "Capital d'avance restant", dto.Advances.Disbursed - dto.Advances.PrincipalRepaid, dto.Advances.OutstandingPrincipal, MoneyTolerance);

            return dto;
        }

        public async Task LogContractIntegrityAsync(int contractId)
        {
            var report = await GetReconciliationAsync(contractId);
            foreach (var check in report.Checks.Where(c => !c.IsValid))
            {
                _logger.LogWarning(
                    "Audit contrat {ContractId} {Code}: expected={Expected}, actual={Actual}, delta={Delta}",
                    contractId, check.Code, check.Expected, check.Actual, check.Delta);
            }
        }

        private static void AddCheck(
            ContractReconciliationDto dto,
            string code,
            string label,
            decimal expected,
            decimal actual,
            decimal tolerance)
        {
            var delta = actual - expected;
            dto.Checks.Add(new ReconciliationCheckDto
            {
                Code = code,
                Label = label,
                Expected = expected,
                Actual = actual,
                Delta = delta,
                Tolerance = tolerance,
                IsValid = Math.Abs(delta) <= tolerance,
            });
        }
    }
}
