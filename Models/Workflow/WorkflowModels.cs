namespace api.Models.Workflow;

public enum ProcessDefinitionStatus
{
    Draft,
    Active,
    Archived
}

public enum ProcessVersionStatus
{
    Draft,
    Published,
    Archived
}

public enum WorkflowActorType
{
    OrganizationUnit,
    Role,
    Team,
    Application,
    ExternalPartner,
    System,
    Other
}

public enum WorkflowTaskKind
{
    Start,
    End,
    Human,
    Machine,
    Gateway,
    SubProcess
}

public enum WorkflowTaskExecutionMode
{
    None,
    Manual,
    Automatic,
    ExternalSystem,
    ApiCall,
    Batch,
    Message
}

public enum WorkflowAssignmentType
{
    LaneActor,
    SpecificRole,
    SpecificUser,
    Expression
}

public enum WorkflowTransitionKind
{
    Sequence,
    Conditional,
    Error,
    Timeout,
    Escalation
}

public enum ProcessInstanceStatus
{
    Running,
    Completed,
    Cancelled,
    Failed,
    Suspended
}

public enum WorkflowTaskInstanceStatus
{
    Pending,
    Ready,
    InProgress,
    Completed,
    Skipped,
    Failed,
    Cancelled
}

public abstract class WorkflowAuditableEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public class ProcessDefinition : WorkflowAuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Domain { get; set; }
    public string? OwnerName { get; set; }
    public ProcessDefinitionStatus Status { get; set; } = ProcessDefinitionStatus.Draft;
    public ICollection<ProcessVersion> Versions { get; set; } = [];
}

public class ProcessVersion : WorkflowAuditableEntity
{
    public int Id { get; set; }
    public int ProcessDefinitionId { get; set; }
    public int VersionNumber { get; set; }
    public string? Label { get; set; }
    public ProcessVersionStatus Status { get; set; } = ProcessVersionStatus.Draft;
    public bool IsCurrent { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
    public decimal? CanvasWidth { get; set; } = 2400;
    public decimal? CanvasHeight { get; set; } = 1400;
    public string? ViewportJson { get; set; }
    public string LaneOrientation { get; set; } = "Vertical";

    public ProcessDefinition ProcessDefinition { get; set; } = null!;
    public ICollection<WorkflowLane> Lanes { get; set; } = [];
    public ICollection<WorkflowTask> Tasks { get; set; } = [];
    public ICollection<WorkflowTransition> Transitions { get; set; } = [];
    public ICollection<ProcessInstance> Instances { get; set; } = [];
}

public class WorkflowLane : WorkflowAuditableEntity
{
    public int Id { get; set; }
    public int ProcessVersionId { get; set; }
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

    public ProcessVersion ProcessVersion { get; set; } = null!;
    public ICollection<WorkflowTask> Tasks { get; set; } = [];
}

public class WorkflowTask : WorkflowAuditableEntity
{
    public int Id { get; set; }
    public int ProcessVersionId { get; set; }
    public int WorkflowLaneId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowTaskKind TaskKind { get; set; } = WorkflowTaskKind.Human;
    public WorkflowTaskExecutionMode ExecutionMode { get; set; } = WorkflowTaskExecutionMode.Manual;
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

    public ProcessVersion ProcessVersion { get; set; } = null!;
    public WorkflowLane WorkflowLane { get; set; } = null!;
    public ICollection<WorkflowTransition> OutgoingTransitions { get; set; } = [];
    public ICollection<WorkflowTransition> IncomingTransitions { get; set; } = [];
}

public class WorkflowTransition : WorkflowAuditableEntity
{
    public int Id { get; set; }
    public int ProcessVersionId { get; set; }
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

    public ProcessVersion ProcessVersion { get; set; } = null!;
    public WorkflowTask SourceTask { get; set; } = null!;
    public WorkflowTask TargetTask { get; set; } = null!;
}

public class ProcessInstance
{
    public int Id { get; set; }
    public int ProcessVersionId { get; set; }
    public string? BusinessKey { get; set; }
    public string? Title { get; set; }
    public ProcessInstanceStatus Status { get; set; } = ProcessInstanceStatus.Running;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public string? StartedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CurrentTaskId { get; set; }
    public string? ContextJson { get; set; }

    public ProcessVersion ProcessVersion { get; set; } = null!;
    public WorkflowTask? CurrentTask { get; set; }
    public ICollection<WorkflowTaskInstance> TaskInstances { get; set; } = [];
    public ICollection<WorkflowEventLog> EventLogs { get; set; } = [];
}

public class WorkflowTaskInstance
{
    public int Id { get; set; }
    public int ProcessInstanceId { get; set; }
    public int WorkflowTaskId { get; set; }
    public WorkflowTaskInstanceStatus Status { get; set; } = WorkflowTaskInstanceStatus.Pending;
    public string? AssignedTo { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResultJson { get; set; }

    public ProcessInstance ProcessInstance { get; set; } = null!;
    public WorkflowTask WorkflowTask { get; set; } = null!;
}

public class WorkflowEventLog
{
    public int Id { get; set; }
    public int ProcessInstanceId { get; set; }
    public int? WorkflowTaskInstanceId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    public ProcessInstance ProcessInstance { get; set; } = null!;
    public WorkflowTaskInstance? WorkflowTaskInstance { get; set; }
}
