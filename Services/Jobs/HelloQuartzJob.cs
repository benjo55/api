using Quartz;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class HelloQuartzJob : IJob
{
    private readonly ILogger<HelloQuartzJob> _logger;

    public HelloQuartzJob(ILogger<HelloQuartzJob> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("⏰ Hello Quartz! Exécuté à {time}", DateTime.Now);
        return Task.CompletedTask;
    }
}

