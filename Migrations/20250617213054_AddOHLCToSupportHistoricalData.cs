using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddOHLCToSupportHistoricalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Close",
                table: "SupportHistoricalData",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "High",
                table: "SupportHistoricalData",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Low",
                table: "SupportHistoricalData",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Open",
                table: "SupportHistoricalData",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Volume",
                table: "SupportHistoricalData",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Close",
                table: "SupportHistoricalData");

            migrationBuilder.DropColumn(
                name: "High",
                table: "SupportHistoricalData");

            migrationBuilder.DropColumn(
                name: "Low",
                table: "SupportHistoricalData");

            migrationBuilder.DropColumn(
                name: "Open",
                table: "SupportHistoricalData");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "SupportHistoricalData");
        }
    }
}
