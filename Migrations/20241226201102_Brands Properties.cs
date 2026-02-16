using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class BrandsProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BrandLabel",
                table: "Brands",
                newName: "BrandName");

            migrationBuilder.AddColumn<string>(
                name: "BrandCode",
                table: "Brands",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrandCode",
                table: "Brands");

            migrationBuilder.RenameColumn(
                name: "BrandName",
                table: "Brands",
                newName: "BrandLabel");
        }
    }
}
