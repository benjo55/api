// Services/OperationEngineService.cs
using api.Data;
using api.Interfaces;
using api.Models;
using api.Configuration;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public class OperationEngineService : IOperationEngineService
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<OperationEngineService> _logger;
        private readonly IContractSupportHoldingRepository _holdingRepo;
        private readonly IContractValuationService _valuationService;
        private readonly IEodDataProvider _eodDataProvider;
        private readonly IDbContextFactory<ApplicationDBContext> _dbContextFactory;
        private readonly EodSettings _eodSettings;
        private readonly IOperationApplier _operationApplier;
        private readonly IManagementFeePolicyResolver _managementFeePolicyResolver;
        private readonly IOperationFeePolicyResolver _operationFeePolicyResolver;
        private readonly IAdvanceOperationService _advanceOperationService;

        private sealed record ManagementFeeAccrualComputation(
            decimal FeeAmount,
            decimal BaseAmountDays,
            int ChargeableDays,
            DateTime? FirstChargeableDate,
            DateTime? LastChargeableDate);

        public OperationEngineService(ApplicationDBContext context, ILogger<OperationEngineService> logger, IContractSupportHoldingRepository holdingRepo, IContractValuationService valuationService, IOptions<EodSettings> eodSettings, IEodDataProvider eodDataProvider, IDbContextFactory<ApplicationDBContext> dbContextFactory, IOperationApplier operationApplier, IManagementFeePolicyResolver managementFeePolicyResolver, IOperationFeePolicyResolver operationFeePolicyResolver, IAdvanceOperationService advanceOperationService)
        {
            _context = context;
            _logger = logger;
            _holdingRepo = holdingRepo;
            _valuationService = valuationService;
            _eodDataProvider = eodDataProvider;
            _dbContextFactory = dbContextFactory;
            _eodSettings = eodSettings.Value;
            _operationApplier = operationApplier;
            _managementFeePolicyResolver = managementFeePolicyResolver;
            _operationFeePolicyResolver = operationFeePolicyResolver;
            _advanceOperationService = advanceOperationService;
        }

        // 1️⃣ Maj quotidienne des VL

        public async Task UpdateValuationsAsync()
        {
            bool onlyStrategy = _eodSettings.OnlyStrategyMode;
            _logger.LogInformation(
                "▶️ Début UpdateValuationsAsync (VL depuis SupportHistoricalData) — Mode test={OnlyStrategy}",
                onlyStrategy);

            IQueryable<FinancialSupport> query = _context.FinancialSupports
                .Where(s => !string.IsNullOrWhiteSpace(s.ISIN));

            // 🔹 Mode test : uniquement supports “stratégie”
            if (onlyStrategy)
            {
                query = query.Where(s =>
                    EF.Functions.Like(s.Label.ToLower(), "%strategie%") ||
                    EF.Functions.Like(s.Label.ToLower(), "%stratégie%"));
            }

            var supports = await query
                .AsNoTracking()
                .ToListAsync();

            if (!supports.Any())
            {
                _logger.LogWarning("⚠️ Aucun support à mettre à jour (filtre={OnlyStrategy})", onlyStrategy);
                return;
            }

            _logger.LogInformation("🔍 {Count} supports sélectionnés pour synchronisation VL", supports.Count);

            int updatedCount = 0;

            foreach (var s in supports)
            {
                try
                {
                    var tracked = await _context.FinancialSupports.FindAsync(s.Id);
                    if (tracked == null)
                        continue;

                    // ==========================================================
                    // 🔹 SOURCE DE VÉRITÉ : DERNIÈRE VL HISTORISÉE
                    // ==========================================================
                    var lastHist = await _context.SupportHistoricalDatas
                        .Where(h =>
                            h.FinancialSupportId == s.Id &&
                            h.Nav != null)
                        .OrderByDescending(h => h.Date)
                        .FirstOrDefaultAsync();

                    if (lastHist == null)
                    {
                        _logger.LogDebug(
                            "⚠️ Aucun historique VL trouvé pour support {SupportId} ({ISIN})",
                            s.Id, s.ISIN);
                        continue;
                    }

                    bool needsUpdate =
                        tracked.LastValuationDate == null ||
                        tracked.LastValuationDate.Value.Date < lastHist.Date.Date ||
                        Math.Abs((tracked.LastValuationAmount ?? 0m) - (lastHist.Nav ?? 0m)) > 0.000001m;

                    if (!needsUpdate)
                        continue;

                    tracked.LastValuationAmount = Math.Round(lastHist.Nav ?? 0m, 7);
                    tracked.LastValuationDate = lastHist.Date;
                    updatedCount++;

                    _logger.LogInformation(
                        "💹 VL synchronisée : {ISIN} → {VL:F5} au {Date:dd/MM/yyyy}",
                        tracked.ISIN,
                        tracked.LastValuationAmount,
                        tracked.LastValuationDate);

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "❌ Erreur UpdateValuationsAsync sur support {SupportId}",
                        s.Id);
                }
            }

            _logger.LogInformation(
                "✅ UpdateValuationsAsync terminé : {Count} supports synchronisés",
                updatedCount);
        }

        // 3️⃣ Dénouement des opérations
        public async Task ProcessPendingOperationsAsync()
        {
            _logger.LogInformation("▶️ Début ProcessPendingOperationsAsync");

            var pendingOps = await _context.Operations
                .Include(o => o.PaymentDetail)
                .Include(o => o.AdvanceDetail)
                .Include(o => o.Allocations)
                    .ThenInclude(a => a.Support)
                .Where(o => o.Status == OperationStatus.Pending)
                .OrderBy(o => o.OperationDate)
                .ThenBy(o => o.Id) // 🔒 déterminisme
                .ToListAsync();

            if (!pendingOps.Any())
            {
                _logger.LogInformation("Aucune opération en attente.");
                return;
            }

            var impactedContracts = new HashSet<int>();

            foreach (var op in pendingOps)
            {
                if (op.Type == OperationType.ScheduledPayment)
                {
                    var payment = op.PaymentDetail;
                    if (payment == null || payment.ScheduleStatus == OperationScheduleStatus.Suspended || payment.ScheduleStatus == OperationScheduleStatus.Stopped)
                    {
                        continue;
                    }

                    if (payment.StartDate.HasValue && payment.StartDate.Value.Date > DateTime.UtcNow.Date)
                    {
                        continue;
                    }
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation(
                        "🧩 Traitement opération #{Id} – {Type} – {Date}",
                        op.Id, op.Type, op.OperationDate.ToShortDateString());

                    var finalized = new List<(OperationSupportAllocation alloc, decimal nav, DateTime date, decimal shares)>();
                    bool ready = true;

                    foreach (var alloc in op.Allocations)
                    {
                        var navRow = await _context.SupportHistoricalDatas
                            .Where(h =>
                                h.FinancialSupportId == alloc.SupportId &&
                                h.Date > op.OperationDate.Date)
                            .OrderBy(h => h.Date)
                            .FirstOrDefaultAsync();

                        if (navRow == null)
                        {
                            ready = false;
                            break;
                        }

                        var nav = navRow.Nav ?? 0m;

                        var shares = nav > 0 && alloc.Amount.HasValue
                            ? NumericPolicy.RoundShares(alloc.Amount.Value / nav)
                            : 0m;

                        finalized.Add((alloc, nav, navRow.Date, shares));
                    }

                    if (!ready)
                    {
                        await transaction.RollbackAsync();
                        continue;
                    }

                    if (op.Type == OperationType.Arbitrage || op.Type == OperationType.ScheduledArbitrage)
                    {
                        var sourceHoldingMap = await _context.ContractSupportHoldings
                            .Where(h => h.ContractId == op.ContractId)
                            .ToDictionaryAsync(
                                h => (h.SupportId, h.CompartmentId),
                                h => h.TotalShares
                            );

                        // 1) Cap des SOURCES à la quantité réellement détenue
                        for (var i = 0; i < finalized.Count; i++)
                        {
                            var item = finalized[i];
                            var alloc = item.alloc;

                            if (alloc.Flow != OperationFlow.Source || alloc.CompartmentId == null)
                                continue;

                            sourceHoldingMap.TryGetValue(
                                (alloc.SupportId, alloc.CompartmentId.Value),
                                out var heldShares
                            );

                            var requestedShares = item.shares;
                            var effectiveShares = Math.Min(requestedShares, heldShares);

                            // Si le résidu est négligeable (< 0.01 parts OU < 0.1% de la position),
                            // on force la liquidation totale pour éviter les miettes.
                            var residualShares = heldShares - effectiveShares;
                            if (residualShares > 0m && heldShares > 0m &&
                                (residualShares < 0.01m || residualShares / heldShares < 0.001m))
                            {
                                _logger.LogInformation(
                                    "🧹 Arbitrage {OpId} snap liquidation totale support={SupportId} compartment={CompartmentId}: résidu={Residual} parts absorbé",
                                    op.Id,
                                    alloc.SupportId,
                                    alloc.CompartmentId,
                                    residualShares
                                );
                                effectiveShares = heldShares;
                            }
                            else if (effectiveShares < requestedShares)
                            {
                                _logger.LogWarning(
                                    "⚠️ Arbitrage {OpId} cap SOURCE support={SupportId} compartment={CompartmentId}: requested={Requested} held={Held}",
                                    op.Id,
                                    alloc.SupportId,
                                    alloc.CompartmentId,
                                    requestedShares,
                                    heldShares
                                );
                            }

                            var effectiveAmount = item.nav > 0
                                ? NumericPolicy.RoundMoney(effectiveShares * item.nav)
                                : 0m;

                            alloc.Amount = effectiveAmount;
                            finalized[i] = (alloc, item.nav, item.date, effectiveShares);
                        }

                        // 2) Rééquilibrage des TARGETS par poche sur le budget SOURCE réel
                        var compartments = finalized
                            .Where(f => f.alloc.CompartmentId.HasValue)
                            .Select(f => f.alloc.CompartmentId!.Value)
                            .Distinct()
                            .ToList();

                        foreach (var compartmentId in compartments)
                        {
                            var sourceBudget = finalized
                                .Where(f => f.alloc.Flow == OperationFlow.Source && f.alloc.CompartmentId == compartmentId)
                                .Sum(f => f.alloc.Amount ?? 0m);

                            var targetIndexes = finalized
                                .Select((f, idx) => new { f, idx })
                                .Where(x => x.f.alloc.Flow == OperationFlow.Target && x.f.alloc.CompartmentId == compartmentId)
                                .Select(x => x.idx)
                                .ToList();

                            if (!targetIndexes.Any())
                                continue;

                            var originalTargetTotal = targetIndexes.Sum(idx => finalized[idx].alloc.Amount ?? 0m);

                            if (sourceBudget <= 0m || originalTargetTotal <= 0m)
                            {
                                foreach (var idx in targetIndexes)
                                {
                                    var item = finalized[idx];
                                    item.alloc.Amount = 0m;
                                    finalized[idx] = (item.alloc, item.nav, item.date, 0m);
                                }

                                continue;
                            }

                            decimal distributed = 0m;

                            for (var j = 0; j < targetIndexes.Count; j++)
                            {
                                var idx = targetIndexes[j];
                                var item = finalized[idx];
                                var originalAmount = item.alloc.Amount ?? 0m;

                                var nextAmount = j == targetIndexes.Count - 1
                                    ? NumericPolicy.RoundMoney(sourceBudget - distributed)
                                    : NumericPolicy.RoundMoney(sourceBudget * (originalAmount / originalTargetTotal));

                                distributed += nextAmount;

                                item.alloc.Amount = nextAmount;

                                var nextShares = item.nav > 0m
                                    ? NumericPolicy.RoundShares(nextAmount / item.nav)
                                    : 0m;

                                finalized[idx] = (item.alloc, item.nav, item.date, nextShares);
                            }
                        }
                    }

                    foreach (var f in finalized)
                    {
                        f.alloc.NavAtOperation = f.nav;
                        f.alloc.NavDateAtOperation = f.date;
                        f.alloc.Shares = f.shares;
                        f.alloc.EstimatedNav = null;
                        f.alloc.EstimatedShares = null;
                    }

                    op.RequestedAmount ??= op.Amount;
                    var executedAmount = ResolveExecutedAmount(op, finalized.Select(f => f.alloc));
                    if (finalized.Any())
                    {
                        op.ExecutedAmount = executedAmount;
                        op.Amount = executedAmount;
                    }

                    // ✅ Frais d'opération : résolution & application (par support/poche)
                    var feeGroups = finalized
                        .Select(f => (SupportId: f.alloc.SupportId, CompartmentId: f.alloc.CompartmentId))
                        .Distinct()
                        .ToList();

                    var contract = await _context.Contracts
                        .Include(c => c.Product)
                        .FirstOrDefaultAsync(c => c.Id == op.ContractId);

                    if (contract == null)
                    {
                        _logger.LogWarning("⚠️ Contrat introuvable pour opération {OpId} contractId={ContractId}", op.Id, op.ContractId);
                        await transaction.RollbackAsync();
                        continue;
                    }

                    foreach (var g in feeGroups)
                    {
                        var supportId = g.SupportId;
                        var compartmentId = g.CompartmentId;

                        if (!compartmentId.HasValue)
                            continue;

                        var compartmentIdValue = compartmentId.Value;

                        var found = finalized.FirstOrDefault(x => x.alloc.SupportId == supportId);
                        var support = found.alloc?.Support;

                        var resolvedFees = _operationFeePolicyResolver.ResolveOperationFees(contract, support, op.Type, op.OperationDate);
                        if (resolvedFees == null || !resolvedFees.Any())
                            continue;

                        foreach (var rf in resolvedFees)
                        {
                            decimal baseAmount = 0m;
                            if (rf.ApplyOn == api.Models.Enum.FeeApplyOn.Source)
                            {
                                baseAmount = finalized
                                    .Where(x => x.alloc.Flow == OperationFlow.Source && x.alloc.SupportId == supportId && x.alloc.CompartmentId == compartmentId)
                                    .Sum(x => x.alloc.Amount ?? 0m);
                            }
                            else
                            {
                                baseAmount = finalized
                                    .Where(x => x.alloc.Flow == OperationFlow.Target && x.alloc.SupportId == supportId && x.alloc.CompartmentId == compartmentId)
                                    .Sum(x => x.alloc.Amount ?? 0m);
                            }

                            if (baseAmount <= 0m)
                                continue;

                            var feeAmount = rf.Mode == api.Models.Enum.FeeAmountMode.Percentage
                                ? NumericPolicy.RoundMoney(baseAmount * rf.Rate / 100m)
                                : rf.FixedAmount;

                            if (feeAmount <= 0m)
                                continue;

                            var currentHolding = await _context.ContractSupportHoldings
                                .AsNoTracking()
                                .FirstOrDefaultAsync(h => h.ContractId == contract.Id && h.SupportId == supportId && h.CompartmentId == compartmentId);

                            if (currentHolding == null || currentHolding.TotalShares <= 0m)
                                continue;

                            var postingNav = await GetPostingNavAsync(support ?? new FinancialSupport { Id = supportId }, supportId, op.OperationDate.AddDays(-1));
                            if (postingNav <= 0m)
                                continue;

                            var sharesToRemove = Math.Min(currentHolding.TotalShares, NumericPolicy.RoundShares(feeAmount / postingNav));
                            if (sharesToRemove <= 0m)
                                continue;

                            var feeOperation = new Operation
                            {
                                ContractId = contract.Id,
                                Type = OperationType.OperationFee,
                                SourceOperationId = op.Id,
                                OperationDate = op.OperationDate,
                                ExecutionDate = op.OperationDate,
                                Status = OperationStatus.Executed,
                                Amount = feeAmount,
                                RequestedAmount = feeAmount,
                                ExecutedAmount = feeAmount,
                                Currency = contract.Currency,
                                Allocations = new List<OperationSupportAllocation>
                                {
                                    new OperationSupportAllocation
                                    {
                                        SupportId = supportId,
                                        Amount = feeAmount,
                                        Shares = sharesToRemove,
                                        NavAtOperation = postingNav,
                                        NavDateAtOperation = op.OperationDate.AddDays(-1),
                                        CompartmentId = compartmentIdValue
                                    }
                                },
                                CreatedDate = DateTime.UtcNow,
                                UpdatedDate = DateTime.UtcNow,
                            };

                            await _context.Operations.AddAsync(feeOperation);
                            await _context.ContractSupportFeeApplications.AddAsync(new ContractSupportFeeApplication
                            {
                                ContractId = contract.Id,
                                FeeOperation = feeOperation,
                                SourceOperationId = op.Id,
                                FeeNature = MapFeeNatureFromOperation(op.Type),
                                CompartmentId = compartmentIdValue,
                                SupportId = supportId,
                                ApplyOn = rf.ApplyOn,
                                BaseAmount = baseAmount,
                                FeeMode = rf.Mode,
                                AppliedRate = rf.Mode == FeeAmountMode.Percentage ? rf.Rate : null,
                                FeeAmount = feeAmount,
                                FeeShares = sharesToRemove,
                                NavUsed = postingNav,
                                NavDateUsed = op.OperationDate.AddDays(-1),
                                PolicySource = rf.Source,
                                PolicyId = null,
                                EffectiveDate = op.OperationDate.Date,
                                CreatedDate = DateTime.UtcNow,
                            });
                            await _operationApplier.ApplyAsync(feeOperation, _context);
                            await _context.SaveChangesAsync();
                        }
                    }

                    if (!finalized.Any())
                    {
                        var canExecuteWithoutAllocations =
                            op.Type == OperationType.Advance ||
                            op.Type == OperationType.AdvanceRepayment ||
                            op.Type == OperationType.Succession ||
                            op.Type == OperationType.Donation ||
                            op.Type == OperationType.BeneficiaryChange ||
                            op.Type == OperationType.Pledge ||
                            op.Type == OperationType.ConversionToAnnuity;

                        if (!canExecuteWithoutAllocations)
                            throw new InvalidOperationException($"Opération {op.Id} sans allocations.");

                        if (op.Type is OperationType.Advance or OperationType.AdvanceRepayment)
                            await _advanceOperationService.ApplyAsync(op);

                        op.RequestedAmount ??= op.Amount;
                        op.ExecutedAmount = op.Amount;
                        op.Status = OperationStatus.Executed;
                        op.ExecutionDate = op.OperationDate;
                        op.UpdatedDate = DateTime.UtcNow;

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        impactedContracts.Add(op.ContractId);
                        _logger.LogInformation("✔ Opération #{Id} exécutée sans allocations", op.Id);
                        continue;
                    }

                    op.Status = OperationStatus.Executed;
                    op.ExecutionDate = finalized.Max(f => f.date);
                    op.UpdatedDate = DateTime.UtcNow;

                    await _operationApplier.ApplyAsync(op, _context);

                    if (op.Type == OperationType.ScheduledPayment &&
                        op.PaymentDetail != null &&
                        op.PaymentDetail.ScheduleStatus == OperationScheduleStatus.Active)
                    {
                        var nextOperationDate = ComputeNextScheduleDate(op.OperationDate, op.PaymentDetail.Frequency);
                        if (nextOperationDate.HasValue)
                        {
                            var nextOperation = new Operation
                            {
                                ContractId = op.ContractId,
                                Type = OperationType.ScheduledPayment,
                                Status = OperationStatus.Pending,
                                OperationDate = nextOperationDate.Value,
                                Amount = op.RequestedAmount ?? op.Amount,
                                RequestedAmount = op.RequestedAmount ?? op.Amount,
                                ExecutedAmount = null,
                                Currency = op.Currency,
                                CreatedDate = DateTime.UtcNow,
                                UpdatedDate = DateTime.UtcNow,
                                PaymentDetail = new PaymentDetail
                                {
                                    PaymentMethod = op.PaymentDetail.PaymentMethod,
                                    Frequency = op.PaymentDetail.Frequency,
                                    StartDate = op.PaymentDetail.StartDate,
                                    ScheduleStatus = op.PaymentDetail.ScheduleStatus,
                                    ScheduleGroupId = op.PaymentDetail.ScheduleGroupId,
                                    SourceOfFunds = op.PaymentDetail.SourceOfFunds,
                                    Amount = op.PaymentDetail.Amount,
                                },
                                Allocations = op.Allocations.Select(a => new OperationSupportAllocation
                                {
                                    SupportId = a.SupportId,
                                    CompartmentId = a.CompartmentId,
                                    Amount = a.Amount,
                                    Percentage = a.Percentage,
                                    Flow = a.Flow,
                                }).ToList()
                            };

                            _context.Operations.Add(nextOperation);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    impactedContracts.Add(op.ContractId);

                    _logger.LogInformation("✔ Opération #{Id} exécutée", op.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "❌ Erreur traitement opération {OpId}", op.Id);
                }
            }

            // 🔥 Revalorisation unique par contrat
            foreach (var contractId in impactedContracts)
            {
                await _valuationService.ComputeContractValueAsync(contractId);
            }

            _logger.LogInformation("🏁 ProcessPendingOperationsAsync terminé.");
        }

        private static decimal ResolveExecutedAmount(
            Operation operation,
            IEnumerable<OperationSupportAllocation> allocations)
        {
            var rows = allocations.ToList();
            if (operation.Type is OperationType.Arbitrage or OperationType.ScheduledArbitrage)
            {
                return NumericPolicy.RoundMoney(rows
                    .Where(a => a.Flow == OperationFlow.Source)
                    .Sum(a => a.Amount ?? 0m));
            }

            return NumericPolicy.RoundMoney(rows.Sum(a => a.Amount ?? 0m));
        }

        // 4️⃣ Placeholder règles de gestion
        public async Task ApplyRulesAsync()
        {
            _logger.LogInformation("▶️ ApplyRulesAsync (pas encore implémenté)");
            await Task.CompletedTask;
        }

        // 5️⃣ Frais de gestion automatiques
        public async Task ApplyManagementFeesAsync()
        {
            var runDate = DateTime.UtcNow.Date;
            _logger.LogInformation("▶️ Début ApplyManagementFeesAsync pour la date {RunDate:yyyy-MM-dd}", runDate);

            var contracts = await _context.Contracts
                .Include(c => c.Product)
                    .ThenInclude(p => p!.ManagementFeePolicy)
                .Include(c => c.Supports).ThenInclude(s => s.Support)
                .ToListAsync();

            var accrualStates = await _context.ContractManagementFeeAccruals
                .ToDictionaryAsync(a => (a.ContractId, a.SupportId, a.CompartmentId));

            foreach (var contract in contracts)
            {
                foreach (var alloc in contract.Supports)
                {
                    try
                    {
                        if (alloc.Support == null)
                            continue;

                        var policy = _managementFeePolicyResolver.Resolve(contract, alloc.Support, runDate);
                        if (policy == null)
                            continue;

                        var state = GetOrCreateAccrualState(accrualStates, contract.Id, alloc.SupportId, alloc.CompartmentId);
                        var accrualStart = GetAccrualStartDate(contract, policy, state);
                        var accrualEnd = GetAccrualEndDate(policy, runDate);

                        if (accrualStart <= accrualEnd)
                        {
                            var computation = await ComputeDailyAccrualAsync(contract, alloc, policy, accrualStart, accrualEnd);
                            if (computation.FeeAmount > 0m)
                            {
                                ApplyAccrualComputation(state, computation);
                                _logger.LogInformation(
                                    "📘 Accrual frais contrat {ContractId}, support {SupportId}, poche {CompartmentId} : +{Amount:F7} du {Start:yyyy-MM-dd} au {End:yyyy-MM-dd}",
                                    contract.Id,
                                    alloc.SupportId,
                                    alloc.CompartmentId,
                                    computation.FeeAmount,
                                    accrualStart,
                                    accrualEnd);
                            }

                            state.LastAccruedDate = accrualEnd;
                            state.UpdatedDate = DateTime.UtcNow;
                        }

                        if (policy.PostingMode == ManagementFeePostingMode.NetServedYield)
                        {
                            await _context.SaveChangesAsync();
                            _logger.LogInformation(
                                "⏭️ Accrual conservé sans prélèvement en parts pour contrat {ContractId}, support {SupportId} : mode {PostingMode}",
                                contract.Id,
                                alloc.SupportId,
                                policy.PostingMode);
                            continue;
                        }

                        if (!ShouldPostForRun(policy, runDate, state.LastPostedDate))
                            continue;

                        var feeAmount = NumericPolicy.RoundMoney(state.AccruedAmount);
                        if (feeAmount <= 0m)
                            continue;

                        var currentHolding = await _context.ContractSupportHoldings
                            .AsNoTracking()
                            .FirstOrDefaultAsync(h =>
                                h.ContractId == contract.Id &&
                                h.SupportId == alloc.SupportId &&
                                h.CompartmentId == alloc.CompartmentId);

                        if (currentHolding == null || currentHolding.TotalShares <= 0m)
                            continue;

                        var postingNav = await GetPostingNavAsync(alloc.Support, alloc.SupportId, runDate.AddDays(-1));
                        if (postingNav <= 0m)
                            continue;

                        var sharesToRemove = Math.Min(currentHolding.TotalShares, NumericPolicy.RoundShares(feeAmount / postingNav));

                        if (sharesToRemove <= 0m)
                            continue;

                        var feeOperation = new Operation
                        {
                            ContractId = contract.Id,
                            Type = OperationType.ManagementFee,
                            OperationDate = runDate,
                            ExecutionDate = runDate,
                            Status = OperationStatus.Executed,
                            Amount = feeAmount,
                            RequestedAmount = feeAmount,
                            ExecutedAmount = feeAmount,
                            Currency = contract.Currency,
                            Allocations = new List<OperationSupportAllocation>
                            {
                                new OperationSupportAllocation
                                {
                                    SupportId = alloc.SupportId,
                                    Amount = feeAmount,
                                    Shares = sharesToRemove,
                                    NavAtOperation = postingNav,
                                    NavDateAtOperation = runDate.AddDays(-1),
                                    CompartmentId = alloc.CompartmentId
                                }
                            },
                            CreatedDate = DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow,
                        };

                        await _context.Operations.AddAsync(feeOperation);
                        await _context.ContractSupportFeeApplications.AddAsync(new ContractSupportFeeApplication
                        {
                            ContractId = contract.Id,
                            FeeOperation = feeOperation,
                            SourceOperationId = null,
                            FeeNature = ContractSupportFeeNature.ManagementFee,
                            CompartmentId = alloc.CompartmentId,
                            SupportId = alloc.SupportId,
                            ApplyOn = FeeApplyOn.Target,
                            BaseAmount = GetAverageAccrualBase(state),
                            FeeMode = FeeAmountMode.Percentage,
                            AppliedRate = policy.AnnualRate,
                            RateBase = policy.RateBase,
                            Frequency = policy.Frequency,
                            ProrataMethod = policy.ProrataMethod,
                            PostingMode = policy.PostingMode,
                            AccrualStartDate = state.AccrualStartDate,
                            AccrualEndDate = state.LastAccruedDate,
                            AccruedDays = state.AccruedDays,
                            FeeAmount = feeAmount,
                            FeeShares = sharesToRemove,
                            NavUsed = postingNav,
                            NavDateUsed = runDate.AddDays(-1),
                            PolicySource = policy.Source,
                            PolicyId = null,
                            EffectiveDate = runDate,
                            CreatedDate = DateTime.UtcNow,
                        });

                        // 🔥 APPLIQUER L’EFFET FINANCIER
                        await _operationApplier.ApplyAsync(feeOperation, _context);

                        state.AccruedAmount = 0m;
                        state.AccumulatedBaseAmount = 0m;
                        state.AccruedDays = 0;
                        state.AccrualStartDate = null;
                        state.LastPostedDate = runDate;
                        state.UpdatedDate = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        _logger.LogInformation(
                            "💸 Frais postés : contrat {ContractId}, support {SupportId}, poche {CompartmentId} → {FeeAmount:F7} ({Shares:F7} parts), source={Source}",
                            contract.Id,
                            alloc.SupportId,
                            alloc.CompartmentId,
                            feeAmount,
                            sharesToRemove,
                            policy.Source
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "❌ Erreur ApplyManagementFeesAsync sur contrat {ContractId}, support {SupportId}, poche {CompartmentId}",
                            contract.Id,
                            alloc.SupportId,
                            alloc.CompartmentId);
                    }
                }
            }

            _logger.LogInformation("✅ ApplyManagementFeesAsync terminé");
        }

        private ContractManagementFeeAccrual GetOrCreateAccrualState(
            IDictionary<(int ContractId, int SupportId, int CompartmentId), ContractManagementFeeAccrual> accrualStates,
            int contractId,
            int supportId,
            int compartmentId)
        {
            if (accrualStates.TryGetValue((contractId, supportId, compartmentId), out var existingState))
            {
                return existingState;
            }

            var newState = new ContractManagementFeeAccrual
            {
                ContractId = contractId,
                SupportId = supportId,
                CompartmentId = compartmentId,
                AccruedAmount = 0m,
                AccumulatedBaseAmount = 0m,
                AccruedDays = 0,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
            };

            accrualStates[(contractId, supportId, compartmentId)] = newState;
            _context.ContractManagementFeeAccruals.Add(newState);
            return newState;
        }

        private static DateTime GetAccrualStartDate(Contract contract, ResolvedManagementFeePolicy policy, ContractManagementFeeAccrual state)
        {
            var nextAccrualDate = state.LastAccruedDate?.Date.AddDays(1) ?? contract.DateEffect.Date;
            return MaxDate(policy.EffectiveDate.Date, nextAccrualDate);
        }

        private static DateTime GetAccrualEndDate(ResolvedManagementFeePolicy policy, DateTime runDateUtc)
        {
            var yesterday = runDateUtc.Date.AddDays(-1);
            if (policy.EndDate is DateTime endDate)
            {
                return MinDate(yesterday, endDate.Date);
            }

            return yesterday;
        }

        private async Task<ManagementFeeAccrualComputation> ComputeDailyAccrualAsync(
            Contract contract,
            FinancialSupportAllocation allocation,
            ResolvedManagementFeePolicy policy,
            DateTime startDate,
            DateTime endDate)
        {
            if (startDate > endDate)
                return new ManagementFeeAccrualComputation(0m, 0m, 0, null, null);

            var operationAllocations = await _context.OperationSupportAllocations
                .Include(a => a.Operation)
                .Where(a =>
                    a.SupportId == allocation.SupportId &&
                    a.CompartmentId == allocation.CompartmentId &&
                    a.Operation != null &&
                    a.Operation.ContractId == contract.Id &&
                    a.Operation.Status == OperationStatus.Executed &&
                    a.Operation.ExecutionDate != null &&
                    a.Operation.ExecutionDate <= endDate)
                .AsNoTracking()
                .ToListAsync();

            var openingShares = operationAllocations
                .Where(a => a.Operation!.ExecutionDate!.Value.Date < startDate)
                .Sum(GetSignedShares);

            var dailyShareDeltas = operationAllocations
                .Where(a => a.Operation!.ExecutionDate!.Value.Date >= startDate)
                .GroupBy(a => a.Operation!.ExecutionDate!.Value.Date)
                .ToDictionary(g => g.Key, g => g.Sum(GetSignedShares));

            var historicalRows = await _context.SupportHistoricalDatas
                .Where(h =>
                    h.FinancialSupportId == allocation.SupportId &&
                    h.Date >= startDate &&
                    h.Date <= endDate)
                .AsNoTracking()
                .OrderBy(h => h.Date)
                .ToListAsync();

            var navByDate = historicalRows
                .Select(h => new { Date = h.Date.Date, Nav = h.Nav ?? h.Close })
                .Where(x => x.Nav.HasValue && x.Nav.Value > 0m)
                .GroupBy(x => x.Date)
                .ToDictionary(g => g.Key, g => g.Last().Nav!.Value);

            var fallbackNav = await _context.SupportHistoricalDatas
                .Where(h =>
                    h.FinancialSupportId == allocation.SupportId &&
                    h.Date < startDate &&
                    (h.Nav != null || h.Close != null))
                .OrderByDescending(h => h.Date)
                .Select(h => h.Nav ?? h.Close)
                .FirstOrDefaultAsync();

            var currentNav = fallbackNav > 0m
                ? fallbackNav
                : allocation.Support?.LastValuationAmount;

            var shares = openingShares;
            var accruedAmount = 0m;
            var baseAmountDays = 0m;
            var chargeableDays = 0;
            DateTime? firstChargeableDate = null;
            DateTime? lastChargeableDate = null;

            for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
            {
                if (dailyShareDeltas.TryGetValue(day, out var dayDelta))
                {
                    shares += dayDelta;
                }

                if (navByDate.TryGetValue(day, out var dayNav))
                {
                    currentNav = dayNav;
                }

                if (shares <= 0m || currentNav is not decimal nav || nav <= 0m)
                    continue;

                var dailyBase = shares * nav;
                baseAmountDays += dailyBase;
                chargeableDays++;
                firstChargeableDate ??= day;
                lastChargeableDate = day;

                var dailyAmount = NumericPolicy.RoundAmount(dailyBase * ComputeDailyRate(policy, day));
                accruedAmount = NumericPolicy.RoundAmount(accruedAmount + dailyAmount);
            }

            return new ManagementFeeAccrualComputation(
                accruedAmount,
                NumericPolicy.RoundAmount(baseAmountDays),
                chargeableDays,
                firstChargeableDate,
                lastChargeableDate);
        }

        private static void ApplyAccrualComputation(
            ContractManagementFeeAccrual state,
            ManagementFeeAccrualComputation computation)
        {
            state.AccruedAmount = NumericPolicy.RoundAmount(state.AccruedAmount + computation.FeeAmount);
            state.AccumulatedBaseAmount = NumericPolicy.RoundAmount(
                state.AccumulatedBaseAmount + computation.BaseAmountDays);
            state.AccruedDays += computation.ChargeableDays;
            state.AccrualStartDate ??= computation.FirstChargeableDate;
        }

        private static decimal GetAverageAccrualBase(ContractManagementFeeAccrual state)
            => state.AccruedDays > 0
                ? NumericPolicy.RoundAmount(state.AccumulatedBaseAmount / state.AccruedDays)
                : 0m;

        private async Task<decimal> GetPostingNavAsync(FinancialSupport support, int supportId, DateTime valuationDate)
        {
            decimal? historicalNav = await _context.SupportHistoricalDatas
                .Where(h =>
                    h.FinancialSupportId == supportId &&
                    h.Date <= valuationDate &&
                    (h.Nav != null || h.Close != null))
                .OrderByDescending(h => h.Date)
                .Select(h => h.Nav ?? h.Close)
                .FirstOrDefaultAsync();

            if (historicalNav is decimal value && value > 0m)
            {
                return value;
            }

            return support.LastValuationAmount ?? 0m;
        }

        private static bool ShouldPostForRun(ResolvedManagementFeePolicy policy, DateTime runDateUtc, DateTime? lastPostedDateUtc)
        {
            var runDate = runDateUtc.Date;

            if (lastPostedDateUtc?.Date == runDate)
                return false;

            if (policy.EndDate?.Date.AddDays(1) == runDate)
                return true;

            return policy.Frequency switch
            {
                ManagementFeeFrequency.Monthly => runDate.Day == 1,
                ManagementFeeFrequency.Quarterly => runDate.Day == 1 && runDate.Month is 1 or 4 or 7 or 10,
                ManagementFeeFrequency.Yearly => runDate.Day == 1 && runDate.Month == 1,
                _ => false,
            };
        }

        private static decimal ComputeDailyRate(ResolvedManagementFeePolicy policy, DateTime accrualDateUtc)
        {
            var annualRate = policy.AnnualRate / 100m;

            if (policy.ProrataMethod == ManagementFeeProrataMethod.Actual365)
            {
                return annualRate / 365m;
            }

            var periodsPerYear = policy.Frequency switch
            {
                ManagementFeeFrequency.Monthly => 12,
                ManagementFeeFrequency.Quarterly => 4,
                ManagementFeeFrequency.Yearly => 1,
                _ => 1,
            };

            var periodRate = policy.ProrataMethod == ManagementFeeProrataMethod.ExactPeriodicCompounded
                ? (decimal)(Math.Pow(1d + (double)annualRate, 1d / periodsPerYear) - 1d)
                : policy.Frequency switch
                {
                    ManagementFeeFrequency.Monthly => annualRate / 12m,
                    ManagementFeeFrequency.Quarterly => annualRate / 4m,
                    ManagementFeeFrequency.Yearly => annualRate,
                    _ => 0m,
                };

            var (periodStart, periodEnd) = GetPeriodBounds(policy.Frequency, accrualDateUtc.Date);
            var periodDays = (periodEnd - periodStart).Days + 1;

            return periodDays > 0 ? periodRate / periodDays : 0m;
        }

        private static DateTime? ComputeNextScheduleDate(DateTime currentDate, string? frequency)
        {
            return frequency?.ToLowerInvariant() switch
            {
                "monthly" => currentDate.AddMonths(1),
                "quarterly" => currentDate.AddMonths(3),
                "yearly" => currentDate.AddYears(1),
                _ => null,
            };
        }

        private static (DateTime Start, DateTime End) GetPeriodBounds(ManagementFeeFrequency frequency, DateTime referenceDate)
        {
            return frequency switch
            {
                ManagementFeeFrequency.Monthly =>
                    (new DateTime(referenceDate.Year, referenceDate.Month, 1),
                     new DateTime(referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month))),
                ManagementFeeFrequency.Quarterly => GetQuarterBounds(referenceDate),
                ManagementFeeFrequency.Yearly =>
                    (new DateTime(referenceDate.Year, 1, 1), new DateTime(referenceDate.Year, 12, 31)),
                _ => (referenceDate.Date, referenceDate.Date),
            };
        }

        private static (DateTime Start, DateTime End) GetQuarterBounds(DateTime referenceDate)
        {
            var quarterStartMonth = ((referenceDate.Month - 1) / 3) * 3 + 1;
            var start = new DateTime(referenceDate.Year, quarterStartMonth, 1);
            var end = start.AddMonths(3).AddDays(-1);
            return (start, end);
        }

        private static decimal GetSignedShares(OperationSupportAllocation allocation)
        {
            var shares = allocation.Shares ?? allocation.EstimatedShares ?? 0m;
            if (shares <= 0m || allocation.Operation == null)
                return 0m;

            if (allocation.Operation.Type == OperationType.Arbitrage || allocation.Operation.Type == OperationType.ScheduledArbitrage)
            {
                return allocation.Flow == OperationFlow.Target ? shares : -shares;
            }

            if (allocation.Operation.Type.IsPayment())
                return shares;

            if (allocation.Operation.Type.IsWithdrawal())
                return -shares;

            return 0m;
        }

        private static ContractSupportFeeNature MapFeeNatureFromOperation(OperationType operationType)
        {
            return operationType switch
            {
                OperationType.InitialPayment or OperationType.FreePayment or OperationType.ScheduledPayment
                    => ContractSupportFeeNature.ContributionFee,
                OperationType.PartialWithdrawal or OperationType.TotalWithdrawal or OperationType.ScheduledWithdrawal
                    => ContractSupportFeeNature.WithdrawalFee,
                OperationType.Arbitrage or OperationType.ScheduledArbitrage
                    => ContractSupportFeeNature.ArbitrageFee,
                _ => ContractSupportFeeNature.OtherFee,
            };
        }

        private static DateTime MaxDate(DateTime left, DateTime right)
            => left >= right ? left : right;

        private static DateTime MinDate(DateTime left, DateTime right)
            => left <= right ? left : right;

        public async Task RebuildContractAsync(int contractId)
        {
            _logger.LogWarning("♻️ Démarrage REBUILD COMPLET du contrat {ContractId}", contractId);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // =============================================================
                // 1️⃣ PURGE TOTALE DES FSA ET DES HOLDINGS
                // =============================================================
                var oldFsas = await _context.FinancialSupportAllocations
                    .Where(x => x.ContractId == contractId)
                    .ToListAsync();

                var oldHoldings = await _context.ContractSupportHoldings
                    .Where(x => x.ContractId == contractId)
                    .ToListAsync();

                _context.FinancialSupportAllocations.RemoveRange(oldFsas);
                _context.ContractSupportHoldings.RemoveRange(oldHoldings);

                await _context.SaveChangesAsync();

                // =============================================================
                // 2️⃣ CHARGER TOUTES LES OPÉRATIONS EXECUTED
                // =============================================================
                var executedOps = await _context.Operations
                    .Include(o => o.Allocations)
                        .ThenInclude(a => a.Support)
                    .Where(o =>
                        o.ContractId == contractId &&
                        o.Status == OperationStatus.Executed)
                    .OrderBy(o => o.ExecutionDate ?? o.OperationDate)
                    .ThenBy(o => o.Id)
                    .ToListAsync();

                if (!executedOps.Any())
                {
                    _logger.LogWarning("⚠️ Aucun historique EXECUTED — rebuild sans effet.");
                    await transaction.CommitAsync();
                    return;
                }

                // =============================================================
                // 3️⃣ REJEU VIA OPERATION APPLIER (SOURCE UNIQUE)
                // =============================================================
                foreach (var op in executedOps)
                {
                    await _operationApplier.ApplyAsync(op, _context);
                }

                await _context.SaveChangesAsync();

                // =============================================================
                // 4️⃣ REVALORISATION DU CONTRAT
                // =============================================================
                await _valuationService.ComputeContractValueAsync(contractId);

                await transaction.CommitAsync();

                _logger.LogInformation("✅ REBUILD COMPLET terminé pour contrat {ContractId}", contractId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Erreur durant RebuildContractAsync pour contrat {ContractId}", contractId);
                throw;
            }
        }

        public async Task ApplyOperationAsync(Operation op)
        {
            if (op == null)
                throw new ArgumentNullException(nameof(op));

            if (op.Status != OperationStatus.Executed)
                throw new InvalidOperationException(
                    $"Impossible d’appliquer une opération non exécutée (ID={op.Id})."
                );

            // 🔒 Transaction = sécurité absolue
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🔄 S’assurer que les allocations sont bien chargées
                if (op.Allocations == null || !op.Allocations.Any())
                {
                    op = await _context.Operations
                        .Include(o => o.Allocations)
                        .ThenInclude(a => a.Support)
                        .FirstOrDefaultAsync(o => o.Id == op.Id)
                        ?? throw new InvalidOperationException(
                            $"Opération {op.Id} introuvable en base."
                        );
                }

                // 🔥 SOURCE UNIQUE DE VÉRITÉ
                await _operationApplier.ApplyAsync(op, _context);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ContractFeeRecalculationResult> RecalculateContractFeesAsync(int contractId)
        {
            _logger.LogWarning("♻️ Recalcul des frais démarré pour contrat {ContractId}", contractId);

            var contractExists = await _context.Contracts.AnyAsync(c => c.Id == contractId);
            if (!contractExists)
                throw new InvalidOperationException($"Contrat {contractId} introuvable.");

            ContractFeeRecalculationResult? result = null;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var feeOps = await _context.Operations
                    .Where(o =>
                        o.ContractId == contractId &&
                        (o.Type == OperationType.ManagementFee || o.Type == OperationType.OperationFee))
                    .ToListAsync();

                var feeApps = await _context.ContractSupportFeeApplications
                    .Where(f => f.ContractId == contractId)
                    .ToListAsync();

                var accruals = await _context.ContractManagementFeeAccruals
                    .Where(a => a.ContractId == contractId)
                    .ToListAsync();

                if (feeApps.Count > 0)
                    _context.ContractSupportFeeApplications.RemoveRange(feeApps);

                if (feeOps.Count > 0)
                    _context.Operations.RemoveRange(feeOps);

                if (accruals.Count > 0)
                    _context.ContractManagementFeeAccruals.RemoveRange(accruals);

                var oldFsas = await _context.FinancialSupportAllocations
                    .Where(x => x.ContractId == contractId)
                    .ToListAsync();

                var oldHoldings = await _context.ContractSupportHoldings
                    .Where(x => x.ContractId == contractId)
                    .ToListAsync();

                _context.FinancialSupportAllocations.RemoveRange(oldFsas);
                _context.ContractSupportHoldings.RemoveRange(oldHoldings);

                await _context.SaveChangesAsync();

                var executedSourceOps = await _context.Operations
                    .Include(o => o.Allocations)
                        .ThenInclude(a => a.Support)
                    .Where(o =>
                        o.ContractId == contractId &&
                        o.Status == OperationStatus.Executed &&
                        (
                            o.Type == OperationType.InitialPayment ||
                            o.Type == OperationType.FreePayment ||
                            o.Type == OperationType.ScheduledPayment ||
                            o.Type == OperationType.PartialWithdrawal ||
                            o.Type == OperationType.TotalWithdrawal ||
                            o.Type == OperationType.ScheduledWithdrawal ||
                            o.Type == OperationType.Arbitrage ||
                            o.Type == OperationType.ScheduledArbitrage
                        ) &&
                        o.Allocations.Any())
                    .OrderBy(o => o.ExecutionDate ?? o.OperationDate)
                    .ThenBy(o => o.Id)
                    .ToListAsync();

                foreach (var op in executedSourceOps)
                {
                    var replayOperation = BuildReplayOperation(op);

                    if (op.Type == OperationType.PartialWithdrawal ||
                        op.Type == OperationType.TotalWithdrawal ||
                        op.Type == OperationType.ScheduledWithdrawal)
                    {
                        await NormalizeWithdrawalAllocationsForReplayAsync(replayOperation);
                    }

                    await _operationApplier.ApplyAsync(replayOperation, _context);
                }

                await _context.SaveChangesAsync();

                var contract = await _context.Contracts
                    .Include(c => c.Product)
                        .ThenInclude(p => p!.ManagementFeePolicy)
                    .FirstOrDefaultAsync(c => c.Id == contractId)
                    ?? throw new InvalidOperationException($"Contrat {contractId} introuvable après rebuild.");

                var replayedOperationFees = await ReplayOperationFeesForExecutedOperationsAsync(contract, executedSourceOps);
                var replayedManagementFees = await ReplayManagementFeesForContractAsync(contract, forcePost: true);

                await transaction.CommitAsync();

                result = new ContractFeeRecalculationResult
                {
                    ContractId = contractId,
                    RemovedFeeOperations = feeOps.Count,
                    RemovedFeeApplications = feeApps.Count,
                    ReplayedOperationFees = replayedOperationFees,
                    ReplayedManagementFees = replayedManagementFees,
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await _valuationService.ComputeContractValueAsync(contractId);

            _logger.LogInformation(
                "✅ Recalcul des frais terminé contrat {ContractId}: removedOps={RemovedOps}, removedApps={RemovedApps}, replayOpFees={ReplayOpFees}, replayMgmtFees={ReplayMgmtFees}",
                contractId,
                result!.RemovedFeeOperations,
                result.RemovedFeeApplications,
                result.ReplayedOperationFees,
                result.ReplayedManagementFees);

            return result;
        }

        private static Operation BuildReplayOperation(Operation source)
        {
            return new Operation
            {
                Id = source.Id,
                ContractId = source.ContractId,
                Type = source.Type,
                Status = source.Status,
                OperationDate = source.OperationDate,
                ExecutionDate = source.ExecutionDate,
                Amount = source.Amount,
                Currency = source.Currency,
                Allocations = source.Allocations
                    .Select(a => new OperationSupportAllocation
                    {
                        SupportId = a.SupportId,
                        CompartmentId = a.CompartmentId,
                        Amount = a.Amount,
                        Shares = a.Shares,
                        EstimatedShares = a.EstimatedShares,
                        Flow = a.Flow,
                        NavAtOperation = a.NavAtOperation,
                        NavDateAtOperation = a.NavDateAtOperation,
                        EstimatedNav = a.EstimatedNav,
                    })
                    .ToList(),
            };
        }

        private async Task NormalizeWithdrawalAllocationsForReplayAsync(Operation operation)
        {
            if (operation.Allocations == null || !operation.Allocations.Any())
                throw new InvalidOperationException($"RecalculateFees: opération {operation.Id} sans allocations de rachat.");

            var replayableAllocations = new List<OperationSupportAllocation>();

            foreach (var alloc in operation.Allocations)
            {
                if (!alloc.CompartmentId.HasValue)
                {
                    alloc.CompartmentId = _context.ContractSupportHoldings.Local
                        .Where(h =>
                            h.ContractId == operation.ContractId &&
                            h.SupportId == alloc.SupportId &&
                            h.TotalShares > 0m)
                        .OrderByDescending(h => h.TotalShares)
                        .Select(h => (int?)h.CompartmentId)
                        .FirstOrDefault();

                    if (!alloc.CompartmentId.HasValue)
                    {
                        alloc.CompartmentId = await _context.ContractSupportHoldings
                            .Where(h =>
                                h.ContractId == operation.ContractId &&
                                h.SupportId == alloc.SupportId &&
                                h.TotalShares > 0m)
                            .OrderByDescending(h => h.TotalShares)
                            .Select(h => (int?)h.CompartmentId)
                            .FirstOrDefaultAsync();
                    }

                    if (!alloc.CompartmentId.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"RecalculateFees: compartiment introuvable pour opId={operation.Id}, supportId={alloc.SupportId}.");
                    }
                }

                var requestedAmount = alloc.Amount ?? 0m;
                var requestedShares = alloc.Shares ?? alloc.EstimatedShares ?? 0m;

                if (requestedShares <= 0m)
                {
                    var nav = alloc.NavAtOperation ?? alloc.EstimatedNav;
                    if (nav is decimal positiveNav && positiveNav > 0m && requestedAmount > 0m)
                    {
                        requestedShares = NumericPolicy.RoundShares(requestedAmount / positiveNav);
                    }
                }

                if (requestedShares <= 0m || requestedAmount <= 0m)
                    throw new InvalidOperationException(
                        $"RecalculateFees: allocation rachat invalide opId={operation.Id}, supportId={alloc.SupportId}, compartmentId={alloc.CompartmentId}.");

                var holding = _context.ContractSupportHoldings.Local
                    .FirstOrDefault(h =>
                        h.ContractId == operation.ContractId &&
                        h.SupportId == alloc.SupportId &&
                        h.CompartmentId == alloc.CompartmentId.Value);

                if (holding == null)
                {
                    holding = await _context.ContractSupportHoldings
                        .FirstOrDefaultAsync(h =>
                            h.ContractId == operation.ContractId &&
                            h.SupportId == alloc.SupportId &&
                            h.CompartmentId == alloc.CompartmentId.Value);
                }

                var heldShares = holding?.TotalShares ?? 0m;
                if (heldShares <= 0m)
                {
                    throw new InvalidOperationException(
                        $"RecalculateFees: holdings insuffisants pour opId={operation.Id}, supportId={alloc.SupportId}, compartmentId={alloc.CompartmentId}.");
                }

                if (requestedShares > heldShares)
                {
                    throw new InvalidOperationException(
                        $"RecalculateFees: rachat irréjouable opId={operation.Id}, supportId={alloc.SupportId}, compartmentId={alloc.CompartmentId}, requestedShares={requestedShares}, heldShares={heldShares}.");
                }
                else
                {
                    alloc.Shares = requestedShares;
                }

                if ((alloc.Shares ?? 0m) > 0m && (alloc.Amount ?? 0m) > 0m)
                {
                    replayableAllocations.Add(alloc);
                }
            }

            operation.Allocations = replayableAllocations;

            if (replayableAllocations.Count == 0)
            {
                throw new InvalidOperationException(
                    $"RecalculateFees: aucune allocation de rachat rejouable pour opId={operation.Id}.");
            }
        }

        private async Task<int> ReplayOperationFeesForExecutedOperationsAsync(Contract contract, List<Operation> executedSourceOps)
        {
            var created = 0;

            foreach (var op in executedSourceOps)
            {
                if (op.Allocations == null || !op.Allocations.Any())
                    continue;

                var feeGroups = op.Allocations
                    .Select(f => new { f.SupportId, f.CompartmentId })
                    .Distinct()
                    .ToList();

                foreach (var g in feeGroups)
                {
                    if (!g.CompartmentId.HasValue)
                        continue;

                    var compartmentId = g.CompartmentId.Value;
                    var supportId = g.SupportId;

                    var support = op.Allocations
                        .FirstOrDefault(a => a.SupportId == supportId)?.Support;

                    if (support == null)
                    {
                        support = await _context.FinancialSupports
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.Id == supportId);
                    }

                    var resolvedFees = _operationFeePolicyResolver.ResolveOperationFees(contract, support, op.Type, op.OperationDate);
                    if (resolvedFees == null || !resolvedFees.Any())
                        continue;

                    foreach (var rf in resolvedFees)
                    {
                        decimal baseAmount;

                        if (rf.ApplyOn == FeeApplyOn.Source)
                        {
                            baseAmount = op.Allocations
                                .Where(x => x.Flow == OperationFlow.Source && x.SupportId == supportId && x.CompartmentId == compartmentId)
                                .Sum(x => x.Amount ?? 0m);
                        }
                        else
                        {
                            baseAmount = op.Allocations
                                .Where(x => x.Flow == OperationFlow.Target && x.SupportId == supportId && x.CompartmentId == compartmentId)
                                .Sum(x => x.Amount ?? 0m);
                        }

                        if (baseAmount <= 0m)
                            continue;

                        var feeAmount = rf.Mode == FeeAmountMode.Percentage
                            ? NumericPolicy.RoundMoney(baseAmount * rf.Rate / 100m)
                            : rf.FixedAmount;

                        if (feeAmount <= 0m)
                            continue;

                        var currentHolding = await _context.ContractSupportHoldings
                            .FirstOrDefaultAsync(h => h.ContractId == contract.Id && h.SupportId == supportId && h.CompartmentId == compartmentId);

                        if (currentHolding == null || currentHolding.TotalShares <= 0m)
                            continue;

                        var postingNav = await GetPostingNavAsync(support ?? new FinancialSupport { Id = supportId }, supportId, op.OperationDate.AddDays(-1));
                        if (postingNav <= 0m)
                            continue;

                        var sharesToRemove = Math.Min(currentHolding.TotalShares, NumericPolicy.RoundShares(feeAmount / postingNav));
                        if (sharesToRemove <= 0m)
                            continue;

                        var feeOperation = new Operation
                        {
                            ContractId = contract.Id,
                            Type = OperationType.OperationFee,
                            SourceOperationId = op.Id,
                            OperationDate = op.OperationDate,
                            ExecutionDate = op.ExecutionDate ?? op.OperationDate,
                            Status = OperationStatus.Executed,
                            Amount = feeAmount,
                            RequestedAmount = feeAmount,
                            ExecutedAmount = feeAmount,
                            Currency = contract.Currency,
                            Allocations = new List<OperationSupportAllocation>
                            {
                                new OperationSupportAllocation
                                {
                                    SupportId = supportId,
                                    Amount = feeAmount,
                                    Shares = sharesToRemove,
                                    NavAtOperation = postingNav,
                                    NavDateAtOperation = op.OperationDate.AddDays(-1),
                                    CompartmentId = compartmentId
                                }
                            },
                            CreatedDate = DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow,
                        };

                        await _context.Operations.AddAsync(feeOperation);
                        await _context.ContractSupportFeeApplications.AddAsync(new ContractSupportFeeApplication
                        {
                            ContractId = contract.Id,
                            FeeOperation = feeOperation,
                            SourceOperationId = op.Id,
                            FeeNature = MapFeeNatureFromOperation(op.Type),
                            CompartmentId = compartmentId,
                            SupportId = supportId,
                            ApplyOn = rf.ApplyOn,
                            BaseAmount = baseAmount,
                            FeeMode = rf.Mode,
                            AppliedRate = rf.Mode == FeeAmountMode.Percentage ? rf.Rate : null,
                            FeeAmount = feeAmount,
                            FeeShares = sharesToRemove,
                            NavUsed = postingNav,
                            NavDateUsed = op.OperationDate.AddDays(-1),
                            PolicySource = rf.Source,
                            PolicyId = null,
                            EffectiveDate = op.OperationDate.Date,
                            CreatedDate = DateTime.UtcNow,
                        });

                        await _operationApplier.ApplyAsync(feeOperation, _context);
                        created++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return created;
        }

        private async Task<int> ReplayManagementFeesForContractAsync(Contract contract, bool forcePost)
        {
            var created = 0;
            var runDate = DateTime.UtcNow.Date;

            var allocations = await _context.FinancialSupportAllocations
                .Include(a => a.Support)
                .Where(a => a.ContractId == contract.Id)
                .ToListAsync();

            var accrualStates = await _context.ContractManagementFeeAccruals
                .Where(a => a.ContractId == contract.Id)
                .ToDictionaryAsync(a => (a.ContractId, a.SupportId, a.CompartmentId));

            foreach (var alloc in allocations)
            {
                if (alloc.Support == null)
                    continue;

                var policy = _managementFeePolicyResolver.Resolve(contract, alloc.Support, runDate);
                if (policy == null)
                    continue;

                var state = GetOrCreateAccrualState(accrualStates, contract.Id, alloc.SupportId, alloc.CompartmentId);
                var accrualStart = GetAccrualStartDate(contract, policy, state);
                var accrualEnd = GetAccrualEndDate(policy, runDate);

                if (accrualStart <= accrualEnd)
                {
                    var computation = await ComputeDailyAccrualAsync(contract, alloc, policy, accrualStart, accrualEnd);
                    if (computation.FeeAmount > 0m)
                    {
                        ApplyAccrualComputation(state, computation);
                    }

                    state.LastAccruedDate = accrualEnd;
                    state.UpdatedDate = DateTime.UtcNow;
                }

                if (policy.PostingMode == ManagementFeePostingMode.NetServedYield)
                    continue;

                if (!forcePost && !ShouldPostForRun(policy, runDate, state.LastPostedDate))
                    continue;

                var feeAmount = NumericPolicy.RoundMoney(state.AccruedAmount);
                if (feeAmount <= 0m)
                    continue;

                var currentHolding = await _context.ContractSupportHoldings
                    .FirstOrDefaultAsync(h =>
                        h.ContractId == contract.Id &&
                        h.SupportId == alloc.SupportId &&
                        h.CompartmentId == alloc.CompartmentId);

                if (currentHolding == null || currentHolding.TotalShares <= 0m)
                    continue;

                var postingNav = await GetPostingNavAsync(alloc.Support, alloc.SupportId, runDate.AddDays(-1));
                if (postingNav <= 0m)
                    continue;

                var sharesToRemove = Math.Min(currentHolding.TotalShares, NumericPolicy.RoundShares(feeAmount / postingNav));
                if (sharesToRemove <= 0m)
                    continue;

                var feeOperation = new Operation
                {
                    ContractId = contract.Id,
                    Type = OperationType.ManagementFee,
                    OperationDate = runDate,
                    ExecutionDate = runDate,
                    Status = OperationStatus.Executed,
                    Amount = feeAmount,
                    RequestedAmount = feeAmount,
                    ExecutedAmount = feeAmount,
                    Currency = contract.Currency,
                    Allocations = new List<OperationSupportAllocation>
                    {
                        new OperationSupportAllocation
                        {
                            SupportId = alloc.SupportId,
                            Amount = feeAmount,
                            Shares = sharesToRemove,
                            NavAtOperation = postingNav,
                            NavDateAtOperation = runDate.AddDays(-1),
                            CompartmentId = alloc.CompartmentId
                        }
                    },
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                };

                await _context.Operations.AddAsync(feeOperation);
                await _context.ContractSupportFeeApplications.AddAsync(new ContractSupportFeeApplication
                {
                    ContractId = contract.Id,
                    FeeOperation = feeOperation,
                    SourceOperationId = null,
                    FeeNature = ContractSupportFeeNature.ManagementFee,
                    CompartmentId = alloc.CompartmentId,
                    SupportId = alloc.SupportId,
                    ApplyOn = FeeApplyOn.Target,
                    BaseAmount = GetAverageAccrualBase(state),
                    FeeMode = FeeAmountMode.Percentage,
                    AppliedRate = policy.AnnualRate,
                    RateBase = policy.RateBase,
                    Frequency = policy.Frequency,
                    ProrataMethod = policy.ProrataMethod,
                    PostingMode = policy.PostingMode,
                    AccrualStartDate = state.AccrualStartDate,
                    AccrualEndDate = state.LastAccruedDate,
                    AccruedDays = state.AccruedDays,
                    FeeAmount = feeAmount,
                    FeeShares = sharesToRemove,
                    NavUsed = postingNav,
                    NavDateUsed = runDate.AddDays(-1),
                    PolicySource = policy.Source,
                    PolicyId = null,
                    EffectiveDate = runDate,
                    CreatedDate = DateTime.UtcNow,
                });

                await _operationApplier.ApplyAsync(feeOperation, _context);

                state.AccruedAmount = 0m;
                state.AccumulatedBaseAmount = 0m;
                state.AccruedDays = 0;
                state.AccrualStartDate = null;
                state.LastPostedDate = runDate;
                state.UpdatedDate = DateTime.UtcNow;

                created++;
            }

            await _context.SaveChangesAsync();
            return created;
        }

        public static class NumericPolicy
        {
            public static decimal RoundShares(decimal value)
                => Math.Round(value, 7, MidpointRounding.AwayFromZero);

            public static decimal RoundMoney(decimal value)
                => Math.Round(value, 2, MidpointRounding.AwayFromZero);

            public static decimal RoundPru(decimal value)
                => Math.Round(value, 7, MidpointRounding.AwayFromZero);

            public static decimal RoundAmount(decimal value)
                => Math.Round(value, 7, MidpointRounding.AwayFromZero);
        }
    }
}
