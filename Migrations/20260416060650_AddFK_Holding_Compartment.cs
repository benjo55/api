using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddFK_Holding_Compartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.CreateIndex(
                name: "IX_ContractSupportHoldings_CompartmentId",
                table: "ContractSupportHoldings",
                column: "CompartmentId");

            migrationBuilder.CreateIndex(
                name: "UX_Holding_Contract_Compartment_Support",
                table: "ContractSupportHoldings",
                columns: new[] { "ContractId", "CompartmentId", "SupportId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractSupportHoldings_Compartments_CompartmentId",
                table: "ContractSupportHoldings",
                column: "CompartmentId",
                principalTable: "Compartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractSupportHoldings_Compartments_CompartmentId",
                table: "ContractSupportHoldings");

            migrationBuilder.DropIndex(
                name: "UX_OSA_Operation_Support_Compartment",
                table: "OperationSupportAllocations");

            migrationBuilder.DropIndex(
                name: "IX_ContractSupportHoldings_CompartmentId",
                table: "ContractSupportHoldings");

            migrationBuilder.DropIndex(
                name: "UX_Holding_Contract_Compartment_Support",
                table: "ContractSupportHoldings");

            migrationBuilder.DropColumn(
                name: "CompartmentId",
                table: "ContractSupportHoldings");

            migrationBuilder.CreateIndex(
                name: "UX_OSA_Operation_Support",
                table: "OperationSupportAllocations",
                columns: new[] { "OperationId", "SupportId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractSupportHoldings_ContractId_SupportId",
                table: "ContractSupportHoldings",
                columns: new[] { "ContractId", "SupportId" },
                unique: true);
        }
    }
}
