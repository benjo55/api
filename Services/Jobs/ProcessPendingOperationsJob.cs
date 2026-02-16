using api.Interfaces;
using Quartz;

public class ProcessPendingOperationsJob : IJob
{
    private readonly IOperationEngineService _engine;
    public ProcessPendingOperationsJob(IOperationEngineService engine) => _engine = engine;

    public async Task Execute(IJobExecutionContext context) =>
        await _engine.ProcessPendingOperationsAsync();
}