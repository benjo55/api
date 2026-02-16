using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompartmentsToContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompartmentId",
                table: "FinancialSupportAllocations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Compartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ManagementMode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compartments_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialSupportAllocations_CompartmentId",
                table: "FinancialSupportAllocations",
                column: "CompartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Compartments_ContractId",
                table: "Compartments",
                column: "ContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialSupportAllocations_Compartments_CompartmentId",
                table: "FinancialSupportAllocations",
                column: "CompartmentId",
                principalTable: "Compartments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialSupportAllocations_Compartments_CompartmentId",
                table: "FinancialSupportAllocations");

            migrationBuilder.DropTable(
                name: "Compartments");

            migrationBuilder.DropIndex(
                name: "IX_FinancialSupportAllocations_CompartmentId",
                table: "FinancialSupportAllocations");

            migrationBuilder.DropColumn(
                name: "CompartmentId",
                table: "FinancialSupportAllocations");
        }
    }
}
