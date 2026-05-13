using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddContractManagementFeeAccruals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractManagementFeeAccruals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    SupportId = table.Column<int>(type: "int", nullable: false),
                    CompartmentId = table.Column<int>(type: "int", nullable: false),
                    AccruedAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    LastAccruedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPostedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractManagementFeeAccruals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractManagementFeeAccruals_Compartments_CompartmentId",
                        column: x => x.CompartmentId,
                        principalTable: "Compartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractManagementFeeAccruals_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractManagementFeeAccruals_FinancialSupports_SupportId",
                        column: x => x.SupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractManagementFeeAccruals_CompartmentId",
                table: "ContractManagementFeeAccruals",
                column: "CompartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractManagementFeeAccruals_SupportId",
                table: "ContractManagementFeeAccruals",
                column: "SupportId");

            migrationBuilder.CreateIndex(
                name: "UX_ContractManagementFeeAccrual_Contract_Support_Compartment",
                table: "ContractManagementFeeAccruals",
                columns: new[] { "ContractId", "SupportId", "CompartmentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractManagementFeeAccruals");
        }
    }
}
