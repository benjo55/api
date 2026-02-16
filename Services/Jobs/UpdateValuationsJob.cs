using Quartz;
using api.Interfaces;
using api.Helpers;
using Microsoft.Extensions.DependencyInjection;

public class UpdateValuationsJob : IJob
{
    private readonly IOperationEngineService _engine;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UpdateValuationsJob> _logger;
    private readonly IFinancialSupportImportService _importService;

    public UpdateValuationsJob(
        IOperationEngineService engine,
        IServiceProvider serviceProvider,
        IFinancialSupportImportService importService,
        ILogger<UpdateValuationsJob> logger)
    {
        _engine = engine;
        _serviceProvider = serviceProvider;
        _importService = importService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("⏰ Quartz → Lancement UpdateValuationsJob (Import EOD + Recalcul)");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var supportRepo = scope.ServiceProvider.GetRequiredService<IFinancialSupportRepository>();

            // 🔥 Correction : pagination étendue
            var supportsPage = await supportRepo.GetAllAsync(new QueryObject
            {
                PageNumber = 1,
                PageSize = 5000
            });

            var supports = supportsPage.Items
                .Where(s =>
                    !string.IsNullOrWhiteSpace(s.Label) &&
                    s.Label.Contains("strategie", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
            _logger.LogInformation($"📊 {supports.Count} supports chargés pour mise à jour EOD.");

            int imported = 0, failed = 0;

            foreach (var support in supports)
            {
                if (string.IsNullOrWhiteSpace(support.ISIN))
                {
                    _logger.LogWarning($"⚠️ Support {support.Id} sans ISIN — ignoré.");
                    continue;
                }

                try
                {
                    _logger.LogInformation($"➡️ Import EOD : {support.Label} ({support.ISIN})...");
                    await _importService.ImportFromEodByIsinAsync(support.Id, support.ISIN);
                    imported++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, $"❌ Échec import EOD {support.Label} ({support.ISIN})");
                }
            }

            _logger.LogInformation($"✨ Import terminé : {imported} ok | {failed} erreurs.");

            // Recalcul moteur
            _logger.LogInformation("🔄 Recalcul interne des valorisations...");
            await _engine.UpdateValuationsAsync();

            _logger.LogInformation("🏁 UpdateValuationsJob terminé.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur dans UpdateValuationsJob");
        }
    }
}
