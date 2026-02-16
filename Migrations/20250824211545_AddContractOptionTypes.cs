using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddContractOptionTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Label",
                table: "ContractOptions");

            migrationBuilder.AddColumn<int>(
                name: "ContractOptionTypeId",
                table: "ContractOptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CustomParameters",
                table: "ContractOptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                table: "Compartments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ContractOptionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mechanism = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultCost = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractOptionTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractOptions_ContractOptionTypeId",
                table: "ContractOptions",
                column: "ContractOptionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractOptionTypes_Code",
                table: "ContractOptionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractOptions_ContractOptionTypes_ContractOptionTypeId",
                table: "ContractOptions",
                column: "ContractOptionTypeId",
                principalTable: "ContractOptionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractOptions_ContractOptionTypes_ContractOptionTypeId",
                table: "ContractOptions");

            migrationBuilder.DropTable(
                name: "ContractOptionTypes");

            migrationBuilder.DropIndex(
                name: "IX_ContractOptions_ContractOptionTypeId",
                table: "ContractOptions");

            migrationBuilder.DropColumn(
                name: "ContractOptionTypeId",
                table: "ContractOptions");

            migrationBuilder.DropColumn(
                name: "CustomParameters",
                table: "ContractOptions");

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "ContractOptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                table: "Compartments",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }
    }
}
