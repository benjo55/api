// Services/OperationEngineService.cs
using api.Data;
using api.Interfaces;
using api.Models;
using api.Configuration;
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

        public OperationEngineService(ApplicationDBContext context, ILogger<OperationEngineService> logger, IContractSupportHoldingRepository holdingRepo, IContractValuationService valuationService, IOptions<EodSettings> eodSettings, IEodDataProvider eodDataProvider, IDbContextFactory<ApplicationDBContext> dbContextFactory, IOperationApplier operationApplier)
        {
            _context = context;
            _logger = logger;
            _holdingRepo = holdingRepo;
            _valuationService = valuationService;
            _eodDataProvider = eodDataProvider;
            _dbContextFactory = dbContextFactory;
            _eodSettings = eodSettings.Value;
            _operationApplier = operationApplier;
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

                    foreach (var f in finalized)
                    {
                        f.alloc.NavAtOperation = f.nav;
                        f.alloc.NavDateAtOperation = f.date;
                        f.alloc.Shares = f.shares;
                        f.alloc.EstimatedNav = null;
                        f.alloc.EstimatedShares = null;
                    }

                    op.Status = OperationStatus.Executed;
                    op.UpdatedDate = DateTime.UtcNow;

                    await _operationApplier.ApplyAsync(op, _context);

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

        // 4️⃣ Placeholder règles de gestion
        public async Task ApplyRulesAsync()
        {
            _logger.LogInformation("▶️ ApplyRulesAsync (pas encore implémenté)");
            await Task.CompletedTask;
        }

        // 5️⃣ Frais de gestion automatiques
        public async Task ApplyManagementFeesAsync()
        {
            _logger.LogInformation("▶️ Début ApplyManagementFeesAsync");

            var contracts = await _context.Contracts
                .Include(c => c.Supports).ThenInclude(s => s.Support)
                .ToListAsync();

            foreach (var contract in contracts)
            {
                if (contract.ManagementFeesRate == null || contract.ManagementFeesRate == 0) continue;

                try
                {
                    // Supposons que les frais soient annuels, ponctionnés mensuellement
                    var monthlyRate = (contract.ManagementFeesRate.Value / 100m) / 12m;

                    foreach (var alloc in contract.Supports)
                    {
                        if (alloc.Support?.LastValuationAmount is not decimal vl || vl <= 0) continue;

                        var feeAmount = (alloc.AllocationPercentage / 100m) * contract.CurrentValue * monthlyRate;

                        if (feeAmount <= 0) continue;

                        // Enregistrer une "opération de frais"
                        var feeOperation = new Operation
                        {
                            ContractId = contract.Id,
                            Type = OperationType.ManagementFee,
                            Status = OperationStatus.Executed,
                            OperationDate = DateTime.UtcNow,
                            Amount = feeAmount,
                            Currency = contract.Currency,
                            Allocations = new List<OperationSupportAllocation>
                        {
                            new OperationSupportAllocation
                            {
                                SupportId = alloc.SupportId,
                                Amount = feeAmount,
                                Percentage = alloc.AllocationPercentage
                            }
                        },
                            CreatedDate = DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow,
                            // Locked = true
                        };

                        await _context.Operations.AddAsync(feeOperation);

                        _logger.LogInformation($"💸 Frais de gestion appliqués : contrat {contract.Id}, support {alloc.SupportId} → {feeAmount:F2}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Erreur ApplyManagementFeesAsync sur contrat {contract.Id}");
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ ApplyManagementFeesAsync terminé");
        }

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
                    .OrderBy(o => o.OperationDate)
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

        public static class NumericPolicy
        {
            public static decimal RoundShares(decimal value)
                => Math.Round(value, 7, MidpointRounding.AwayFromZero);

            public static decimal RoundMoney(decimal value)
                => Math.Round(value, 2, MidpointRounding.AwayFromZero);

            public static decimal RoundPru(decimal value)
                => Math.Round(value, 7, MidpointRounding.AwayFromZero);
        }
    }
}
