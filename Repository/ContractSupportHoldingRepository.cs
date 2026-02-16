using System;
using System.Threading.Tasks;
using api.Data;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace api.Repository
{
    public class ContractSupportHoldingRepository : IContractSupportHoldingRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<ContractSupportHoldingRepository> _logger;

        public ContractSupportHoldingRepository(
            ApplicationDBContext context,
            ILogger<ContractSupportHoldingRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================================
        // 🔹 Lecture simple
        // ==========================================================
        public async Task<ContractSupportHolding?> GetAsync(int contractId, int supportId)
        {
            return await _context.ContractSupportHoldings
                .Include(h => h.Support)
                .FirstOrDefaultAsync(h => h.ContractId == contractId && h.SupportId == supportId);
        }

        // ==========================================================
        // 🔹 Upsert SANS recalcul métier (persistence only)
        // ==========================================================
        public async Task UpsertAsync(ContractSupportHolding holding)
        {
            if (holding == null)
                throw new ArgumentNullException(nameof(holding));

            var existing = await GetAsync(holding.ContractId, holding.SupportId);

            if (existing == null)
            {
                // 🆕 Création simple (TotalShares / Pru / TotalInvested proviennent de OperationEngineService)
                holding.LastUpdated = DateTime.UtcNow;
                await _context.ContractSupportHoldings.AddAsync(holding);

                _logger.LogInformation(
                    "🆕 Nouveau holding créé (contrat={ContractId}, support={SupportId})",
                    holding.ContractId, holding.SupportId);
            }
            else
            {
                // 📝 Mise à jour uniquement des champs NON métier
                existing.CurrentAmount = holding.CurrentAmount;
                existing.PerformancePercent = holding.PerformancePercent;
                existing.LastUpdated = DateTime.UtcNow;

                // ❌ NE PAS toucher :
                // existing.TotalShares
                // existing.TotalInvested
                // existing.Pru

                _logger.LogInformation(
                    "♻️ Holding MAJ (contrat={ContractId}, support={SupportId})",
                    holding.ContractId, holding.SupportId);
            }

            await _context.SaveChangesAsync();

            // ==========================================================
            // 🔹 Suppression automatique si holding vide
            // ==========================================================
            if (holding.TotalShares <= 0)
            {
                var toDelete = await GetAsync(holding.ContractId, holding.SupportId);
                if (toDelete != null && toDelete.TotalShares <= 0)
                {
                    _context.ContractSupportHoldings.Remove(toDelete);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "🧹 Holding supprimé car vide (contrat={ContractId}, support={SupportId})",
                        holding.ContractId, holding.SupportId);
                }
            }
        }

        // ==========================================================
        // 🔹 Vérification simple PRU / VL (lecture uniquement)
        // ==========================================================
        public async Task CheckConsistencyAsync(int contractId)
        {
            var holdings = await _context.ContractSupportHoldings
                .Include(h => h.Support)
                .Where(h => h.ContractId == contractId)
                .ToListAsync();

            foreach (var h in holdings)
            {
                var vl = h.Support?.LastValuationAmount ?? 0m;
                var perf = (h.Pru > 0 && vl > 0) ? ((vl - h.Pru) / h.Pru) * 100m : 0m;

                if (h.PerformancePercent == null ||
                    Math.Abs(perf - h.PerformancePercent.Value) > 0.05m)
                {
                    _logger.LogWarning(
                        "⚠️ Incohérence détectée sur holding (contrat {ContractId}, support {SupportId}) : PRU={Pru:F5}, VL={Vl:F5}, Perf calculée={Perf:F2}%, Perf stockée={PerfDb:F2}%",
                        h.ContractId, h.SupportId, h.Pru, vl, perf, h.PerformancePercent);
                }
            }

            _logger.LogInformation("🔎 Vérif cohérence holdings terminée pour contrat {ContractId}", contractId);
        }
    }
}
