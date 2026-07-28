using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowProcessDesigner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.CreateTable(
                name: "ProcessDefinitions",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Domain = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessVersions",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessDefinitionId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CanvasWidth = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CanvasHeight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ViewportJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LaneOrientation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessVersions_ProcessDefinitions_ProcessDefinitionId",
                        column: x => x.ProcessDefinitionId,
                        principalSchema: "workflow",
                        principalTable: "ProcessDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowLanes",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessVersionId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActorType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorRefId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Y = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Width = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Height = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StyleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowLanes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowLanes_ProcessVersions_ProcessVersionId",
                        column: x => x.ProcessVersionId,
                        principalSchema: "workflow",
                        principalTable: "ProcessVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTasks",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessVersionId = table.Column<int>(type: "int", nullable: false),
                    WorkflowLaneId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TaskKind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExecutionMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AssignmentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AssignmentExpression = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApplicationRefId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ExpectedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    SlaMinutes = table.Column<int>(type: "int", nullable: true),
                    IsBlocking = table.Column<bool>(type: "bit", nullable: false),
                    X = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Y = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Width = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Height = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StyleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTasks_ProcessVersions_ProcessVersionId",
                        column: x => x.ProcessVersionId,
                        principalSchema: "workflow",
                        principalTable: "ProcessVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowTasks_WorkflowLanes_WorkflowLaneId",
                        column: x => x.WorkflowLaneId,
                        principalSchema: "workflow",
                        principalTable: "WorkflowLanes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessInstances",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessVersionId = table.Column<int>(type: "int", nullable: false),
                    BusinessKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentTaskId = table.Column<int>(type: "int", nullable: true),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessInstances_ProcessVersions_ProcessVersionId",
                        column: x => x.ProcessVersionId,
                        principalSchema: "workflow",
                        principalTable: "ProcessVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessInstances_WorkflowTasks_CurrentTaskId",
                        column: x => x.CurrentTaskId,
                        principalSchema: "workflow",
                        principalTable: "WorkflowTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitions",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessVersionId = table.Column<int>(type: "int", nullable: false),
                    SourceTaskId = table.Column<int>(type: "int", nullable: false),
                    TargetTaskId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TransitionKind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ConditionExpression = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConditionLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    PointsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StyleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_ProcessVersions_ProcessVersionId",
                        column: x => x.ProcessVersionId,
                        principalSchema: "workflow",
                        principalTable: "ProcessVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_WorkflowTasks_SourceTaskId",
                        column: x => x.SourceTaskId,
                        principalSchema: "workflow",
                        principalTable: "WorkflowTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_WorkflowTasks_TargetTaskId",
                        column: x => x.TargetTaskId,
                        principalSchema: "workflow",
                        principalTable: "WorkflowTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTaskInstances",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessInstanceId = table.Column<int>(type: "int", nullable: false),
                    WorkflowTaskId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AssignedTo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTaskInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTaskInstances_ProcessInstances_ProcessInstanceId",
                        column: x => x.ProcessInstanceId,
                        principalSchema: "workflow",
                        principalTable: "ProcessInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowTaskInstances_WorkflowTasks_WorkflowTaskId",
                        column: x => x.WorkflowTaskId,
                        principalSchema: "workflow",
                        principalTable: "WorkflowTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowEventLogs",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessInstanceId = table.Column<int>(type: "int", nullable: false),
                    WorkflowTaskInstanceId = table.Column<int>(type: "int", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowEventLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowEventLogs_ProcessInstances_ProcessInstanceId",
                        column: x => x.ProcessInstanceId,
                        principalSchema: "workflow",
                        principalTable: "ProcessInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowEventLogs_WorkflowTaskInstances_WorkflowTaskInstanceId",
                        column: x => x.WorkflowTaskInstanceId,
                        principalSchema: "workflow",
                        principalTable: "WorkflowTaskInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessDefinitions_Code",
                schema: "workflow",
                table: "ProcessDefinitions",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessDefinitions_IsDeleted",
                schema: "workflow",
                table: "ProcessDefinitions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessInstances_CurrentTaskId",
                schema: "workflow",
                table: "ProcessInstances",
                column: "CurrentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessInstances_ProcessVersionId",
                schema: "workflow",
                table: "ProcessInstances",
                column: "ProcessVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessVersions_IsDeleted",
                schema: "workflow",
                table: "ProcessVersions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessVersions_ProcessDefinitionId",
                schema: "workflow",
                table: "ProcessVersions",
                column: "ProcessDefinitionId",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessVersions_ProcessDefinitionId_VersionNumber",
                schema: "workflow",
                table: "ProcessVersions",
                columns: new[] { "ProcessDefinitionId", "VersionNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEventLogs_ProcessInstanceId_CreatedAt",
                schema: "workflow",
                table: "WorkflowEventLogs",
                columns: new[] { "ProcessInstanceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEventLogs_WorkflowTaskInstanceId",
                schema: "workflow",
                table: "WorkflowEventLogs",
                column: "WorkflowTaskInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowLanes_IsDeleted",
                schema: "workflow",
                table: "WorkflowLanes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowLanes_ProcessVersionId_Code",
                schema: "workflow",
                table: "WorkflowLanes",
                columns: new[] { "ProcessVersionId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskInstances_ProcessInstanceId",
                schema: "workflow",
                table: "WorkflowTaskInstances",
                column: "ProcessInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskInstances_WorkflowTaskId",
                schema: "workflow",
                table: "WorkflowTaskInstances",
                column: "WorkflowTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTasks_IsDeleted",
                schema: "workflow",
                table: "WorkflowTasks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTasks_ProcessVersionId_Code",
                schema: "workflow",
                table: "WorkflowTasks",
                columns: new[] { "ProcessVersionId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTasks_ProcessVersionId_WorkflowLaneId",
                schema: "workflow",
                table: "WorkflowTasks",
                columns: new[] { "ProcessVersionId", "WorkflowLaneId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTasks_WorkflowLaneId",
                schema: "workflow",
                table: "WorkflowTasks",
                column: "WorkflowLaneId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_IsDeleted",
                schema: "workflow",
                table: "WorkflowTransitions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_ProcessVersionId",
                schema: "workflow",
                table: "WorkflowTransitions",
                column: "ProcessVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_SourceTaskId_TargetTaskId_TransitionKind",
                schema: "workflow",
                table: "WorkflowTransitions",
                columns: new[] { "SourceTaskId", "TargetTaskId", "TransitionKind" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_TargetTaskId",
                schema: "workflow",
                table: "WorkflowTransitions",
                column: "TargetTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowEventLogs",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "WorkflowTransitions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "WorkflowTaskInstances",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "ProcessInstances",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "WorkflowTasks",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "WorkflowLanes",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "ProcessVersions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "ProcessDefinitions",
                schema: "workflow");
        }
    }
}
