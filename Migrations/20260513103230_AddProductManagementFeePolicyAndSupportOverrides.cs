using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductManagementFeePolicyAndSupportOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContractManagementFeeOverrideEffectiveDate",
                table: "FinancialSupports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ContractManagementFeeOverrideEnabled",
                table: "FinancialSupports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractManagementFeeOverrideEndDate",
                table: "FinancialSupports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractManagementFeeOverrideFrequency",
                table: "FinancialSupports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractManagementFeeOverridePostingMode",
                table: "FinancialSupports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractManagementFeeOverrideProrataMethod",
                table: "FinancialSupports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ContractManagementFeeOverrideRate",
                table: "FinancialSupports",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductManagementFeePolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AnnualRate = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    ProrataMethod = table.Column<int>(type: "int", nullable: false),
                    PostingMode = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductManagementFeePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductManagementFeePolicies_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductManagementFeePolicies_ProductId",
                table: "ProductManagementFeePolicies",
                column: "ProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductManagementFeePolicies");

            migrationBuilder.DropColumn(
                name: "ContractManagementFeeOverrideEffectiveDate",
                table: "FinancialSupports");

            migrationBuilder.DropColumn(
                name: "ContractManagementFeeOverrideEnabled",
                table: "FinancialSupports");

            migrationBuilder.DropColumn(
                name: "ContractManagementFeeOverrideEndDate",
                table: "FinancialSupports");

            migrationBuilder.DropColumn(
                name: "ContractManagementFeeOverrideFrequency",
                table: "FinancialSupports");

            migrationBuilder.DropColumn(
                name: "ContractManagementFeeOverridePostingMode",
                table: "FinancialSupports");

            migrationBuilder.DropColumn(
                name: "ContractManagementFeeOverrideProrataMethod",
                table: "FinancialSupports");

            migrationBuilder.DropColumn(
                name: "ContractManagementFeeOverrideRate",
                table: "FinancialSupports");
        }
    }
}
