using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompartmentIdToOperationSupportAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompartmentId",
                table: "OperationSupportAllocations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationSupportAllocations_CompartmentId",
                table: "OperationSupportAllocations",
                column: "CompartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_OperationSupportAllocations_Compartments_CompartmentId",
                table: "OperationSupportAllocations",
                column: "CompartmentId",
                principalTable: "Compartments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperationSupportAllocations_Compartments_CompartmentId",
                table: "OperationSupportAllocations");

            migrationBuilder.DropIndex(
                name: "IX_OperationSupportAllocations_CompartmentId",
                table: "OperationSupportAllocations");

            migrationBuilder.DropColumn(
                name: "CompartmentId",
                table: "OperationSupportAllocations");
        }
    }
}
