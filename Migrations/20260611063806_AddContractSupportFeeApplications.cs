using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddContractSupportFeeApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceOperationId",
                table: "Operations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContractSupportFeeApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    FeeOperationId = table.Column<int>(type: "int", nullable: false),
                    SourceOperationId = table.Column<int>(type: "int", nullable: true),
                    FeeNature = table.Column<int>(type: "int", nullable: false),
                    CompartmentId = table.Column<int>(type: "int", nullable: false),
                    SupportId = table.Column<int>(type: "int", nullable: false),
                    ApplyOn = table.Column<int>(type: "int", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    FeeShares = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    NavUsed = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    NavDateUsed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PolicySource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PolicyId = table.Column<int>(type: "int", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractSupportFeeApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractSupportFeeApplications_Compartments_CompartmentId",
                        column: x => x.CompartmentId,
                        principalTable: "Compartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractSupportFeeApplications_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractSupportFeeApplications_FinancialSupports_SupportId",
                        column: x => x.SupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractSupportFeeApplications_Operations_FeeOperationId",
                        column: x => x.FeeOperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractSupportFeeApplications_Operations_SourceOperationId",
                        column: x => x.SourceOperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Operations_SourceOperationId",
                table: "Operations",
                column: "SourceOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSupportFeeApplications_CompartmentId",
                table: "ContractSupportFeeApplications",
                column: "CompartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSupportFeeApplications_Contract_Compartment_Support_Nature",
                table: "ContractSupportFeeApplications",
                columns: new[] { "ContractId", "CompartmentId", "SupportId", "FeeNature" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractSupportFeeApplications_Contract_Date",
                table: "ContractSupportFeeApplications",
                columns: new[] { "ContractId", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractSupportFeeApplications_FeeOperation",
                table: "ContractSupportFeeApplications",
                column: "FeeOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSupportFeeApplications_SourceOperationId",
                table: "ContractSupportFeeApplications",
                column: "SourceOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractSupportFeeApplications_SupportId",
                table: "ContractSupportFeeApplications",
                column: "SupportId");

            migrationBuilder.AddForeignKey(
                name: "FK_Operations_Operations_SourceOperationId",
                table: "Operations",
                column: "SourceOperationId",
                principalTable: "Operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Operations_Operations_SourceOperationId",
                table: "Operations");

            migrationBuilder.DropTable(
                name: "ContractSupportFeeApplications");

            migrationBuilder.DropIndex(
                name: "IX_Operations_SourceOperationId",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "SourceOperationId",
                table: "Operations");
        }
    }
}
