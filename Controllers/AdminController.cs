using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using api.Interfaces;
using api.Helpers;
using api.Data;
using api.Services;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IFinancialSupportImportService _importService;
        private readonly IFinancialSupportRepository _supportRepo;
        private readonly IOperationEngineService _engine;
        private readonly ILogger<AdminController> _logger;
        private readonly IServiceProvider _services;

        public AdminController(
            IFinancialSupportImportService importService,
            IFinancialSupportRepository supportRepo,
            IOperationEngineService engine,
            ILogger<AdminController> logger,
            IServiceProvider services)
        {
            _importService = importService;
            _supportRepo = supportRepo;
            _engine = engine;
            _logger = logger;
            _services = services;
        }

        /// <summary>
        /// ⚙️ Lance immédiatement l'import des VL depuis EOD pour tous les supports.
        /// </summary>
        [HttpPost("run-eod-now")]
        public async Task<IActionResult> RunEodNow([FromServices] EodBulkImportService bulk)
        {
            await bulk.RunFullImportAsync();
            return Ok("Import EOD manuel terminé.");
        }

        [HttpPost("process-pending")]
        public async Task<IActionResult> ProcessPendingOperations()
        {
            _logger.LogInformation("▶️ Lancement manuel du moteur de dénouement…");

            await _engine.ProcessPendingOperationsAsync();

            _logger.LogInformation("✔️ Dénouement manuel exécuté avec succès.");

            return Ok(new
            {
                success = true,
                message = "Dénouement manuel exécuté avec succès 🔁"
            });
        }

        [HttpPost("engine/run")]
        public async Task<IActionResult> RunEngineNow()
        {
            using var scope = _services.CreateScope();
            var engine = scope.ServiceProvider.GetRequiredService<IOperationEngineService>();

            await engine.UpdateValuationsAsync();          // Récupère les VL
            await engine.ProcessPendingOperationsAsync();  // Dénoue les opérations pending
            await engine.ApplyManagementFeesAsync();       // Facultatif (frais mensuels)

            return Ok("🚀 Moteur exécuté manuellement — VL mises à jour, opérations dénouées.");
        }

        /// <summary>
        /// Lance manuellement la mise à jour des valeurs liquidatives via EOD.
        /// </summary>
        [HttpPost("update-valuations")]
        public async Task<IActionResult> UpdateValuations()
        {
            using var scope = _services.CreateScope();

            var job = scope.ServiceProvider.GetRequiredService<UpdateValuationsJob>();

            // Exécute exactement le même code que Quartz, mais manuellement
            await job.Execute(null!);

            return Ok("UpdateValuationsJob exécuté manuellement avec succès.");
        }

        /// <summary>
        /// 🔄 Forcer un recalcul complet d’un contrat immédiatement.
        /// </summary>
        [HttpPost("recompute-contract/{contractId:int}")]
        public async Task<IActionResult> RecomputeContract([FromRoute] int contractId)
        {
            using var scope = _services.CreateScope();

            var contractRepo = scope.ServiceProvider.GetRequiredService<IContractRepository>();
            var valuationService = scope.ServiceProvider.GetRequiredService<IContractValuationService>();

            _logger.LogInformation("▶️ Recompute manuel du contrat {contractId}", contractId);

            try
            {
                await valuationService.ComputeContractValueAsync(contractId);

                return Ok(new
                {
                    success = true,
                    message = $"Recompute du contrat {contractId} exécuté avec succès."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du recompute du contrat {contractId}", contractId);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors du recompute du contrat.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 🔄 Forcer un recalcul complet de TOUS les contrats.
        /// </summary>
        [HttpPost("recompute-all-contracts")]
        public async Task<IActionResult> RecomputeAllContracts()
        {
            using var scope = _services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<api.Data.ApplicationDBContext>();
            var valuationService = scope.ServiceProvider.GetRequiredService<IContractValuationService>();

            var contractIds = await context.Contracts
                .Select(c => c.Id)
                .ToListAsync();

            int success = 0, failed = 0;

            foreach (var id in contractIds)
            {
                try
                {
                    await valuationService.ComputeContractValueAsync(id);
                    success++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Recompute contrat {id} échoué", id);
                    failed++;
                }
            }

            return Ok(new
            {
                total = contractIds.Count,
                success,
                failed
            });
        }

        /// <summary>
        /// 🧹 Purge les positions résiduelles (< seuil de parts) dans ContractSupportHolding et FinancialSupportAllocation.
        /// </summary>
        [HttpPost("cleanup-residual-holdings")]
        public async Task<IActionResult> CleanupResidualHoldings([FromQuery] decimal threshold = 0.01m)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var valuationService = scope.ServiceProvider.GetRequiredService<IContractValuationService>();

            // 1) Holdings résiduels : TotalShares > 0 mais < threshold
            var residualHoldings = await context.ContractSupportHoldings
                .Where(h => h.TotalShares > 0m && h.TotalShares < threshold)
                .ToListAsync();

            var affectedContractIds = residualHoldings.Select(h => h.ContractId).Distinct().ToList();

            // 2) Zeroing des FSA correspondantes
            foreach (var h in residualHoldings)
            {
                var fsa = await context.FinancialSupportAllocations
                    .FirstOrDefaultAsync(f =>
                        f.ContractId == h.ContractId &&
                        f.SupportId == h.SupportId &&
                        f.CompartmentId == h.CompartmentId);

                if (fsa != null)
                {
                    fsa.CurrentShares = 0m;
                    fsa.InvestedAmount = 0m;
                }

                h.TotalShares = 0m;
                h.TotalInvested = 0m;
                h.Pru = 0m;
                h.CurrentAmount = 0m;
            }

            await context.SaveChangesAsync();

            _logger.LogInformation("🧹 Cleanup résidus: {Count} holdings purgés sur {ContractCount} contrats",
                residualHoldings.Count, affectedContractIds.Count);

            // 3) Recompute des contrats affectés pour mettre à jour la valorisation
            int recomputeSuccess = 0, recomputeFailed = 0;
            foreach (var contractId in affectedContractIds)
            {
                try
                {
                    await valuationService.ComputeContractValueAsync(contractId);
                    recomputeSuccess++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Recompute après cleanup échoué pour contrat {ContractId}", contractId);
                    recomputeFailed++;
                }
            }

            return Ok(new
            {
                threshold,
                residualHoldingsPurged = residualHoldings.Count,
                affectedContracts = affectedContractIds.Count,
                recomputeSuccess,
                recomputeFailed
            });
        }

    }
}
