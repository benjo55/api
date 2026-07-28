using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddCartographyApplicationDocumentation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationCriticality",
                schema: "cmdb",
                table: "ApplicationProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetailedDescription",
                schema: "cmdb",
                table: "ApplicationProfiles",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneralTechnicalFramework",
                schema: "cmdb",
                table: "ApplicationProfiles",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainFunctionalProcesses",
                schema: "cmdb",
                table: "ApplicationProfiles",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverallArchitecture",
                schema: "cmdb",
                table: "ApplicationProfiles",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                schema: "cmdb",
                table: "ApplicationProfiles",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationCriticality",
                schema: "cmdb",
                table: "ApplicationProfiles");

            migrationBuilder.DropColumn(
                name: "DetailedDescription",
                schema: "cmdb",
                table: "ApplicationProfiles");

            migrationBuilder.DropColumn(
                name: "GeneralTechnicalFramework",
                schema: "cmdb",
                table: "ApplicationProfiles");

            migrationBuilder.DropColumn(
                name: "MainFunctionalProcesses",
                schema: "cmdb",
                table: "ApplicationProfiles");

            migrationBuilder.DropColumn(
                name: "OverallArchitecture",
                schema: "cmdb",
                table: "ApplicationProfiles");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                schema: "cmdb",
                table: "ApplicationProfiles");
        }
    }
}
