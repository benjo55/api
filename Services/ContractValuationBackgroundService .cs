using api.Interfaces; // là où est ton ContractValuationService
using api.Helpers;


public class ContractValuationBackgroundService : BackgroundService
{
    private readonly ILogger<ContractValuationBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ContractValuationBackgroundService(
        ILogger<ContractValuationBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // boucle infinie tant que l'appli tourne
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var valuationService = scope.ServiceProvider.GetRequiredService<IContractValuationService>();
                var contractRepo = scope.ServiceProvider.GetRequiredService<IContractRepository>();

                // Récupère tous les contrats
                var contracts = await contractRepo.GetAllAsync(new QueryObject
                {
                    PageNumber = 1,
                    PageSize = int.MaxValue
                });

                foreach (var contract in contracts.Items)
                {
                    var value = await valuationService.ComputeContractValueAsync(contract.Id);
                    await contractRepo.UpdateCurrentValueAsync(contract.Id, value);
                }

                _logger.LogInformation("✅ Recalcul des contrats effectué à {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du recalcul des valeurs de contrat");
            }

            // Attendre 24h (ou autre périodicité)
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
