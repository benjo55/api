using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContractModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BeneficiaryClauses_ContractId",
                table: "BeneficiaryClauses");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryClauses_ContractId",
                table: "BeneficiaryClauses",
                column: "ContractId",
                unique: true,
                filter: "[ContractId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BeneficiaryClauses_ContractId",
                table: "BeneficiaryClauses");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryClauses_ContractId",
                table: "BeneficiaryClauses",
                column: "ContractId");
        }
    }
}
