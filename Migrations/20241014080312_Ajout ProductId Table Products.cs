using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutProductIdTableProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ProductId",
                table: "Contracts",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Products_ProductId",
                table: "Contracts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Products_ProductId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_ProductId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Contracts");
        }
    }
}
