using api.Data;
using api.Models.Workflow;
using api.Services.Workflow;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests;

public sealed class ProcessValidationServiceTests
{
    [Fact]
    public async Task ValidateAsync_AcceptsLinearWorkflow()
    {
        await using var db = CreateDb();
        var version = await SeedLinearWorkflowAsync(db);
        var service = new ProcessValidationService(db);

        var result = await service.ValidateAsync(version.Id, CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task ValidateAsync_RejectsWorkflowWithoutEnd()
    {
        await using var db = CreateDb();
        var version = await SeedLinearWorkflowAsync(db);
        var endTask = await db.WorkflowTasks.SingleAsync(x => x.ProcessVersionId == version.Id && x.TaskKind == WorkflowTaskKind.End);
        endTask.IsDeleted = true;
        await db.SaveChangesAsync();
        var service = new ProcessValidationService(db);

        var result = await service.ValidateAsync(version.Id, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "END_REQUIRED");
    }

    [Fact]
    public async Task ValidateAsync_RejectsUnreachableTask()
    {
        await using var db = CreateDb();
        var version = await SeedLinearWorkflowAsync(db);
        var lane = await db.WorkflowLanes.SingleAsync(x => x.ProcessVersionId == version.Id);
        var orphan = new WorkflowTask
        {
            ProcessVersionId = version.Id,
            WorkflowLaneId = lane.Id,
            Code = "ORPHAN",
            Name = "Tâche isolée",
            TaskKind = WorkflowTaskKind.Human,
            ExecutionMode = WorkflowTaskExecutionMode.Manual,
            X = 500,
            Y = 40,
        };
        db.WorkflowTasks.Add(orphan);
        await db.SaveChangesAsync();
        var service = new ProcessValidationService(db);

        var result = await service.ValidateAsync(version.Id, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "TASK_NOT_REACHABLE" && issue.ObjectId == orphan.Id);
    }

    private static ApplicationDBContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase($"workflow-validation-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDBContext(options);
    }

    private static async Task<ProcessVersion> SeedLinearWorkflowAsync(ApplicationDBContext db)
    {
        var process = new ProcessDefinition
        {
            Code = "PROC_TEST",
            Name = "Processus de test",
            Status = ProcessDefinitionStatus.Draft,
        };
        var version = new ProcessVersion
        {
            ProcessDefinition = process,
            VersionNumber = 1,
            Status = ProcessVersionStatus.Draft,
        };
        var lane = new WorkflowLane
        {
            ProcessVersion = version,
            Code = "LANE_METIER",
            Name = "Métier",
            ActorType = WorkflowActorType.OrganizationUnit,
        };
        var start = new WorkflowTask
        {
            ProcessVersion = version,
            WorkflowLane = lane,
            Code = "START",
            Name = "Début",
            TaskKind = WorkflowTaskKind.Start,
            ExecutionMode = WorkflowTaskExecutionMode.None,
        };
        var human = new WorkflowTask
        {
            ProcessVersion = version,
            WorkflowLane = lane,
            Code = "VALIDATE",
            Name = "Valider",
            TaskKind = WorkflowTaskKind.Human,
            ExecutionMode = WorkflowTaskExecutionMode.Manual,
        };
        var end = new WorkflowTask
        {
            ProcessVersion = version,
            WorkflowLane = lane,
            Code = "END",
            Name = "Fin",
            TaskKind = WorkflowTaskKind.End,
            ExecutionMode = WorkflowTaskExecutionMode.None,
        };

        db.AddRange(process, version, lane, start, human, end);
        await db.SaveChangesAsync();
        db.WorkflowTransitions.AddRange(
            new WorkflowTransition
            {
                ProcessVersionId = version.Id,
                SourceTaskId = start.Id,
                TargetTaskId = human.Id,
                TransitionKind = WorkflowTransitionKind.Sequence,
            },
            new WorkflowTransition
            {
                ProcessVersionId = version.Id,
                SourceTaskId = human.Id,
                TargetTaskId = end.Id,
                TransitionKind = WorkflowTransitionKind.Sequence,
            });
        await db.SaveChangesAsync();
        return version;
    }
}
