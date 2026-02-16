using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using api.Interfaces;
using api.Helpers;
using api.Data;
using api.Services;

namespace api.Controllers
{
    [ApiController]
    [Route("api/admin")]
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

    }
}
