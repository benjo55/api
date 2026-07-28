using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class LegalDocumentProductAssignmentsAndVariables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClauseDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClauseDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentLayoutTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PageFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MarginTopMm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MarginRightMm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MarginBottomMm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MarginLeftMm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Css = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeaderHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FooterHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateVersion = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentLayoutTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClauseRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClauseDefinitionId = table.Column<int>(type: "int", nullable: false),
                    MajorVersion = table.Column<int>(type: "int", nullable: false),
                    MinorVersion = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EditorJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlainText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClauseRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClauseRevisions_ClauseDefinitions_ClauseDefinitionId",
                        column: x => x.ClauseDefinitionId,
                        principalTable: "ClauseDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractDocumentInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    TemplateRevisionId = table.Column<int>(type: "int", nullable: false),
                    ApplicableGeneralTermsRevisionId = table.Column<int>(type: "int", nullable: true),
                    DataSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PdfArtifactId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractDocumentInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractDocumentInstances_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentArtifacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LegalDocumentRevisionId = table.Column<int>(type: "int", nullable: true),
                    ContractDocumentInstanceId = table.Column<int>(type: "int", nullable: true),
                    StorageKey = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CacheKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentArtifacts_ContractDocumentInstances_ContractDocumentInstanceId",
                        column: x => x.ContractDocumentInstanceId,
                        principalTable: "ContractDocumentInstances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DocumentAuditEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalDocumentDefinitionId = table.Column<int>(type: "int", nullable: true),
                    LegalDocumentRevisionId = table.Column<int>(type: "int", nullable: true),
                    LegalDocumentNodeId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    DetailJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalDocumentDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CurrentDraftRevisionId = table.Column<int>(type: "int", nullable: true),
                    CurrentPublishedRevisionId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalDocumentRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalDocumentDefinitionId = table.Column<int>(type: "int", nullable: false),
                    BasedOnRevisionId = table.Column<int>(type: "int", nullable: true),
                    MajorVersion = table.Column<int>(type: "int", nullable: false),
                    MinorVersion = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ChangeSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ValidationComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DocumentLayoutTemplateId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalDocumentRevisions_DocumentLayoutTemplates_DocumentLayoutTemplateId",
                        column: x => x.DocumentLayoutTemplateId,
                        principalTable: "DocumentLayoutTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegalDocumentRevisions_LegalDocumentDefinitions_LegalDocumentDefinitionId",
                        column: x => x.LegalDocumentDefinitionId,
                        principalTable: "LegalDocumentDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LegalDocumentRevisions_LegalDocumentRevisions_BasedOnRevisionId",
                        column: x => x.BasedOnRevisionId,
                        principalTable: "LegalDocumentRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LegalDocumentNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalDocumentRevisionId = table.Column<int>(type: "int", nullable: false),
                    ParentNodeId = table.Column<int>(type: "int", nullable: true),
                    StableKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EditorJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlainText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IncludeInTableOfContents = table.Column<bool>(type: "bit", nullable: false),
                    StartOnNewPage = table.Column<bool>(type: "bit", nullable: false),
                    KeepWithNext = table.Column<bool>(type: "bit", nullable: false),
                    NumberingStyle = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsConditional = table.Column<bool>(type: "bit", nullable: false),
                    DisplayConditionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceClauseRevisionId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalDocumentNodes_ClauseRevisions_SourceClauseRevisionId",
                        column: x => x.SourceClauseRevisionId,
                        principalTable: "ClauseRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegalDocumentNodes_LegalDocumentNodes_ParentNodeId",
                        column: x => x.ParentNodeId,
                        principalTable: "LegalDocumentNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegalDocumentNodes_LegalDocumentRevisions_LegalDocumentRevisionId",
                        column: x => x.LegalDocumentRevisionId,
                        principalTable: "LegalDocumentRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductDocumentAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    LegalDocumentRevisionId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDocumentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductDocumentAssignments_LegalDocumentRevisions_LegalDocumentRevisionId",
                        column: x => x.LegalDocumentRevisionId,
                        principalTable: "LegalDocumentRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductDocumentAssignments_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClauseDefinitions_Code",
                table: "ClauseDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClauseRevisions_ClauseDefinitionId_MajorVersion_MinorVersion",
                table: "ClauseRevisions",
                columns: new[] { "ClauseDefinitionId", "MajorVersion", "MinorVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractDocumentInstances_ApplicableGeneralTermsRevisionId",
                table: "ContractDocumentInstances",
                column: "ApplicableGeneralTermsRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractDocumentInstances_ContractId",
                table: "ContractDocumentInstances",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractDocumentInstances_PdfArtifactId",
                table: "ContractDocumentInstances",
                column: "PdfArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractDocumentInstances_TemplateRevisionId",
                table: "ContractDocumentInstances",
                column: "TemplateRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentArtifacts_CacheKey",
                table: "DocumentArtifacts",
                column: "CacheKey",
                filter: "[CacheKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentArtifacts_ContractDocumentInstanceId",
                table: "DocumentArtifacts",
                column: "ContractDocumentInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentArtifacts_LegalDocumentRevisionId",
                table: "DocumentArtifacts",
                column: "LegalDocumentRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentArtifacts_StorageKey",
                table: "DocumentArtifacts",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditEvents_LegalDocumentDefinitionId",
                table: "DocumentAuditEvents",
                column: "LegalDocumentDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditEvents_LegalDocumentNodeId",
                table: "DocumentAuditEvents",
                column: "LegalDocumentNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditEvents_LegalDocumentRevisionId_CreatedAt",
                table: "DocumentAuditEvents",
                columns: new[] { "LegalDocumentRevisionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLayoutTemplates_Code_TemplateVersion",
                table: "DocumentLayoutTemplates",
                columns: new[] { "Code", "TemplateVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentDefinitions_Code",
                table: "LegalDocumentDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentDefinitions_CurrentDraftRevisionId",
                table: "LegalDocumentDefinitions",
                column: "CurrentDraftRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentDefinitions_CurrentPublishedRevisionId",
                table: "LegalDocumentDefinitions",
                column: "CurrentPublishedRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentNodes_LegalDocumentRevisionId",
                table: "LegalDocumentNodes",
                column: "LegalDocumentRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentNodes_LegalDocumentRevisionId_StableKey",
                table: "LegalDocumentNodes",
                columns: new[] { "LegalDocumentRevisionId", "StableKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentNodes_ParentNodeId",
                table: "LegalDocumentNodes",
                column: "ParentNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentNodes_ParentNodeId_SortOrder",
                table: "LegalDocumentNodes",
                columns: new[] { "ParentNodeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentNodes_SourceClauseRevisionId",
                table: "LegalDocumentNodes",
                column: "SourceClauseRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentRevisions_BasedOnRevisionId",
                table: "LegalDocumentRevisions",
                column: "BasedOnRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentRevisions_DocumentLayoutTemplateId",
                table: "LegalDocumentRevisions",
                column: "DocumentLayoutTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentRevisions_LegalDocumentDefinitionId_MajorVersion_MinorVersion",
                table: "LegalDocumentRevisions",
                columns: new[] { "LegalDocumentDefinitionId", "MajorVersion", "MinorVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductDocumentAssignments_LegalDocumentRevisionId",
                table: "ProductDocumentAssignments",
                column: "LegalDocumentRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDocumentAssignments_ProductId_Role_ValidFrom_ValidTo",
                table: "ProductDocumentAssignments",
                columns: new[] { "ProductId", "Role", "ValidFrom", "ValidTo" });

            migrationBuilder.AddForeignKey(
                name: "FK_ContractDocumentInstances_DocumentArtifacts_PdfArtifactId",
                table: "ContractDocumentInstances",
                column: "PdfArtifactId",
                principalTable: "DocumentArtifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractDocumentInstances_LegalDocumentRevisions_ApplicableGeneralTermsRevisionId",
                table: "ContractDocumentInstances",
                column: "ApplicableGeneralTermsRevisionId",
                principalTable: "LegalDocumentRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractDocumentInstances_LegalDocumentRevisions_TemplateRevisionId",
                table: "ContractDocumentInstances",
                column: "TemplateRevisionId",
                principalTable: "LegalDocumentRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentArtifacts_LegalDocumentRevisions_LegalDocumentRevisionId",
                table: "DocumentArtifacts",
                column: "LegalDocumentRevisionId",
                principalTable: "LegalDocumentRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAuditEvents_LegalDocumentDefinitions_LegalDocumentDefinitionId",
                table: "DocumentAuditEvents",
                column: "LegalDocumentDefinitionId",
                principalTable: "LegalDocumentDefinitions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAuditEvents_LegalDocumentNodes_LegalDocumentNodeId",
                table: "DocumentAuditEvents",
                column: "LegalDocumentNodeId",
                principalTable: "LegalDocumentNodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAuditEvents_LegalDocumentRevisions_LegalDocumentRevisionId",
                table: "DocumentAuditEvents",
                column: "LegalDocumentRevisionId",
                principalTable: "LegalDocumentRevisions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LegalDocumentDefinitions_LegalDocumentRevisions_CurrentDraftRevisionId",
                table: "LegalDocumentDefinitions",
                column: "CurrentDraftRevisionId",
                principalTable: "LegalDocumentRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LegalDocumentDefinitions_LegalDocumentRevisions_CurrentPublishedRevisionId",
                table: "LegalDocumentDefinitions",
                column: "CurrentPublishedRevisionId",
                principalTable: "LegalDocumentRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractDocumentInstances_DocumentArtifacts_PdfArtifactId",
                table: "ContractDocumentInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_LegalDocumentDefinitions_LegalDocumentRevisions_CurrentDraftRevisionId",
                table: "LegalDocumentDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_LegalDocumentDefinitions_LegalDocumentRevisions_CurrentPublishedRevisionId",
                table: "LegalDocumentDefinitions");

            migrationBuilder.DropTable(
                name: "DocumentAuditEvents");

            migrationBuilder.DropTable(
                name: "ProductDocumentAssignments");

            migrationBuilder.DropTable(
                name: "LegalDocumentNodes");

            migrationBuilder.DropTable(
                name: "ClauseRevisions");

            migrationBuilder.DropTable(
                name: "ClauseDefinitions");

            migrationBuilder.DropTable(
                name: "DocumentArtifacts");

            migrationBuilder.DropTable(
                name: "ContractDocumentInstances");

            migrationBuilder.DropTable(
                name: "LegalDocumentRevisions");

            migrationBuilder.DropTable(
                name: "DocumentLayoutTemplates");

            migrationBuilder.DropTable(
                name: "LegalDocumentDefinitions");
        }
    }
}
