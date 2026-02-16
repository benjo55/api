using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimatedQuantitiesOSA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedNav",
                table: "OperationSupportAllocations",
                type: "decimal(20,7)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedShares",
                table: "OperationSupportAllocations",
                type: "decimal(20,7)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedNav",
                table: "OperationSupportAllocations");

            migrationBuilder.DropColumn(
                name: "EstimatedShares",
                table: "OperationSupportAllocations");
        }
    }
}
