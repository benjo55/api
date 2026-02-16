using api.Interfaces;
using Quartz;

public class ApplyManagementFeesJob : IJob
{
    private readonly IOperationEngineService _engine;
    public ApplyManagementFeesJob(IOperationEngineService engine) => _engine = engine;

    public async Task Execute(IJobExecutionContext context) =>
        await _engine.ApplyManagementFeesAsync();
}