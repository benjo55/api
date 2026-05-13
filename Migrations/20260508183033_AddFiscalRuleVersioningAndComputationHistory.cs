using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalRuleVersioningAndComputationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxRuleVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRuleVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxComputations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxProfileId = table.Column<int>(type: "int", nullable: false),
                    TaxRuleVersionId = table.Column<int>(type: "int", nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    GrossWithdrawal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GainAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxComputations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxComputations_TaxProfiles_TaxProfileId",
                        column: x => x.TaxProfileId,
                        principalTable: "TaxProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxComputations_TaxRuleVersions_TaxRuleVersionId",
                        column: x => x.TaxRuleVersionId,
                        principalTable: "TaxRuleVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxComputationId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalEvents_TaxComputations_TaxComputationId",
                        column: x => x.TaxComputationId,
                        principalTable: "TaxComputations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TaxRuleVersions",
                columns: new[] { "Id", "Code", "CreatedDate", "EffectiveFrom", "EffectiveTo", "IsActive", "Label", "Notes" },
                values: new object[] { 1, "FR-ASSURANCE-2024", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Référentiel fiscal France Assurance 2024+", "Version initiale du moteur fiscal (PFU, seuil 8 ans AV, art. 990I/757B, PER)." });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalEvents_TaxComputationId",
                table: "FiscalEvents",
                column: "TaxComputationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxComputations_CreatedDate",
                table: "TaxComputations",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_TaxComputations_TaxProfileId",
                table: "TaxComputations",
                column: "TaxProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxComputations_TaxRuleVersionId",
                table: "TaxComputations",
                column: "TaxRuleVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRuleVersions_IsActive_EffectiveFrom",
                table: "TaxRuleVersions",
                columns: new[] { "IsActive", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiscalEvents");

            migrationBuilder.DropTable(
                name: "TaxComputations");

            migrationBuilder.DropTable(
                name: "TaxRuleVersions");
        }
    }
}
