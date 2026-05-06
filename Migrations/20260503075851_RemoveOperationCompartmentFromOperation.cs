using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOperationCompartmentFromOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Operations_Compartments_CompartmentId",
                table: "Operations");

            migrationBuilder.DropIndex(
                name: "IX_Operations_CompartmentId",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "CompartmentId",
                table: "Operations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompartmentId",
                table: "Operations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Operations_CompartmentId",
                table: "Operations",
                column: "CompartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Operations_Compartments_CompartmentId",
                table: "Operations",
                column: "CompartmentId",
                principalTable: "Compartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
