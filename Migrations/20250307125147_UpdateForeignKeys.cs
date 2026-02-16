using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BeneficiaryClauses_Contracts_ContractId",
                table: "BeneficiaryClauses");

            migrationBuilder.AlterColumn<int>(
                name: "ContractId",
                table: "BeneficiaryClauses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_BeneficiaryClauses_Contracts_ContractId",
                table: "BeneficiaryClauses",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BeneficiaryClauses_Contracts_ContractId",
                table: "BeneficiaryClauses");

            migrationBuilder.AlterColumn<int>(
                name: "ContractId",
                table: "BeneficiaryClauses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BeneficiaryClauses_Contracts_ContractId",
                table: "BeneficiaryClauses",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
