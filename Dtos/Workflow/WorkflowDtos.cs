using api.Models.Workflow;

namespace api.Dtos.Workflow;

public sealed class ProcessDefinitionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Domain { get; set; }
    public string? OwnerName { get; set; }
    public ProcessDefinitionStatus Status { get; set; }
    public int VersionCount { get; set; }
    public int? CurrentVersionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProcessDefinitionWriteDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Domain { get; set; }
    public string? OwnerName { get; set; }
    public ProcessDefinitionStatus Status { get; set; } = ProcessDefinitionStatus.Draft;
}

public sealed class ProcessVersionDto
{
    public int Id { get; set; }
    public int ProcessDefinitionId { get; set; }
    public int VersionNumber { get; set; }
    public string? Label { get; set; }
    public ProcessVersionStatus Status { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
    public decimal? CanvasWidth { get; set; }
    public decimal? CanvasHeight { get; set; }
    public string? ViewportJson { get; set; }
    public string LaneOrientation { get; set; } = "Vertical";
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ProcessVersionCreateDto
{
    public string? Label { get; set; }
    public decimal? CanvasWidth { get; set; } = 2400;
    public decimal? CanvasHeight { get; set; } = 1400;
    public string? ViewportJson { get; set; }
    public string LaneOrientation { get; set; } = "Vertical";
}

public sealed class WorkflowLaneDto
{
    public int Id { get; set; }
    public int ProcessVersionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowActorType ActorType { get; set; }
    public string? ActorRefId { get; set; }
    public int OrderIndex { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string? StyleJson { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class WorkflowLaneWriteDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowActorType ActorType { get; set; } = WorkflowActorType.OrganizationUnit;
    public string? ActorRefId { get; set; }
    public int OrderIndex { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; } = 1800;
    public decimal Height { get; set; } = 180;
    public string? StyleJson { get; set; }
}

public sealed class WorkflowTaskDto
{
    public int Id { get; set; }
    public int ProcessVersionId { get; set; }
    public int WorkflowLaneId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowTaskKind TaskKind { get; set; }
    public WorkflowTaskExecutionMode ExecutionMode { get; set; }
    public WorkflowAssignmentType? AssignmentType { get; set; }
    public string? AssignmentExpression { get; set; }
    public string? ApplicationRefId { get; set; }
    public int? ExpectedDurationMinutes { get; set; }
    public int? SlaMinutes { get; set; }
    public bool IsBlocking { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string? StyleJson { get; set; }
    public string? ConfigurationJson { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class WorkflowTaskWriteDto
{
    public int WorkflowLaneId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowTaskKind TaskKind { get; set; } = WorkflowTaskKind.Human;
    public WorkflowTaskExecutionMode? ExecutionMode { get; set; }
    public WorkflowAssignmentType? AssignmentType { get; set; }
    public string? AssignmentExpression { get; set; }
    public string? ApplicationRefId { get; set; }
    public int? ExpectedDurationMinutes { get; set; }
    public int? SlaMinutes { get; set; }
    public bool IsBlocking { get; set; } = true;
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; } = 190;
    public decimal Height { get; set; } = 80;
    public string? StyleJson { get; set; }
    public string? ConfigurationJson { get; set; }
}

public sealed class WorkflowTransitionDto
{
    public int Id { get; set; }
    public int ProcessVersionId { get; set; }
    public int SourceTaskId { get; set; }
    public int TargetTaskId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public WorkflowTransitionKind TransitionKind { get; set; }
    public string? ConditionExpression { get; set; }
    public string? ConditionLabel { get; set; }
    public int OrderIndex { get; set; }
    public string? PointsJson { get; set; }
    public string? StyleJson { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class WorkflowTransitionWriteDto
{
    public int SourceTaskId { get; set; }
    public int TargetTaskId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public WorkflowTransitionKind TransitionKind { get; set; } = WorkflowTransitionKind.Sequence;
    public string? ConditionExpression { get; set; }
    public string? ConditionLabel { get; set; }
    public int OrderIndex { get; set; }
    public string? PointsJson { get; set; }
    public string? StyleJson { get; set; }
}

public sealed class WorkflowDiagramDto
{
    public ProcessDefinitionDto Process { get; set; } = new();
    public ProcessVersionDto Version { get; set; } = new();
    public List<WorkflowLaneDto> Lanes { get; set; } = [];
    public List<WorkflowTaskDto> Tasks { get; set; } = [];
    public List<WorkflowTransitionDto> Transitions { get; set; } = [];
}

public sealed class WorkflowDiagramWriteDto
{
    public ProcessVersionCreateDto Version { get; set; } = new();
    public List<WorkflowLaneDto> Lanes { get; set; } = [];
    public List<WorkflowTaskDto> Tasks { get; set; } = [];
    public List<WorkflowTransitionDto> Transitions { get; set; } = [];
}

public sealed class ProcessValidationIssueDto
{
    public string Severity { get; set; } = "Error";
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ObjectType { get; set; }
    public int? ObjectId { get; set; }
}

public sealed class ProcessValidationResultDto
{
    public bool IsValid => Issues.All(x => !string.Equals(x.Severity, "Error", StringComparison.OrdinalIgnoreCase));
    public List<ProcessValidationIssueDto> Issues { get; set; } = [];
}

public sealed class ProcessInstanceCreateDto
{
    public string? BusinessKey { get; set; }
    public string? Title { get; set; }
    public string? ContextJson { get; set; }
}

public sealed class ProcessInstanceDto
{
    public int Id { get; set; }
    public int ProcessVersionId { get; set; }
    public string? BusinessKey { get; set; }
    public string? Title { get; set; }
    public ProcessInstanceStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public string? StartedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CurrentTaskId { get; set; }
    public string? ContextJson { get; set; }
    public List<WorkflowTaskInstanceDto> TaskInstances { get; set; } = [];
}

public sealed class WorkflowTaskInstanceDto
{
    public int Id { get; set; }
    public int ProcessInstanceId { get; set; }
    public int WorkflowTaskId { get; set; }
    public string WorkflowTaskName { get; set; } = string.Empty;
    public WorkflowTaskInstanceStatus Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResultJson { get; set; }
}

public sealed class CompleteTaskInstanceDto
{
    public string? ResultJson { get; set; }
}
