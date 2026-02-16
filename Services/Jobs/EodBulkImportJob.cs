using Quartz;
using Microsoft.Extensions.Logging;
using api.Services;

public class EodBulkImportJob : IJob
{
    private readonly EodBulkImportService _bulkService;
    private readonly ILogger<EodBulkImportJob> _logger;

    public EodBulkImportJob(EodBulkImportService bulkService, ILogger<EodBulkImportJob> logger)
    {
        _bulkService = bulkService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("⏰ Lancement du batch EOD global...");
        await _bulkService.RunFullImportAsync();
    }
}
