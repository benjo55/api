using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddValuationFieldsToCompartmentsAndAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperationSupportAllocations_FinancialSupports_SupportId",
                table: "OperationSupportAllocations");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentAmount",
                table: "FinancialSupportAllocations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentShares",
                table: "FinancialSupportAllocations",
                type: "decimal(18,7)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentValue",
                table: "Compartments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_OperationSupportAllocations_FinancialSupports_SupportId",
                table: "OperationSupportAllocations",
                column: "SupportId",
                principalTable: "FinancialSupports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperationSupportAllocations_FinancialSupports_SupportId",
                table: "OperationSupportAllocations");

            migrationBuilder.DropColumn(
                name: "CurrentAmount",
                table: "FinancialSupportAllocations");

            migrationBuilder.DropColumn(
                name: "CurrentShares",
                table: "FinancialSupportAllocations");

            migrationBuilder.DropColumn(
                name: "CurrentValue",
                table: "Compartments");

            migrationBuilder.AddForeignKey(
                name: "FK_OperationSupportAllocations_FinancialSupports_SupportId",
                table: "OperationSupportAllocations",
                column: "SupportId",
                principalTable: "FinancialSupports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
