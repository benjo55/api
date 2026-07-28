using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddCmdbCartography : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cmdb");

            migrationBuilder.EnsureSchema(
                name: "integration");

            migrationBuilder.CreateTable(
                name: "AttributeDefinitions",
                schema: "cmdb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsFacet = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationItems",
                schema: "cmdb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalCiNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ApplicationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DatabaseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EntityPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApplicationDomain = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PlatformType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PlatformName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BudgetCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Rto = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Rpo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsPlaceholder = table.Column<bool>(type: "bit", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    SourceUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Locked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportRuns",
                schema: "cmdb",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InsertedCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedCount = table.Column<int>(type: "int", nullable: false),
                    RelationshipCount = table.Column<int>(type: "int", nullable: false),
                    AttributeCount = table.Column<int>(type: "int", nullable: false),
                    SupportAssignmentCount = table.Column<int>(type: "int", nullable: false),
                    RejectedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelationshipTypes",
                schema: "cmdb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Family = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsDirectional = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationshipTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technologies",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Family = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technologies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttributeValues",
                schema: "cmdb",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigurationItemId = table.Column<int>(type: "int", nullable: false),
                    AttributeDefinitionId = table.Column<int>(type: "int", nullable: false),
                    RawValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StringValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NumberValue = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeValues_AttributeDefinitions_AttributeDefinitionId",
                        column: x => x.AttributeDefinitionId,
                        principalSchema: "cmdb",
                        principalTable: "AttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttributeValues_ConfigurationItems_ConfigurationItemId",
                        column: x => x.ConfigurationItemId,
                        principalSchema: "cmdb",
                        principalTable: "ConfigurationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportAssignments",
                schema: "cmdb",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigurationItemId = table.Column<int>(type: "int", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ManagerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ManagerEntity = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ManagerTeam = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportAssignments_ConfigurationItems_ConfigurationItemId",
                        column: x => x.ConfigurationItemId,
                        principalSchema: "cmdb",
                        principalTable: "ConfigurationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relationships",
                schema: "cmdb",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceCiId = table.Column<int>(type: "int", nullable: false),
                    TargetCiId = table.Column<int>(type: "int", nullable: false),
                    RelationshipTypeId = table.Column<int>(type: "int", nullable: false),
                    IsBlocking = table.Column<bool>(type: "bit", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relationships_ConfigurationItems_SourceCiId",
                        column: x => x.SourceCiId,
                        principalSchema: "cmdb",
                        principalTable: "ConfigurationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relationships_ConfigurationItems_TargetCiId",
                        column: x => x.TargetCiId,
                        principalSchema: "cmdb",
                        principalTable: "ConfigurationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relationships_RelationshipTypes_RelationshipTypeId",
                        column: x => x.RelationshipTypeId,
                        principalSchema: "cmdb",
                        principalTable: "RelationshipTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExchangePatterns",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Family = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InteractionMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TriggerMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DefaultTechnologyId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    Locked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangePatterns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangePatterns_Technologies_DefaultTechnologyId",
                        column: x => x.DefaultTechnologyId,
                        principalSchema: "integration",
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationFlows",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SourceCiId = table.Column<int>(type: "int", nullable: false),
                    TargetCiId = table.Column<int>(type: "int", nullable: false),
                    BrokerCiId = table.Column<int>(type: "int", nullable: true),
                    ExchangePatternId = table.Column<int>(type: "int", nullable: false),
                    TechnologyId = table.Column<int>(type: "int", nullable: true),
                    CmdbRelationshipId = table.Column<long>(type: "bigint", nullable: true),
                    FlowGroupCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Criticality = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TransportProtocol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ChannelName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EndpointReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AverageMessagesPerDay = table.Column<long>(type: "bigint", nullable: true),
                    PeakMessagesPerMinute = table.Column<int>(type: "int", nullable: true),
                    AveragePayloadKb = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    ExpectedLatencyMs = table.Column<int>(type: "int", nullable: true),
                    DataClassification = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ContainsPersonalData = table.Column<bool>(type: "bit", nullable: false),
                    IsEncryptedInTransit = table.Column<bool>(type: "bit", nullable: true),
                    ValidFromDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidToDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Locked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationFlows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationFlows_ConfigurationItems_BrokerCiId",
                        column: x => x.BrokerCiId,
                        principalSchema: "cmdb",
                        principalTable: "ConfigurationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationFlows_ConfigurationItems_SourceCiId",
                        column: x => x.SourceCiId,
                        principalSchema: "cmdb",
                        principalTable: "ConfigurationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationFlows_ConfigurationItems_TargetCiId",
                        column: x => x.TargetCiId,
                        principalSchema: "cmdb",
                        principalTable: "ConfigurationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationFlows_ExchangePatterns_ExchangePatternId",
                        column: x => x.ExchangePatternId,
                        principalSchema: "integration",
                        principalTable: "ExchangePatterns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationFlows_Relationships_CmdbRelationshipId",
                        column: x => x.CmdbRelationshipId,
                        principalSchema: "cmdb",
                        principalTable: "Relationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IntegrationFlows_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalSchema: "integration",
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FlowRouteSteps",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntegrationFlowId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepKind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConfigurationItemId = table.Column<int>(type: "int", nullable: true),
                    TechnologyId = table.Column<int>(type: "int", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowRouteSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowRouteSteps_ConfigurationItems_ConfigurationItemId",
                        column: x => x.ConfigurationItemId,
                        principalSchema: "cmdb",
                        principalTable: "ConfigurationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlowRouteSteps_IntegrationFlows_IntegrationFlowId",
                        column: x => x.IntegrationFlowId,
                        principalSchema: "integration",
                        principalTable: "IntegrationFlows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlowRouteSteps_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalSchema: "integration",
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                schema: "integration",
                table: "ExchangePatterns",
                columns: new[] { "Id", "Code", "CreatedDate", "DefaultTechnologyId", "Description", "Family", "InteractionMode", "IsActive", "IsSystem", "Locked", "Name", "TriggerMode", "UpdatedDate" },
                values: new object[,]
                {
                    { 5, "ELT_BATCH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Extraction, chargement puis transformation planifiés.", "ELT", "Asynchronous", true, true, false, "ELT batch", "Scheduled", null },
                    { 7, "CDC_STREAM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Capture continue des changements de données.", "CDC", "Asynchronous", true, true, false, "CDC continu", "Continuous", null }
                });

            migrationBuilder.InsertData(
                schema: "integration",
                table: "Technologies",
                columns: new[] { "Id", "Code", "Family", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "REST", "API", true, "API REST" },
                    { 2, "KAFKA", "Messaging", true, "Apache Kafka" },
                    { 3, "SFTP", "FileTransfer", true, "SFTP" },
                    { 4, "SSIS", "ETL", true, "SQL Server Integration Services" },
                    { 5, "TALEND", "ETL", true, "Talend" },
                    { 6, "RABBITMQ", "Messaging", true, "RabbitMQ" },
                    { 7, "JDBC", "Database", true, "JDBC / accès base" }
                });

            migrationBuilder.InsertData(
                schema: "integration",
                table: "ExchangePatterns",
                columns: new[] { "Id", "Code", "CreatedDate", "DefaultTechnologyId", "Description", "Family", "InteractionMode", "IsActive", "IsSystem", "Locked", "Name", "TriggerMode", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "API_SYNC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Requête/réponse synchrone.", "API", "Synchronous", true, true, false, "API synchrone", "OnDemand", null },
                    { 2, "API_ASYNC_CALLBACK", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Appel asynchrone avec notification de résultat.", "API", "Asynchronous", true, true, false, "API asynchrone avec callback", "OnDemand", null },
                    { 3, "KAFKA_EVENT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Publication et consommation événementielles.", "Messaging", "Asynchronous", true, true, false, "Événement Kafka", "EventDriven", null },
                    { 4, "ETL_BATCH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Extraction, transformation et chargement planifiés.", "ETL", "Asynchronous", true, true, false, "ETL batch", "Scheduled", null },
                    { 6, "SFTP_BATCH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "Dépôt ou collecte planifiée de fichiers.", "FileTransfer", "Asynchronous", true, true, false, "Fichier SFTP", "Scheduled", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_Code",
                schema: "cmdb",
                table: "AttributeDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttributeValues_AttributeDefinitionId",
                schema: "cmdb",
                table: "AttributeValues",
                column: "AttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeValues_ConfigurationItemId_AttributeDefinitionId",
                schema: "cmdb",
                table: "AttributeValues",
                columns: new[] { "ConfigurationItemId", "AttributeDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationItems_ExternalCiNumber",
                schema: "cmdb",
                table: "ConfigurationItems",
                column: "ExternalCiNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationItems_Model_Category_Status_IsCurrent",
                schema: "cmdb",
                table: "ConfigurationItems",
                columns: new[] { "Model", "Category", "Status", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationItems_Name",
                schema: "cmdb",
                table: "ConfigurationItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangePatterns_Code",
                schema: "integration",
                table: "ExchangePatterns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangePatterns_DefaultTechnologyId",
                schema: "integration",
                table: "ExchangePatterns",
                column: "DefaultTechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowRouteSteps_ConfigurationItemId",
                schema: "integration",
                table: "FlowRouteSteps",
                column: "ConfigurationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowRouteSteps_IntegrationFlowId_StepOrder",
                schema: "integration",
                table: "FlowRouteSteps",
                columns: new[] { "IntegrationFlowId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowRouteSteps_TechnologyId",
                schema: "integration",
                table: "FlowRouteSteps",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationFlows_BrokerCiId",
                schema: "integration",
                table: "IntegrationFlows",
                column: "BrokerCiId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationFlows_CmdbRelationshipId",
                schema: "integration",
                table: "IntegrationFlows",
                column: "CmdbRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationFlows_Code",
                schema: "integration",
                table: "IntegrationFlows",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationFlows_ExchangePatternId",
                schema: "integration",
                table: "IntegrationFlows",
                column: "ExchangePatternId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationFlows_SourceCiId_Status",
                schema: "integration",
                table: "IntegrationFlows",
                columns: new[] { "SourceCiId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationFlows_TargetCiId_Status",
                schema: "integration",
                table: "IntegrationFlows",
                columns: new[] { "TargetCiId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationFlows_TechnologyId",
                schema: "integration",
                table: "IntegrationFlows",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_RelationshipTypeId",
                schema: "cmdb",
                table: "Relationships",
                column: "RelationshipTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_SourceCiId_IsCurrent",
                schema: "cmdb",
                table: "Relationships",
                columns: new[] { "SourceCiId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_SourceCiId_TargetCiId_RelationshipTypeId_SourceSystem",
                schema: "cmdb",
                table: "Relationships",
                columns: new[] { "SourceCiId", "TargetCiId", "RelationshipTypeId", "SourceSystem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_TargetCiId_IsCurrent",
                schema: "cmdb",
                table: "Relationships",
                columns: new[] { "TargetCiId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipTypes_Code",
                schema: "cmdb",
                table: "RelationshipTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportAssignments_ConfigurationItemId_GroupName_RoleName",
                schema: "cmdb",
                table: "SupportAssignments",
                columns: new[] { "ConfigurationItemId", "GroupName", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_Code",
                schema: "integration",
                table: "Technologies",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttributeValues",
                schema: "cmdb");

            migrationBuilder.DropTable(
                name: "FlowRouteSteps",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "ImportRuns",
                schema: "cmdb");

            migrationBuilder.DropTable(
                name: "SupportAssignments",
                schema: "cmdb");

            migrationBuilder.DropTable(
                name: "AttributeDefinitions",
                schema: "cmdb");

            migrationBuilder.DropTable(
                name: "IntegrationFlows",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "ExchangePatterns",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "Relationships",
                schema: "cmdb");

            migrationBuilder.DropTable(
                name: "Technologies",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "ConfigurationItems",
                schema: "cmdb");

            migrationBuilder.DropTable(
                name: "RelationshipTypes",
                schema: "cmdb");
        }
    }
}
