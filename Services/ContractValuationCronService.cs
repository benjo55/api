using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using api.Interfaces;
using api.Helpers;

public class ContractValuationCronService : BackgroundService
{
    private readonly ILogger<ContractValuationCronService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CronExpression _cron;
    private readonly TimeZoneInfo _timeZone;

    public ContractValuationCronService(
        ILogger<ContractValuationCronService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        // ⏰ Tous les jours à 2h du matin
        _cron = CronExpression.Parse("0 2 * * *");
        _timeZone = TimeZoneInfo.Local;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Calcul de la prochaine occurrence
            var next = _cron.GetNextOccurrence(DateTimeOffset.Now, _timeZone);
            if (next.HasValue)
            {
                var delay = next.Value - DateTimeOffset.Now;
                if (delay.TotalMilliseconds > 0)
                {
                    _logger.LogInformation("⏳ Prochain traitement CRON prévu le {date}", next.Value);
                    await Task.Delay(delay, stoppingToken);
                }
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();

                var valuationService = scope.ServiceProvider.GetRequiredService<IContractValuationService>();
                var contractRepo = scope.ServiceProvider.GetRequiredService<IContractRepository>();
                var engine = scope.ServiceProvider.GetRequiredService<IOperationEngineService>();

                // 1️⃣ Traitement des opérations en attente
                _logger.LogInformation("⚙️ [CRON] Début ProcessPendingOperationsAsync...");
                await engine.ProcessPendingOperationsAsync();
                _logger.LogInformation("✔️ [CRON] ProcessPendingOperationsAsync terminé.");

                // 2️⃣ Recalcul des contrats (inclut ComputeContractValueAsync)
                _logger.LogInformation("📊 [CRON] Recalcul des contrats...");
                var contracts = await contractRepo.GetAllAsync(new QueryObject
                {
                    PageNumber = 1,
                    PageSize = int.MaxValue
                });

                foreach (var contract in contracts.Items)
                {
                    await contractRepo.RecalculateValueAsync(contract.Id, valuationService, source: "ContractValuationCronService");
                }

                _logger.LogInformation("🏁 [CRON] Recalcul global terminé à {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du traitement CRON");
            }
        }
    }
}
