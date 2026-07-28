using api.Data;
using api.Dtos.Workflow;
using api.Models.Workflow;
using api.Services.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
public sealed class ProcessesController : ControllerBase
{
    private readonly ApplicationDBContext _db;
    private readonly IProcessValidationService _validationService;
    private readonly IWorkflowRuntimeService _runtimeService;

    public ProcessesController(
        ApplicationDBContext db,
        IProcessValidationService validationService,
        IWorkflowRuntimeService runtimeService)
    {
        _db = db;
        _validationService = validationService;
        _runtimeService = runtimeService;
    }

    [HttpGet("api/processes")]
    public async Task<ActionResult<List<ProcessDefinitionDto>>> GetProcesses(
        [FromQuery] string? search,
        [FromQuery] ProcessDefinitionStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _db.ProcessDefinitions
            .AsNoTracking()
            .Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term) || (x.Domain != null && x.Domain.Contains(term)));
        }
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status);
        }

        var processes = await query
            .Include(x => x.Versions.Where(v => !v.IsDeleted))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return processes.Select(ToProcessDto).ToList();
    }

    [HttpGet("api/processes/{id:int}")]
    public async Task<ActionResult<ProcessDefinitionDto>> GetProcess(int id, CancellationToken cancellationToken)
    {
        var process = await _db.ProcessDefinitions
            .AsNoTracking()
            .Include(x => x.Versions.Where(v => !v.IsDeleted))
            .Where(x => x.Id == id && !x.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);
        return process is null ? NotFound() : Ok(ToProcessDto(process));
    }

    [HttpPost("api/processes")]
    public async Task<ActionResult<ProcessDefinitionDto>> CreateProcess(
        ProcessDefinitionWriteDto dto,
        CancellationToken cancellationToken)
    {
        var code = NormalizeCode(dto.Code);
        if (await _db.ProcessDefinitions.AnyAsync(x => x.Code == code && !x.IsDeleted, cancellationToken))
        {
            return Conflict("Ce code de processus existe déjà.");
        }

        var process = new ProcessDefinition();
        Apply(dto, process);
        StampCreated(process);
        _db.ProcessDefinitions.Add(process);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetProcess), new { id = process.Id }, await GetProcessDtoAsync(process.Id, cancellationToken));
    }

    [HttpPut("api/processes/{id:int}")]
    public async Task<ActionResult<ProcessDefinitionDto>> UpdateProcess(
        int id,
        ProcessDefinitionWriteDto dto,
        CancellationToken cancellationToken)
    {
        var process = await _db.ProcessDefinitions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (process is null) return NotFound();
        var code = NormalizeCode(dto.Code);
        if (await _db.ProcessDefinitions.AnyAsync(x => x.Id != id && x.Code == code && !x.IsDeleted, cancellationToken))
        {
            return Conflict("Ce code de processus existe déjà.");
        }

        Apply(dto, process);
        StampUpdated(process);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(await GetProcessDtoAsync(id, cancellationToken));
    }

    [HttpDelete("api/processes/{id:int}")]
    public async Task<IActionResult> DeleteProcess(int id, CancellationToken cancellationToken)
    {
        var process = await _db.ProcessDefinitions
            .Include(x => x.Versions)
            .ThenInclude(x => x.Lanes)
            .Include(x => x.Versions)
            .ThenInclude(x => x.Tasks)
            .Include(x => x.Versions)
            .ThenInclude(x => x.Transitions)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (process is null) return NotFound();

        process.IsDeleted = true;
        StampUpdated(process);
        foreach (var version in process.Versions)
        {
            version.IsDeleted = true;
            StampUpdated(version);
            foreach (var lane in version.Lanes) lane.IsDeleted = true;
            foreach (var task in version.Tasks) task.IsDeleted = true;
            foreach (var transition in version.Transitions) transition.IsDeleted = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("api/processes/{processId:int}/versions")]
    public async Task<ActionResult<List<ProcessVersionDto>>> GetVersions(int processId, CancellationToken cancellationToken)
    {
        return await _db.ProcessVersions
            .AsNoTracking()
            .Where(x => x.ProcessDefinitionId == processId && !x.IsDeleted)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => ToVersionDto(x))
            .ToListAsync(cancellationToken);
    }

    [HttpPost("api/processes/{processId:int}/versions")]
    public async Task<ActionResult<ProcessVersionDto>> CreateVersion(
        int processId,
        ProcessVersionCreateDto dto,
        CancellationToken cancellationToken)
    {
        if (!await _db.ProcessDefinitions.AnyAsync(x => x.Id == processId && !x.IsDeleted, cancellationToken))
        {
            return NotFound("Processus introuvable.");
        }

        var nextNumber = await _db.ProcessVersions
            .Where(x => x.ProcessDefinitionId == processId)
            .Select(x => (int?)x.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var version = new ProcessVersion
        {
            ProcessDefinitionId = processId,
            VersionNumber = nextNumber + 1,
            Label = dto.Label,
            CanvasWidth = dto.CanvasWidth,
            CanvasHeight = dto.CanvasHeight,
            LaneOrientation = string.IsNullOrWhiteSpace(dto.LaneOrientation) ? "Vertical" : dto.LaneOrientation.Trim(),
            Status = ProcessVersionStatus.Draft,
        };
        StampCreated(version);
        _db.ProcessVersions.Add(version);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToVersionDto(version));
    }

    [HttpPost("api/process-versions/{versionId:int}/duplicate")]
    public async Task<ActionResult<ProcessVersionDto>> DuplicateVersion(int versionId, CancellationToken cancellationToken)
    {
        var source = await LoadVersionGraphAsync(versionId, tracking: false, cancellationToken);
        if (source is null) return NotFound();

        var nextNumber = await _db.ProcessVersions
            .Where(x => x.ProcessDefinitionId == source.ProcessDefinitionId)
            .Select(x => (int?)x.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var copy = new ProcessVersion
        {
            ProcessDefinitionId = source.ProcessDefinitionId,
            VersionNumber = nextNumber + 1,
            Label = $"{source.Label ?? $"Version {source.VersionNumber}"} - copie",
            CanvasWidth = source.CanvasWidth,
            CanvasHeight = source.CanvasHeight,
            ViewportJson = source.ViewportJson,
            LaneOrientation = source.LaneOrientation,
        };
        StampCreated(copy);
        _db.ProcessVersions.Add(copy);
        await _db.SaveChangesAsync(cancellationToken);

        var laneMap = new Dictionary<int, WorkflowLane>();
        foreach (var lane in source.Lanes.Where(x => !x.IsDeleted).OrderBy(x => x.OrderIndex))
        {
            var laneCopy = new WorkflowLane
            {
                ProcessVersionId = copy.Id,
                Code = lane.Code,
                Name = lane.Name,
                Description = lane.Description,
                ActorType = lane.ActorType,
                ActorRefId = lane.ActorRefId,
                OrderIndex = lane.OrderIndex,
                X = lane.X,
                Y = lane.Y,
                Width = lane.Width,
                Height = lane.Height,
                StyleJson = lane.StyleJson,
            };
            StampCreated(laneCopy);
            laneMap[lane.Id] = laneCopy;
            _db.WorkflowLanes.Add(laneCopy);
        }
        await _db.SaveChangesAsync(cancellationToken);

        var taskMap = new Dictionary<int, WorkflowTask>();
        foreach (var task in source.Tasks.Where(x => !x.IsDeleted))
        {
            var taskCopy = new WorkflowTask
            {
                ProcessVersionId = copy.Id,
                WorkflowLaneId = laneMap[task.WorkflowLaneId].Id,
                Code = task.Code,
                Name = task.Name,
                Description = task.Description,
                TaskKind = task.TaskKind,
                ExecutionMode = task.ExecutionMode,
                AssignmentType = task.AssignmentType,
                AssignmentExpression = task.AssignmentExpression,
                ApplicationRefId = task.ApplicationRefId,
                ExpectedDurationMinutes = task.ExpectedDurationMinutes,
                SlaMinutes = task.SlaMinutes,
                IsBlocking = task.IsBlocking,
                X = task.X,
                Y = task.Y,
                Width = task.Width,
                Height = task.Height,
                StyleJson = task.StyleJson,
                ConfigurationJson = task.ConfigurationJson,
            };
            StampCreated(taskCopy);
            taskMap[task.Id] = taskCopy;
            _db.WorkflowTasks.Add(taskCopy);
        }
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var transition in source.Transitions.Where(x => !x.IsDeleted))
        {
            var transitionCopy = new WorkflowTransition
            {
                ProcessVersionId = copy.Id,
                SourceTaskId = taskMap[transition.SourceTaskId].Id,
                TargetTaskId = taskMap[transition.TargetTaskId].Id,
                Code = transition.Code,
                Name = transition.Name,
                Description = transition.Description,
                TransitionKind = transition.TransitionKind,
                ConditionExpression = transition.ConditionExpression,
                ConditionLabel = transition.ConditionLabel,
                OrderIndex = transition.OrderIndex,
                PointsJson = transition.PointsJson,
                StyleJson = transition.StyleJson,
            };
            StampCreated(transitionCopy);
            _db.WorkflowTransitions.Add(transitionCopy);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToVersionDto(copy));
    }

    [HttpPost("api/process-versions/{versionId:int}/publish")]
    public async Task<ActionResult<ProcessValidationResultDto>> PublishVersion(int versionId, CancellationToken cancellationToken)
    {
        var version = await _db.ProcessVersions
            .Include(x => x.ProcessDefinition)
            .FirstOrDefaultAsync(x => x.Id == versionId && !x.IsDeleted, cancellationToken);
        if (version is null) return NotFound();
        if (version.Status != ProcessVersionStatus.Draft)
        {
            return Conflict("Seule une version brouillon peut être publiée.");
        }

        var validation = await _validationService.ValidateAsync(versionId, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(validation);
        }

        var siblings = await _db.ProcessVersions
            .Where(x => x.ProcessDefinitionId == version.ProcessDefinitionId && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var sibling in siblings)
        {
            sibling.IsCurrent = false;
        }

        version.Status = ProcessVersionStatus.Published;
        version.IsCurrent = true;
        version.PublishedAt = DateTime.UtcNow;
        version.PublishedBy = CurrentUser();
        StampUpdated(version);
        version.ProcessDefinition.Status = ProcessDefinitionStatus.Active;
        StampUpdated(version.ProcessDefinition);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(validation);
    }

    [HttpPost("api/process-versions/{versionId:int}/validate")]
    public Task<ProcessValidationResultDto> ValidateVersion(int versionId, CancellationToken cancellationToken) =>
        _validationService.ValidateAsync(versionId, cancellationToken);

    [HttpGet("api/process-versions/{versionId:int}/diagram")]
    public async Task<ActionResult<WorkflowDiagramDto>> GetDiagram(int versionId, CancellationToken cancellationToken)
    {
        var version = await LoadVersionGraphAsync(versionId, tracking: false, cancellationToken);
        if (version is null) return NotFound();
        return Ok(ToDiagramDto(version));
    }

    [HttpPut("api/process-versions/{versionId:int}/diagram")]
    public async Task<ActionResult<WorkflowDiagramDto>> SaveDiagram(
        int versionId,
        WorkflowDiagramWriteDto dto,
        CancellationToken cancellationToken)
    {
        var version = await LoadVersionGraphAsync(versionId, tracking: true, cancellationToken);
        if (version is null) return NotFound();
        if (version.Status != ProcessVersionStatus.Draft) return Conflict("Une version publiée ne peut pas être modifiée.");

        version.CanvasWidth = dto.Version.CanvasWidth;
        version.CanvasHeight = dto.Version.CanvasHeight;
        version.ViewportJson = dto.Version.ViewportJson;
        version.LaneOrientation = dto.Version.LaneOrientation;
        StampUpdated(version);

        foreach (var laneDto in dto.Lanes)
        {
            var lane = version.Lanes.FirstOrDefault(x => x.Id == laneDto.Id);
            if (lane is null) continue;
            lane.X = laneDto.X;
            lane.Y = laneDto.Y;
            lane.Width = laneDto.Width;
            lane.Height = laneDto.Height;
            lane.OrderIndex = laneDto.OrderIndex;
            lane.Code = NormalizeCode(laneDto.Code);
            lane.Name = laneDto.Name.Trim();
            lane.Description = NullIfWhiteSpace(laneDto.Description);
            lane.ActorType = laneDto.ActorType;
            lane.ActorRefId = NullIfWhiteSpace(laneDto.ActorRefId);
            lane.StyleJson = laneDto.StyleJson;
            StampUpdated(lane);
        }

        foreach (var taskDto in dto.Tasks)
        {
            var task = version.Tasks.FirstOrDefault(x => x.Id == taskDto.Id);
            if (task is null) continue;
            task.WorkflowLaneId = taskDto.WorkflowLaneId;
            task.Code = NormalizeCode(taskDto.Code);
            task.Name = taskDto.Name.Trim();
            task.Description = NullIfWhiteSpace(taskDto.Description);
            task.TaskKind = taskDto.TaskKind;
            task.ExecutionMode = taskDto.ExecutionMode;
            task.AssignmentType = taskDto.AssignmentType;
            task.AssignmentExpression = NullIfWhiteSpace(taskDto.AssignmentExpression);
            task.ApplicationRefId = NullIfWhiteSpace(taskDto.ApplicationRefId);
            task.ExpectedDurationMinutes = taskDto.ExpectedDurationMinutes;
            task.SlaMinutes = taskDto.SlaMinutes;
            task.IsBlocking = taskDto.IsBlocking;
            task.X = taskDto.X;
            task.Y = taskDto.Y;
            task.Width = taskDto.Width;
            task.Height = taskDto.Height;
            task.StyleJson = taskDto.StyleJson;
            task.ConfigurationJson = NullIfWhiteSpace(taskDto.ConfigurationJson);
            StampUpdated(task);
        }

        foreach (var transitionDto in dto.Transitions)
        {
            var transition = version.Transitions.FirstOrDefault(x => x.Id == transitionDto.Id);
            if (transition is null) continue;
            transition.PointsJson = transitionDto.PointsJson;
            transition.StyleJson = transitionDto.StyleJson;
            transition.Code = string.IsNullOrWhiteSpace(transitionDto.Code)
                ? null
                : NormalizeCode(transitionDto.Code);
            transition.Name = NullIfWhiteSpace(transitionDto.Name);
            transition.Description = NullIfWhiteSpace(transitionDto.Description);
            transition.TransitionKind = transitionDto.TransitionKind;
            transition.ConditionExpression = NullIfWhiteSpace(transitionDto.ConditionExpression);
            transition.ConditionLabel = NullIfWhiteSpace(transitionDto.ConditionLabel);
            transition.OrderIndex = transitionDto.OrderIndex;
            StampUpdated(transition);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToDiagramDto(await LoadVersionGraphAsync(versionId, tracking: false, cancellationToken) ?? version));
    }

    [HttpPost("api/process-versions/{versionId:int}/lanes")]
    public async Task<ActionResult<WorkflowLaneDto>> CreateLane(int versionId, WorkflowLaneWriteDto dto, CancellationToken cancellationToken)
    {
        var version = await EnsureDraftVersionAsync(versionId, cancellationToken);
        if (version is null) return NotFound();
        if (version.Status != ProcessVersionStatus.Draft) return Conflict("Une version publiée ne peut pas être modifiée.");
        if (await _db.WorkflowLanes.AnyAsync(x => x.ProcessVersionId == versionId && x.Code == NormalizeCode(dto.Code) && !x.IsDeleted, cancellationToken))
        {
            return Conflict("Ce code de couloir existe déjà dans cette version.");
        }

        var lane = new WorkflowLane { ProcessVersionId = versionId };
        Apply(dto, lane);
        StampCreated(lane);
        _db.WorkflowLanes.Add(lane);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToLaneDto(lane));
    }

    [HttpPut("api/workflow-lanes/{laneId:int}")]
    public async Task<ActionResult<WorkflowLaneDto>> UpdateLane(int laneId, WorkflowLaneWriteDto dto, CancellationToken cancellationToken)
    {
        var lane = await _db.WorkflowLanes.Include(x => x.ProcessVersion).FirstOrDefaultAsync(x => x.Id == laneId && !x.IsDeleted, cancellationToken);
        if (lane is null) return NotFound();
        if (lane.ProcessVersion.Status != ProcessVersionStatus.Draft) return Conflict("Une version publiée ne peut pas être modifiée.");
        Apply(dto, lane);
        StampUpdated(lane);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToLaneDto(lane));
    }

    [HttpDelete("api/workflow-lanes/{laneId:int}")]
    public async Task<IActionResult> DeleteLane(int laneId, CancellationToken cancellationToken)
    {
        var lane = await _db.WorkflowLanes.Include(x => x.ProcessVersion).FirstOrDefaultAsync(x => x.Id == laneId && !x.IsDeleted, cancellationToken);
        if (lane is null) return NotFound();
        if (lane.ProcessVersion.Status != ProcessVersionStatus.Draft) return Conflict("Une version publiée ne peut pas être modifiée.");
        if (await _db.WorkflowTasks.AnyAsync(x => x.WorkflowLaneId == laneId && !x.IsDeleted, cancellationToken))
        {
            return Conflict("Ce couloir contient encore des tâches.");
        }
        lane.IsDeleted = true;
        StampUpdated(lane);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("api/process-versions/{versionId:int}/tasks")]
    public async Task<ActionResult<WorkflowTaskDto>> CreateTask(int versionId, WorkflowTaskWriteDto dto, CancellationToken cancellationToken)
    {
        var version = await EnsureDraftVersionAsync(versionId, cancellationToken);
        if (version is null) return NotFound();
        if (version.Status != ProcessVersionStatus.Draft) return Conflict("Une version publiée ne peut pas être modifiée.");
        if (!await _db.WorkflowLanes.AnyAsync(x => x.Id == dto.WorkflowLaneId && x.ProcessVersionId == versionId && !x.IsDeleted, cancellationToken))
        {
            return BadRequest("Le couloir de rattachement est invalide.");
        }
        if (await _db.WorkflowTasks.AnyAsync(x => x.ProcessVersionId == versionId && x.Code == NormalizeCode(dto.Code) && !x.IsDeleted, cancellationToken))
        {
            return Conflict("Ce code de tâche existe déjà dans cette version.");
        }

        var task = new WorkflowTask { ProcessVersionId = versionId };
        Apply(dto, task);
        StampCreated(task);
        _db.WorkflowTasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToTaskDto(task));
    }

    [HttpPut("api/workflow-tasks/{taskId:int}")]
    public async Task<ActionResult<WorkflowTaskDto>> UpdateTask(int taskId, WorkflowTaskWriteDto dto, CancellationToken cancellationToken)
    {
        var task = await _db.WorkflowTasks.Include(x => x.ProcessVersion).FirstOrDefaultAsync(x => x.Id == taskId && !x.IsDeleted, cancellationToken);
        if (task is null) return NotFound();
        if (task.ProcessVersion.Status != ProcessVersionStatus.Draft) return Conflict("Une version publiée ne peut pas être modifiée.");
        if (!await _db.WorkflowLanes.AnyAsync(x => x.Id == dto.WorkflowLaneId && x.ProcessVersionId == task.ProcessVersionId && !x.IsDeleted, cancellationToken))
        {
            return BadRequest("Le couloir de rattachement est invalide.");
        }
        Apply(dto, task);
        StampUpdated(task);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToTaskDto(task));
    }

    [HttpDelete("api/workflow-tasks/{taskId:int}")]
    public async Task<IActionResult> DeleteTask(int taskId, CancellationToken cancellationToken)
    {
        var task = await _db.WorkflowTasks.Include(x => x.ProcessVersion).FirstOrDefaultAsync(x => x.Id == taskId && !x.IsDeleted, cancellationToken);
        if (task is null) return NotFound();
        if (task.ProcessVersion.Status != ProcessVersionStatus.Draft) return Conflict("Une version publiée ne peut pas être modifiée.");
        task.IsDeleted = true;
        StampUpdated(task);
        var transitions = await _db.WorkflowTransitions
            .Where(x => (x.SourceTaskId == taskId || x.TargetTaskId == taskId) && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var transition in transitions)
        {
            transition.IsDeleted = true;
            StampUpdated(transition);
        }
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("api/process-versions/{versionId:int}/transitions")]
    public async Task<ActionResult<WorkflowTransitionDto>> CreateTransition(int versionId, WorkflowTransitionWriteDto dto, CancellationToken cancellationToken)
    {
        var version = await EnsureDraftVersionAsync(versionId, cancellationToken);
        if (version is null) return NotFound();
        if (version.Status != ProcessVersionStatus.Draft) return Conflict("Une version publiée ne peut pas être modifiée.");
        var error = await ValidateTransitionWriteAsync(versionId, dto, cancellationToken);
        if (error is not null) return BadRequest(error);

        var transition = new WorkflowTransition { ProcessVersionId = versionId };
        Apply(dto, transition);
        StampCreated(transition);
        _db.WorkflowTransitions.Add(transition);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToTransitionDto(transition));
    }

    [HttpPut("api/workflow-transitions/{transitionId:int}")]
    public async Task<ActionResult<WorkflowTransitionDto>> UpdateTransition(int transitionId, WorkflowTransitionWriteDto dto, CancellationToken cancellationToken)
    {
        var transition = await _db.WorkflowTransitions.Include(x => x.ProcessVersion).FirstOrDefaultAsync(x => x.Id == transitionId && !x.IsDeleted, cancellationToken);
        if (transition is null) return NotFound();
        if (transition.ProcessVersion.Status != ProcessVersionStatus.Draft) return Conflict("Une version publiée ne peut pas être modifiée.");
        var error = await ValidateTransitionWriteAsync(transition.ProcessVersionId, dto, cancellationToken);
        if (error is not null) return BadRequest(error);
        Apply(dto, transition);
        StampUpdated(transition);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToTransitionDto(transition));
    }

    [HttpDelete("api/workflow-transitions/{transitionId:int}")]
    public async Task<IActionResult> DeleteTransition(int transitionId, CancellationToken cancellationToken)
    {
        var transition = await _db.WorkflowTransitions.Include(x => x.ProcessVersion).FirstOrDefaultAsync(x => x.Id == transitionId && !x.IsDeleted, cancellationToken);
        if (transition is null) return NotFound();
        if (transition.ProcessVersion.Status != ProcessVersionStatus.Draft) return Conflict("Une version publiée ne peut pas être modifiée.");
        transition.IsDeleted = true;
        StampUpdated(transition);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("api/process-versions/{versionId:int}/instances")]
    public async Task<ActionResult<ProcessInstanceDto>> StartInstance(int versionId, ProcessInstanceCreateDto dto, CancellationToken cancellationToken)
    {
        var instance = await _runtimeService.StartAsync(versionId, dto, CurrentUser(), cancellationToken);
        return instance is null ? BadRequest("Impossible d'instancier cette version de processus.") : Ok(instance);
    }

    [HttpGet("api/process-instances/{instanceId:int}")]
    public async Task<ActionResult<ProcessInstanceDto>> GetInstance(int instanceId, CancellationToken cancellationToken)
    {
        var instance = await _db.ProcessInstances
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
                TaskInstances = x.TaskInstances.OrderBy(t => t.Id).Select(t => new WorkflowTaskInstanceDto
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
                }).ToList(),
            })
            .SingleOrDefaultAsync(cancellationToken);
        return instance is null ? NotFound() : Ok(instance);
    }

    [HttpPost("api/task-instances/{taskInstanceId:int}/complete")]
    public async Task<ActionResult<ProcessInstanceDto>> CompleteTaskInstance(
        int taskInstanceId,
        CompleteTaskInstanceDto dto,
        CancellationToken cancellationToken)
    {
        var instance = await _runtimeService.CompleteTaskAsync(taskInstanceId, dto, CurrentUser(), cancellationToken);
        return instance is null ? NotFound() : Ok(instance);
    }

    private async Task<ProcessVersion?> EnsureDraftVersionAsync(int versionId, CancellationToken cancellationToken)
    {
        var version = await _db.ProcessVersions.FirstOrDefaultAsync(x => x.Id == versionId && !x.IsDeleted, cancellationToken);
        return version is { Status: ProcessVersionStatus.Draft } ? version : version;
    }

    private async Task<string?> ValidateTransitionWriteAsync(int versionId, WorkflowTransitionWriteDto dto, CancellationToken cancellationToken)
    {
        if (dto.SourceTaskId == dto.TargetTaskId) return "Une transition ne peut pas pointer vers sa propre source.";
        var tasks = await _db.WorkflowTasks
            .Where(x => x.ProcessVersionId == versionId && (x.Id == dto.SourceTaskId || x.Id == dto.TargetTaskId) && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        return tasks.Count == 2 ? null : "La source et la cible doivent appartenir à la même version.";
    }

    private static void Apply(ProcessDefinitionWriteDto dto, ProcessDefinition process)
    {
        process.Code = NormalizeCode(dto.Code);
        process.Name = dto.Name.Trim();
        process.Description = NullIfWhiteSpace(dto.Description);
        process.Domain = NullIfWhiteSpace(dto.Domain);
        process.OwnerName = NullIfWhiteSpace(dto.OwnerName);
        process.Status = dto.Status;
    }

    private static void Apply(WorkflowLaneWriteDto dto, WorkflowLane lane)
    {
        lane.Code = NormalizeCode(dto.Code);
        lane.Name = dto.Name.Trim();
        lane.Description = NullIfWhiteSpace(dto.Description);
        lane.ActorType = dto.ActorType;
        lane.ActorRefId = NullIfWhiteSpace(dto.ActorRefId);
        lane.OrderIndex = dto.OrderIndex;
        lane.X = dto.X;
        lane.Y = dto.Y;
        lane.Width = dto.Width;
        lane.Height = dto.Height;
        lane.StyleJson = NullIfWhiteSpace(dto.StyleJson);
    }

    private static void Apply(WorkflowTaskWriteDto dto, WorkflowTask task)
    {
        task.WorkflowLaneId = dto.WorkflowLaneId;
        task.Code = NormalizeCode(dto.Code);
        task.Name = dto.Name.Trim();
        task.Description = NullIfWhiteSpace(dto.Description);
        task.TaskKind = dto.TaskKind;
        task.ExecutionMode = dto.ExecutionMode ?? DefaultExecutionMode(dto.TaskKind);
        task.AssignmentType = dto.AssignmentType;
        task.AssignmentExpression = NullIfWhiteSpace(dto.AssignmentExpression);
        task.ApplicationRefId = NullIfWhiteSpace(dto.ApplicationRefId);
        task.ExpectedDurationMinutes = dto.ExpectedDurationMinutes;
        task.SlaMinutes = dto.SlaMinutes;
        task.IsBlocking = dto.IsBlocking;
        task.X = dto.X;
        task.Y = dto.Y;
        task.Width = dto.Width;
        task.Height = dto.Height;
        task.StyleJson = NullIfWhiteSpace(dto.StyleJson);
        task.ConfigurationJson = NullIfWhiteSpace(dto.ConfigurationJson);
    }

    private static void Apply(WorkflowTransitionWriteDto dto, WorkflowTransition transition)
    {
        transition.SourceTaskId = dto.SourceTaskId;
        transition.TargetTaskId = dto.TargetTaskId;
        transition.Code = string.IsNullOrWhiteSpace(dto.Code) ? null : NormalizeCode(dto.Code);
        transition.Name = NullIfWhiteSpace(dto.Name);
        transition.Description = NullIfWhiteSpace(dto.Description);
        transition.TransitionKind = dto.TransitionKind;
        transition.ConditionExpression = NullIfWhiteSpace(dto.ConditionExpression);
        transition.ConditionLabel = NullIfWhiteSpace(dto.ConditionLabel);
        transition.OrderIndex = dto.OrderIndex;
        transition.PointsJson = NullIfWhiteSpace(dto.PointsJson);
        transition.StyleJson = NullIfWhiteSpace(dto.StyleJson);
    }

    private static WorkflowTaskExecutionMode DefaultExecutionMode(WorkflowTaskKind kind) =>
        kind switch
        {
            WorkflowTaskKind.Human => WorkflowTaskExecutionMode.Manual,
            WorkflowTaskKind.Machine => WorkflowTaskExecutionMode.Automatic,
            WorkflowTaskKind.Start or WorkflowTaskKind.End or WorkflowTaskKind.Gateway => WorkflowTaskExecutionMode.None,
            _ => WorkflowTaskExecutionMode.None,
        };

    private async Task<ProcessVersion?> LoadVersionGraphAsync(int versionId, bool tracking, CancellationToken cancellationToken)
    {
        var query = _db.ProcessVersions
            .Include(x => x.ProcessDefinition)
            .Include(x => x.Lanes.Where(l => !l.IsDeleted))
            .Include(x => x.Tasks.Where(t => !t.IsDeleted))
            .Include(x => x.Transitions.Where(t => !t.IsDeleted))
            .Where(x => x.Id == versionId && !x.IsDeleted);
        if (!tracking) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private static WorkflowDiagramDto ToDiagramDto(ProcessVersion version) =>
        new()
        {
            Process = ToProcessDto(version.ProcessDefinition),
            Version = ToVersionDto(version),
            Lanes = version.Lanes.OrderBy(x => x.OrderIndex).Select(ToLaneDto).ToList(),
            Tasks = version.Tasks.OrderBy(x => x.Id).Select(ToTaskDto).ToList(),
            Transitions = version.Transitions.OrderBy(x => x.OrderIndex).Select(ToTransitionDto).ToList(),
        };

    private async Task<ProcessDefinitionDto> GetProcessDtoAsync(int id, CancellationToken cancellationToken)
    {
        var process = await _db.ProcessDefinitions.AsNoTracking()
            .Include(x => x.Versions.Where(v => !v.IsDeleted))
            .Where(x => x.Id == id)
            .SingleAsync(cancellationToken);
        return ToProcessDto(process);
    }

    private static ProcessDefinitionDto ToProcessDto(ProcessDefinition x) =>
        new()
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Domain = x.Domain,
            OwnerName = x.OwnerName,
            Status = x.Status,
            VersionCount = x.Versions.Count(v => !v.IsDeleted),
            CurrentVersionId = x.Versions.Where(v => v.IsCurrent && !v.IsDeleted).Select(v => (int?)v.Id).FirstOrDefault(),
            CreatedAt = x.CreatedAt,
            CreatedBy = x.CreatedBy,
            UpdatedAt = x.UpdatedAt,
            UpdatedBy = x.UpdatedBy,
            RowVersion = x.RowVersion,
        };

    private static ProcessVersionDto ToVersionDto(ProcessVersion x) =>
        new()
        {
            Id = x.Id,
            ProcessDefinitionId = x.ProcessDefinitionId,
            VersionNumber = x.VersionNumber,
            Label = x.Label,
            Status = x.Status,
            IsCurrent = x.IsCurrent,
            PublishedAt = x.PublishedAt,
            PublishedBy = x.PublishedBy,
            CanvasWidth = x.CanvasWidth,
            CanvasHeight = x.CanvasHeight,
            ViewportJson = x.ViewportJson,
            LaneOrientation = x.LaneOrientation,
            CreatedAt = x.CreatedAt,
            CreatedBy = x.CreatedBy,
            UpdatedAt = x.UpdatedAt,
            UpdatedBy = x.UpdatedBy,
            RowVersion = x.RowVersion,
        };

    private static WorkflowLaneDto ToLaneDto(WorkflowLane x) =>
        new()
        {
            Id = x.Id,
            ProcessVersionId = x.ProcessVersionId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            ActorType = x.ActorType,
            ActorRefId = x.ActorRefId,
            OrderIndex = x.OrderIndex,
            X = x.X,
            Y = x.Y,
            Width = x.Width,
            Height = x.Height,
            StyleJson = x.StyleJson,
            RowVersion = x.RowVersion,
        };

    private static WorkflowTaskDto ToTaskDto(WorkflowTask x) =>
        new()
        {
            Id = x.Id,
            ProcessVersionId = x.ProcessVersionId,
            WorkflowLaneId = x.WorkflowLaneId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            TaskKind = x.TaskKind,
            ExecutionMode = x.ExecutionMode,
            AssignmentType = x.AssignmentType,
            AssignmentExpression = x.AssignmentExpression,
            ApplicationRefId = x.ApplicationRefId,
            ExpectedDurationMinutes = x.ExpectedDurationMinutes,
            SlaMinutes = x.SlaMinutes,
            IsBlocking = x.IsBlocking,
            X = x.X,
            Y = x.Y,
            Width = x.Width,
            Height = x.Height,
            StyleJson = x.StyleJson,
            ConfigurationJson = x.ConfigurationJson,
            RowVersion = x.RowVersion,
        };

    private static WorkflowTransitionDto ToTransitionDto(WorkflowTransition x) =>
        new()
        {
            Id = x.Id,
            ProcessVersionId = x.ProcessVersionId,
            SourceTaskId = x.SourceTaskId,
            TargetTaskId = x.TargetTaskId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            TransitionKind = x.TransitionKind,
            ConditionExpression = x.ConditionExpression,
            ConditionLabel = x.ConditionLabel,
            OrderIndex = x.OrderIndex,
            PointsJson = x.PointsJson,
            StyleJson = x.StyleJson,
            RowVersion = x.RowVersion,
        };

    private string? CurrentUser() => User?.Identity?.Name;

    private void StampCreated(WorkflowAuditableEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = CurrentUser();
    }

    private void StampUpdated(WorkflowAuditableEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = CurrentUser();
    }

    private static string NormalizeCode(string code) =>
        (code ?? string.Empty).Trim().ToUpperInvariant();

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
