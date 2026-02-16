using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddNavToOperationSupportAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NavAtOperation",
                table: "OperationSupportAllocations",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NavDateAtOperation",
                table: "OperationSupportAllocations",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NavAtOperation",
                table: "OperationSupportAllocations");

            migrationBuilder.DropColumn(
                name: "NavDateAtOperation",
                table: "OperationSupportAllocations");
        }
    }
}
