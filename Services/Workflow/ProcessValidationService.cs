using api.Data;
using api.Dtos.Workflow;
using api.Models.Workflow;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Workflow;

public interface IProcessValidationService
{
    Task<ProcessValidationResultDto> ValidateAsync(int processVersionId, CancellationToken cancellationToken = default);
}

public sealed class ProcessValidationService : IProcessValidationService
{
    private readonly ApplicationDBContext _db;

    public ProcessValidationService(ApplicationDBContext db) => _db = db;

    public async Task<ProcessValidationResultDto> ValidateAsync(
        int processVersionId,
        CancellationToken cancellationToken = default)
    {
        var version = await _db.ProcessVersions
            .AsNoTracking()
            .Include(x => x.Lanes.Where(l => !l.IsDeleted))
            .Include(x => x.Tasks.Where(t => !t.IsDeleted))
            .Include(x => x.Transitions.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == processVersionId && !x.IsDeleted, cancellationToken);

        var result = new ProcessValidationResultDto();
        if (version is null)
        {
            Add(result, "VERSION_NOT_FOUND", "La version de processus est introuvable.");
            return result;
        }

        var lanes = version.Lanes.ToList();
        var tasks = version.Tasks.ToList();
        var transitions = version.Transitions.ToList();
        var taskById = tasks.ToDictionary(x => x.Id);
        var laneById = lanes.ToDictionary(x => x.Id);

        foreach (var duplicate in tasks.GroupBy(x => x.Code.Trim().ToUpperInvariant()).Where(x => x.Count() > 1))
        {
            Add(result, "DUPLICATE_TASK_CODE", $"Code de tâche en doublon dans cette version : {duplicate.Key}.", "Task");
        }

        foreach (var duplicate in lanes.GroupBy(x => x.Code.Trim().ToUpperInvariant()).Where(x => x.Count() > 1))
        {
            Add(result, "DUPLICATE_LANE_CODE", $"Code de couloir en doublon dans cette version : {duplicate.Key}.", "Lane");
        }

        var starts = tasks.Where(x => x.TaskKind == WorkflowTaskKind.Start).ToList();
        if (starts.Count != 1)
        {
            Add(result, "START_COUNT", "Le processus doit posséder exactement une tâche Début.");
        }

        var ends = tasks.Where(x => x.TaskKind == WorkflowTaskKind.End).ToList();
        if (ends.Count == 0)
        {
            Add(result, "END_REQUIRED", "Le processus doit posséder au moins une tâche Fin.");
        }

        foreach (var task in tasks)
        {
            if (!laneById.ContainsKey(task.WorkflowLaneId))
            {
                Add(result, "TASK_WITHOUT_LANE", $"La tâche {task.Code} n'est rattachée à aucun couloir valide.", "Task", task.Id);
            }
        }

        foreach (var transition in transitions)
        {
            if (transition.SourceTaskId == transition.TargetTaskId)
            {
                Add(result, "SELF_TRANSITION", "Une transition ne peut pas pointer vers sa propre source.", "Transition", transition.Id);
            }

            if (!taskById.TryGetValue(transition.SourceTaskId, out var source))
            {
                Add(result, "TRANSITION_WITHOUT_SOURCE", "Une transition n'a pas de tâche source valide.", "Transition", transition.Id);
                continue;
            }

            if (!taskById.TryGetValue(transition.TargetTaskId, out _))
            {
                Add(result, "TRANSITION_WITHOUT_TARGET", "Une transition n'a pas de tâche cible valide.", "Transition", transition.Id);
            }

            if (source.TaskKind == WorkflowTaskKind.Gateway &&
                transition.TransitionKind == WorkflowTransitionKind.Conditional &&
                string.IsNullOrWhiteSpace(transition.ConditionExpression) &&
                string.IsNullOrWhiteSpace(transition.ConditionLabel))
            {
                Add(result, "GATEWAY_CONDITION_REQUIRED", "Les transitions conditionnelles sortant d'une décision doivent porter un libellé ou une condition.", "Transition", transition.Id);
            }
        }

        foreach (var start in starts)
        {
            if (transitions.Any(x => x.TargetTaskId == start.Id))
            {
                Add(result, "START_HAS_INCOMING", "La tâche Début ne doit pas avoir de transition entrante.", "Task", start.Id);
            }
        }

        foreach (var end in ends)
        {
            if (transitions.Any(x => x.SourceTaskId == end.Id))
            {
                Add(result, "END_HAS_OUTGOING", "Une tâche Fin ne doit pas avoir de transition sortante.", "Task", end.Id);
            }
        }

        foreach (var duplicate in transitions
            .GroupBy(x => new { x.SourceTaskId, x.TargetTaskId, x.TransitionKind })
            .Where(x => x.Count() > 1))
        {
            Add(result, "DUPLICATE_TRANSITION", "Transition source/cible/type en doublon.", "Transition", duplicate.First().Id);
        }

        if (starts.Count == 1)
        {
            var reachableFromStart = Traverse(starts[0].Id, transitions, forward: true);
            foreach (var task in tasks.Where(x => x.TaskKind != WorkflowTaskKind.Start && !reachableFromStart.Contains(x.Id)))
            {
                Add(result, "TASK_NOT_REACHABLE", $"La tâche {task.Code} n'est pas atteignable depuis le Début.", "Task", task.Id);
            }
        }

        if (ends.Count > 0)
        {
            var canReachEnd = new HashSet<int>();
            foreach (var end in ends)
            {
                canReachEnd.UnionWith(Traverse(end.Id, transitions, forward: false));
            }

            foreach (var task in tasks.Where(x => x.TaskKind != WorkflowTaskKind.End && !canReachEnd.Contains(x.Id)))
            {
                Add(result, "TASK_CANNOT_REACH_END", $"La tâche {task.Code} ne peut rejoindre aucune tâche Fin.", "Task", task.Id);
            }
        }

        return result;
    }

    private static HashSet<int> Traverse(
        int startTaskId,
        IReadOnlyCollection<WorkflowTransition> transitions,
        bool forward)
    {
        var visited = new HashSet<int> { startTaskId };
        var queue = new Queue<int>();
        queue.Enqueue(startTaskId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var next = forward
                ? transitions.Where(x => x.SourceTaskId == current).Select(x => x.TargetTaskId)
                : transitions.Where(x => x.TargetTaskId == current).Select(x => x.SourceTaskId);

            foreach (var taskId in next)
            {
                if (visited.Add(taskId))
                {
                    queue.Enqueue(taskId);
                }
            }
        }

        return visited;
    }

    private static void Add(
        ProcessValidationResultDto result,
        string code,
        string message,
        string? objectType = null,
        int? objectId = null,
        string severity = "Error")
    {
        result.Issues.Add(new ProcessValidationIssueDto
        {
            Severity = severity,
            Code = code,
            Message = message,
            ObjectType = objectType,
            ObjectId = objectId,
        });
    }
}
