using api.Interfaces;
using api.Models;
using Microsoft.Extensions.Logging;
using api.Data;
using api.Helpers;

namespace api.Services
{
    public class EodBulkImportService
    {
        private readonly IFinancialSupportRepository _supportRepo;
        private readonly IFinancialSupportImportService _importService;
        private readonly ILogger<EodBulkImportService> _logger;

        public EodBulkImportService(
            IFinancialSupportRepository supportRepo,
            IFinancialSupportImportService importService,
            ILogger<EodBulkImportService> logger)
        {
            _supportRepo = supportRepo;
            _importService = importService;
            _logger = logger;
        }

        public async Task RunFullImportAsync()
        {
            _logger.LogInformation("🚀 Début du batch EOD pour tous les supports...");

            var supports = await _supportRepo.GetAllAsync(new QueryObject
            {
                PageNumber = 1,
                PageSize = int.MaxValue
            });

            int success = 0, errors = 0;

            foreach (var s in supports.Items)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(s.ISIN))
                        continue;

                    _logger.LogInformation($"[EOD] Import {s.Code} ({s.ISIN})...");
                    await _importService.ImportFromEodByIsinAsync(s.Id, s.ISIN);
                    success++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning($"[EOD] ⚠️ {s.ISIN} - {ex.Message}");
                }
            }

            _logger.LogInformation($"✅ Import terminé : {success} réussis, {errors} erreurs.");
        }
    }
}
