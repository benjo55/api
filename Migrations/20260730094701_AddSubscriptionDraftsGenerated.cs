using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionDraftsGenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionDrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    CurrentStep = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    HighestCompletedStep = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProjectDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SituationDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvestorProfileDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendationDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvestmentDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProtectionDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StepStatusesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionDrafts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionDrafts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionDraftAuditEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionDraftId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StepKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PreviousStateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewStateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RulesVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionDraftAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionDraftAuditEvents_SubscriptionDrafts_SubscriptionDraftId",
                        column: x => x.SubscriptionDraftId,
                        principalTable: "SubscriptionDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionDraftAuditEvents_Draft_Date",
                table: "SubscriptionDraftAuditEvents",
                columns: new[] { "SubscriptionDraftId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionDrafts_ProductId",
                table: "SubscriptionDrafts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionDrafts_User_Status_UpdatedAt",
                table: "SubscriptionDrafts",
                columns: new[] { "UserId", "Status", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionDraftAuditEvents");

            migrationBuilder.DropTable(
                name: "SubscriptionDrafts");
        }
    }
}
