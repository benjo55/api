using api.Models.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Models.Configurations;

internal static class WorkflowConfigurationHelpers
{
    public static void ConfigureAudit<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : WorkflowAuditableEntity
    {
        entity.Property(x => x.CreatedBy).HasMaxLength(120);
        entity.Property(x => x.UpdatedBy).HasMaxLength(120);
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => x.IsDeleted);
    }
}

public sealed class ProcessDefinitionConfiguration : IEntityTypeConfiguration<ProcessDefinition>
{
    public void Configure(EntityTypeBuilder<ProcessDefinition> entity)
    {
        entity.ToTable("ProcessDefinitions", "workflow");
        entity.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(4000);
        entity.Property(x => x.Domain).HasMaxLength(160);
        entity.Property(x => x.OwnerName).HasMaxLength(200);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        WorkflowConfigurationHelpers.ConfigureAudit(entity);
    }
}

public sealed class ProcessVersionConfiguration : IEntityTypeConfiguration<ProcessVersion>
{
    public void Configure(EntityTypeBuilder<ProcessVersion> entity)
    {
        entity.ToTable("ProcessVersions", "workflow");
        entity.HasIndex(x => new { x.ProcessDefinitionId, x.VersionNumber }).IsUnique().HasFilter("[IsDeleted] = 0");
        entity.HasIndex(x => x.ProcessDefinitionId)
            .IsUnique()
            .HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        entity.Property(x => x.Label).HasMaxLength(200);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        entity.Property(x => x.LaneOrientation).HasMaxLength(30).IsRequired();
        entity.Property(x => x.CanvasWidth).HasPrecision(18, 2);
        entity.Property(x => x.CanvasHeight).HasPrecision(18, 2);
        entity.Property(x => x.ViewportJson).HasColumnType("nvarchar(max)");
        entity.Property(x => x.PublishedBy).HasMaxLength(120);
        entity.HasOne(x => x.ProcessDefinition)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.ProcessDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
        WorkflowConfigurationHelpers.ConfigureAudit(entity);
    }
}

public sealed class WorkflowLaneConfiguration : IEntityTypeConfiguration<WorkflowLane>
{
    public void Configure(EntityTypeBuilder<WorkflowLane> entity)
    {
        entity.ToTable("WorkflowLanes", "workflow");
        entity.HasIndex(x => new { x.ProcessVersionId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
        entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(2000);
        entity.Property(x => x.ActorType).HasConversion<string>().HasMaxLength(40).IsRequired();
        entity.Property(x => x.ActorRefId).HasMaxLength(120);
        entity.Property(x => x.X).HasPrecision(18, 2);
        entity.Property(x => x.Y).HasPrecision(18, 2);
        entity.Property(x => x.Width).HasPrecision(18, 2);
        entity.Property(x => x.Height).HasPrecision(18, 2);
        entity.Property(x => x.StyleJson).HasColumnType("nvarchar(max)");
        entity.HasOne(x => x.ProcessVersion)
            .WithMany(x => x.Lanes)
            .HasForeignKey(x => x.ProcessVersionId)
            .OnDelete(DeleteBehavior.Cascade);
        WorkflowConfigurationHelpers.ConfigureAudit(entity);
    }
}

public sealed class WorkflowTaskConfiguration : IEntityTypeConfiguration<WorkflowTask>
{
    public void Configure(EntityTypeBuilder<WorkflowTask> entity)
    {
        entity.ToTable("WorkflowTasks", "workflow");
        entity.HasIndex(x => new { x.ProcessVersionId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
        entity.HasIndex(x => new { x.ProcessVersionId, x.WorkflowLaneId });
        entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(4000);
        entity.Property(x => x.TaskKind).HasConversion<string>().HasMaxLength(30).IsRequired();
        entity.Property(x => x.ExecutionMode).HasConversion<string>().HasMaxLength(30).IsRequired();
        entity.Property(x => x.AssignmentType).HasConversion<string>().HasMaxLength(30);
        entity.Property(x => x.AssignmentExpression).HasMaxLength(1000);
        entity.Property(x => x.ApplicationRefId).HasMaxLength(120);
        entity.Property(x => x.X).HasPrecision(18, 2);
        entity.Property(x => x.Y).HasPrecision(18, 2);
        entity.Property(x => x.Width).HasPrecision(18, 2);
        entity.Property(x => x.Height).HasPrecision(18, 2);
        entity.Property(x => x.StyleJson).HasColumnType("nvarchar(max)");
        entity.Property(x => x.ConfigurationJson).HasColumnType("nvarchar(max)");
        entity.HasOne(x => x.ProcessVersion)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.ProcessVersionId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.WorkflowLane)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.WorkflowLaneId)
            .OnDelete(DeleteBehavior.Restrict);
        WorkflowConfigurationHelpers.ConfigureAudit(entity);
    }
}

public sealed class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> entity)
    {
        entity.ToTable("WorkflowTransitions", "workflow");
        entity.HasIndex(x => new { x.SourceTaskId, x.TargetTaskId, x.TransitionKind })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        entity.Property(x => x.Code).HasMaxLength(80);
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.Description).HasMaxLength(2000);
        entity.Property(x => x.TransitionKind).HasConversion<string>().HasMaxLength(30).IsRequired();
        entity.Property(x => x.ConditionExpression).HasMaxLength(1000);
        entity.Property(x => x.ConditionLabel).HasMaxLength(200);
        entity.Property(x => x.PointsJson).HasColumnType("nvarchar(max)");
        entity.Property(x => x.StyleJson).HasColumnType("nvarchar(max)");
        entity.HasOne(x => x.ProcessVersion)
            .WithMany(x => x.Transitions)
            .HasForeignKey(x => x.ProcessVersionId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.SourceTask)
            .WithMany(x => x.OutgoingTransitions)
            .HasForeignKey(x => x.SourceTaskId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.TargetTask)
            .WithMany(x => x.IncomingTransitions)
            .HasForeignKey(x => x.TargetTaskId)
            .OnDelete(DeleteBehavior.Restrict);
        WorkflowConfigurationHelpers.ConfigureAudit(entity);
    }
}

public sealed class ProcessInstanceConfiguration : IEntityTypeConfiguration<ProcessInstance>
{
    public void Configure(EntityTypeBuilder<ProcessInstance> entity)
    {
        entity.ToTable("ProcessInstances", "workflow");
        entity.Property(x => x.BusinessKey).HasMaxLength(160);
        entity.Property(x => x.Title).HasMaxLength(300);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        entity.Property(x => x.StartedBy).HasMaxLength(120);
        entity.Property(x => x.ContextJson).HasColumnType("nvarchar(max)");
        entity.HasOne(x => x.ProcessVersion)
            .WithMany(x => x.Instances)
            .HasForeignKey(x => x.ProcessVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.CurrentTask)
            .WithMany()
            .HasForeignKey(x => x.CurrentTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WorkflowTaskInstanceConfiguration : IEntityTypeConfiguration<WorkflowTaskInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowTaskInstance> entity)
    {
        entity.ToTable("WorkflowTaskInstances", "workflow");
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        entity.Property(x => x.AssignedTo).HasMaxLength(200);
        entity.Property(x => x.ResultJson).HasColumnType("nvarchar(max)");
        entity.HasOne(x => x.ProcessInstance)
            .WithMany(x => x.TaskInstances)
            .HasForeignKey(x => x.ProcessInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.WorkflowTask)
            .WithMany()
            .HasForeignKey(x => x.WorkflowTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WorkflowEventLogConfiguration : IEntityTypeConfiguration<WorkflowEventLog>
{
    public void Configure(EntityTypeBuilder<WorkflowEventLog> entity)
    {
        entity.ToTable("WorkflowEventLogs", "workflow");
        entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Message).HasMaxLength(2000);
        entity.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
        entity.Property(x => x.CreatedBy).HasMaxLength(120);
        entity.HasIndex(x => new { x.ProcessInstanceId, x.CreatedAt });
        entity.HasOne(x => x.ProcessInstance)
            .WithMany(x => x.EventLogs)
            .HasForeignKey(x => x.ProcessInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.WorkflowTaskInstance)
            .WithMany()
            .HasForeignKey(x => x.WorkflowTaskInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
