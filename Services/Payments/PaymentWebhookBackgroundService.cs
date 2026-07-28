using api.Interfaces;

namespace api.Services.Payments
{
    public sealed class PaymentWebhookBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentWebhookBackgroundService> _logger;

        public PaymentWebhookBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<PaymentWebhookBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IPublicDonationService>();
                    var processed = await service.ProcessPendingWebhooksAsync(stoppingToken);
                    if (processed > 0)
                    {
                        _logger.LogInformation("Webhooks HelloAsso traites: {Count}", processed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur traitement asynchrone des webhooks HelloAsso");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
