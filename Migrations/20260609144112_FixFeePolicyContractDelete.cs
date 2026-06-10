using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class FixFeePolicyContractDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeePolicies_Contracts_ContractId",
                table: "FeePolicies");

            migrationBuilder.AddForeignKey(
                name: "FK_FeePolicies_Contracts_ContractId",
                table: "FeePolicies",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeePolicies_Contracts_ContractId",
                table: "FeePolicies");

            migrationBuilder.AddForeignKey(
                name: "FK_FeePolicies_Contracts_ContractId",
                table: "FeePolicies",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
