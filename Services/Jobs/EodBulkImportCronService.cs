using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using api.Interfaces;
using api.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

public class EodBulkImportCronService : BackgroundService
{
    private readonly ILogger<EodBulkImportCronService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CronExpression _cron;
    private readonly TimeZoneInfo _timeZone;

    public EodBulkImportCronService(
        ILogger<EodBulkImportCronService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _cron = CronExpression.Parse("0 3 * * *"); // ✅ tous les jours à 3h du matin
        _timeZone = TimeZoneInfo.Local;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var next = _cron.GetNextOccurrence(DateTimeOffset.Now, _timeZone);
            if (next.HasValue)
            {
                var delay = next.Value - DateTimeOffset.Now;
                if (delay.TotalMilliseconds > 0)
                {
                    _logger.LogInformation("⏳ Prochain import EOD prévu le {date}", next.Value);
                    await Task.Delay(delay, stoppingToken);
                }
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var bulkService = scope.ServiceProvider.GetRequiredService<EodBulkImportService>();
                await bulkService.RunFullImportAsync();
                _logger.LogInformation("✅ Import EOD global terminé à {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'import EOD global");
            }
        }
    }
}
