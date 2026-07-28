using api.Data;
using api.Dtos.Workflow;
using api.Models.Workflow;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Workflow;

public interface IWorkflowRuntimeService
{
    Task<ProcessInstanceDto?> StartAsync(
        int processVersionId,
        ProcessInstanceCreateDto dto,
        string? user,
        CancellationToken cancellationToken = default);

    Task<ProcessInstanceDto?> CompleteTaskAsync(
        int taskInstanceId,
        CompleteTaskInstanceDto dto,
        string? user,
        CancellationToken cancellationToken = default);
}

public sealed class WorkflowRuntimeService : IWorkflowRuntimeService
{
    private readonly ApplicationDBContext _db;

    public WorkflowRuntimeService(ApplicationDBContext db) => _db = db;

    public async Task<ProcessInstanceDto?> StartAsync(
        int processVersionId,
        ProcessInstanceCreateDto dto,
        string? user,
        CancellationToken cancellationToken = default)
    {
        var version = await LoadVersionAsync(processVersionId, cancellationToken);
        if (version is null || version.Status != ProcessVersionStatus.Published)
        {
            return null;
        }

        var start = version.Tasks.SingleOrDefault(x => x.TaskKind == WorkflowTaskKind.Start && !x.IsDeleted);
        if (start is null)
        {
            return null;
        }

        var instance = new ProcessInstance
        {
            ProcessVersionId = processVersionId,
            BusinessKey = dto.BusinessKey,
            Title = dto.Title,
            ContextJson = dto.ContextJson,
            StartedBy = user,
            Status = ProcessInstanceStatus.Running,
        };
        _db.ProcessInstances.Add(instance);
        await _db.SaveChangesAsync(cancellationToken);

        await AddEventAsync(instance.Id, null, "Started", "Instance créée.", user, cancellationToken);
        await AdvanceFromTaskAsync(instance, start, user, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetInstanceDtoAsync(instance.Id, cancellationToken);
    }

    public async Task<ProcessInstanceDto?> CompleteTaskAsync(
        int taskInstanceId,
        CompleteTaskInstanceDto dto,
        string? user,
        CancellationToken cancellationToken = default)
    {
        var taskInstance = await _db.WorkflowTaskInstances
            .Include(x => x.WorkflowTask)
            .Include(x => x.ProcessInstance)
            .FirstOrDefaultAsync(x => x.Id == taskInstanceId, cancellationToken);
        if (taskInstance is null || taskInstance.Status is WorkflowTaskInstanceStatus.Completed or WorkflowTaskInstanceStatus.Cancelled)
        {
            return null;
        }

        taskInstance.Status = WorkflowTaskInstanceStatus.Completed;
        taskInstance.ResultJson = dto.ResultJson;
        taskInstance.CompletedAt = DateTime.UtcNow;
        await AddEventAsync(taskInstance.ProcessInstanceId, taskInstance.Id, "TaskCompleted", $"Tâche {taskInstance.WorkflowTask.Code} complétée.", user, cancellationToken);

        await AdvanceFromTaskAsync(taskInstance.ProcessInstance, taskInstance.WorkflowTask, user, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetInstanceDtoAsync(taskInstance.ProcessInstanceId, cancellationToken);
    }

    private async Task AdvanceFromTaskAsync(
        ProcessInstance instance,
        WorkflowTask completedTask,
        string? user,
        CancellationToken cancellationToken)
    {
        var version = await LoadVersionAsync(instance.ProcessVersionId, cancellationToken)
            ?? throw new InvalidOperationException("Version introuvable.");

        var nextTransition = version.Transitions
            .Where(x => !x.IsDeleted && x.SourceTaskId == completedTask.Id)
            .OrderBy(x => x.OrderIndex)
            .FirstOrDefault(x => x.TransitionKind == WorkflowTransitionKind.Sequence ||
                                 string.IsNullOrWhiteSpace(x.ConditionExpression));

        if (nextTransition is null)
        {
            instance.Status = ProcessInstanceStatus.Completed;
            instance.CompletedAt = DateTime.UtcNow;
            instance.CurrentTaskId = null;
            await AddEventAsync(instance.Id, null, "Completed", "Instance clôturée faute de transition sortante.", user, cancellationToken);
            return;
        }

        var nextTask = version.Tasks.Single(x => x.Id == nextTransition.TargetTaskId);
        if (nextTask.TaskKind == WorkflowTaskKind.End)
        {
            instance.Status = ProcessInstanceStatus.Completed;
            instance.CompletedAt = DateTime.UtcNow;
            instance.CurrentTaskId = nextTask.Id;
            _db.WorkflowTaskInstances.Add(new WorkflowTaskInstance
            {
                ProcessInstanceId = instance.Id,
                WorkflowTaskId = nextTask.Id,
                Status = WorkflowTaskInstanceStatus.Completed,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
            });
            await AddEventAsync(instance.Id, null, "Completed", $"Tâche Fin atteinte : {nextTask.Name}.", user, cancellationToken);
            return;
        }

        var newStatus = nextTask.TaskKind == WorkflowTaskKind.Human
            ? WorkflowTaskInstanceStatus.Ready
            : WorkflowTaskInstanceStatus.Completed;

        var newTaskInstance = new WorkflowTaskInstance
        {
            ProcessInstanceId = instance.Id,
            WorkflowTaskId = nextTask.Id,
            Status = newStatus,
            StartedAt = DateTime.UtcNow,
        };
        if (newStatus == WorkflowTaskInstanceStatus.Completed)
        {
            newTaskInstance.CompletedAt = DateTime.UtcNow;
        }

        _db.WorkflowTaskInstances.Add(newTaskInstance);
        instance.CurrentTaskId = nextTask.Id;
        await AddEventAsync(instance.Id, null, "TaskReady", $"Tâche courante : {nextTask.Name}.", user, cancellationToken);

        if (newStatus == WorkflowTaskInstanceStatus.Completed)
        {
            await AdvanceFromTaskAsync(instance, nextTask, user, cancellationToken);
        }
    }

    private Task<ProcessVersion?> LoadVersionAsync(int processVersionId, CancellationToken cancellationToken) =>
        _db.ProcessVersions
            .Include(x => x.Tasks.Where(t => !t.IsDeleted))
            .Include(x => x.Transitions.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == processVersionId && !x.IsDeleted, cancellationToken);

    private async Task AddEventAsync(
        int processInstanceId,
        int? taskInstanceId,
        string type,
        string? message,
        string? user,
        CancellationToken cancellationToken)
    {
        _db.WorkflowEventLogs.Add(new WorkflowEventLog
        {
            ProcessInstanceId = processInstanceId,
            WorkflowTaskInstanceId = taskInstanceId,
            EventType = type,
            Message = message,
            CreatedBy = user,
        });
        await Task.CompletedTask;
    }

    private async Task<ProcessInstanceDto?> GetInstanceDtoAsync(int instanceId, CancellationToken cancellationToken)
    {
        return await _db.ProcessInstances
            .AsNoTracking()
            .Where(x => x.Id == instanceId)
            .Select(x => new ProcessInstanceDto
            {
                Id = x.Id,
                ProcessVersionId = x.ProcessVersionId,
                BusinessKey = x.BusinessKey,
                Title = x.Title,
                Status = x.Status,
                StartedAt = x.StartedAt,
                StartedBy = x.StartedBy,
                CompletedAt = x.CompletedAt,
                CurrentTaskId = x.CurrentTaskId,
                ContextJson = x.ContextJson,
                TaskInstances = x.TaskInstances
                    .OrderBy(t => t.Id)
                    .Select(t => new WorkflowTaskInstanceDto
                    {
                        Id = t.Id,
                        ProcessInstanceId = t.ProcessInstanceId,
                        WorkflowTaskId = t.WorkflowTaskId,
                        WorkflowTaskName = t.WorkflowTask.Name,
                        Status = t.Status,
                        AssignedTo = t.AssignedTo,
                        StartedAt = t.StartedAt,
                        CompletedAt = t.CompletedAt,
                        ResultJson = t.ResultJson,
                    })
                    .ToList(),
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
