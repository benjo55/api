using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndex_ContractSupportHolding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractSupportHoldings_FinancialSupports_SupportId",
                table: "ContractSupportHoldings");

            migrationBuilder.DropIndex(
                name: "IX_ContractSupportHoldings_ContractId",
                table: "ContractSupportHoldings");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalShares",
                table: "ContractSupportHoldings",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,7)",
                oldPrecision: 18,
                oldScale: 7);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalInvested",
                table: "ContractSupportHoldings",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,5)",
                oldPrecision: 18,
                oldScale: 5);

            migrationBuilder.AlterColumn<decimal>(
                name: "Pru",
                table: "ContractSupportHoldings",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,7)",
                oldPrecision: 18,
                oldScale: 7);

            migrationBuilder.CreateIndex(
                name: "IX_ContractSupportHoldings_ContractId_SupportId",
                table: "ContractSupportHoldings",
                columns: new[] { "ContractId", "SupportId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractSupportHoldings_FinancialSupports_SupportId",
                table: "ContractSupportHoldings",
                column: "SupportId",
                principalTable: "FinancialSupports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractSupportHoldings_FinancialSupports_SupportId",
                table: "ContractSupportHoldings");

            migrationBuilder.DropIndex(
                name: "IX_ContractSupportHoldings_ContractId_SupportId",
                table: "ContractSupportHoldings");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalShares",
                table: "ContractSupportHoldings",
                type: "decimal(18,7)",
                precision: 18,
                scale: 7,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalInvested",
                table: "ContractSupportHoldings",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Pru",
                table: "ContractSupportHoldings",
                type: "decimal(18,7)",
                precision: 18,
                scale: 7,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.CreateIndex(
                name: "IX_ContractSupportHoldings_ContractId",
                table: "ContractSupportHoldings",
                column: "ContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractSupportHoldings_FinancialSupports_SupportId",
                table: "ContractSupportHoldings",
                column: "SupportId",
                principalTable: "FinancialSupports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
