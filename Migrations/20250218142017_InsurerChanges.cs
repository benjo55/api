using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class InsurerChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Website",
                table: "Insurers",
                newName: "WebSite");

            migrationBuilder.RenameColumn(
                name: "Headquarters",
                table: "Insurers",
                newName: "HeadQuarters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WebSite",
                table: "Insurers",
                newName: "Website");

            migrationBuilder.RenameColumn(
                name: "HeadQuarters",
                table: "Insurers",
                newName: "Headquarters");
        }
    }
}
