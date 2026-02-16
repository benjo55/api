using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class ProductChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InsurerId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_InsurerId",
                table: "Products",
                column: "InsurerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Insurers_InsurerId",
                table: "Products",
                column: "InsurerId",
                principalTable: "Insurers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Insurers_InsurerId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_InsurerId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "InsurerId",
                table: "Products");
        }
    }
}
