using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterConstraintUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialSupportAllocations_Compartments_CompartmentId",
                table: "FinancialSupportAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialSupportAllocations_FinancialSupports_SupportId",
                table: "FinancialSupportAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_OperationSupportAllocations_Compartments_CompartmentId",
                table: "OperationSupportAllocations");

            migrationBuilder.DropIndex(
                name: "IX_OperationSupportAllocations_OperationId",
                table: "OperationSupportAllocations");

            migrationBuilder.DropIndex(
                name: "IX_FinancialSupportAllocations_ContractId",
                table: "FinancialSupportAllocations");

            migrationBuilder.AlterColumn<int>(
                name: "CompartmentId",
                table: "OperationSupportAllocations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompartmentId",
                table: "FinancialSupportAllocations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_OSA_Operation_Support",
                table: "OperationSupportAllocations",
                columns: new[] { "OperationId", "SupportId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_FSA_Contract_Compartment_Support",
                table: "FinancialSupportAllocations",
                columns: new[] { "ContractId", "CompartmentId", "SupportId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialSupportAllocations_Compartments_CompartmentId",
                table: "FinancialSupportAllocations",
                column: "CompartmentId",
                principalTable: "Compartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialSupportAllocations_FinancialSupports_SupportId",
                table: "FinancialSupportAllocations",
                column: "SupportId",
                principalTable: "FinancialSupports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OperationSupportAllocations_Compartments_CompartmentId",
                table: "OperationSupportAllocations",
                column: "CompartmentId",
                principalTable: "Compartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialSupportAllocations_Compartments_CompartmentId",
                table: "FinancialSupportAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialSupportAllocations_FinancialSupports_SupportId",
                table: "FinancialSupportAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_OperationSupportAllocations_Compartments_CompartmentId",
                table: "OperationSupportAllocations");

            migrationBuilder.DropIndex(
                name: "UX_OSA_Operation_Support",
                table: "OperationSupportAllocations");

            migrationBuilder.DropIndex(
                name: "UX_FSA_Contract_Compartment_Support",
                table: "FinancialSupportAllocations");

            migrationBuilder.AlterColumn<int>(
                name: "CompartmentId",
                table: "OperationSupportAllocations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CompartmentId",
                table: "FinancialSupportAllocations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_OperationSupportAllocations_OperationId",
                table: "OperationSupportAllocations",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialSupportAllocations_ContractId",
                table: "FinancialSupportAllocations",
                column: "ContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialSupportAllocations_Compartments_CompartmentId",
                table: "FinancialSupportAllocations",
                column: "CompartmentId",
                principalTable: "Compartments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialSupportAllocations_FinancialSupports_SupportId",
                table: "FinancialSupportAllocations",
                column: "SupportId",
                principalTable: "FinancialSupports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OperationSupportAllocations_Compartments_CompartmentId",
                table: "OperationSupportAllocations",
                column: "CompartmentId",
                principalTable: "Compartments",
                principalColumn: "Id");
        }
    }
}
