using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddFeePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeePolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    FeeType = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    ContractId = table.Column<int>(type: "int", nullable: true),
                    CompartmentId = table.Column<int>(type: "int", nullable: true),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: true),
                    SupportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AmountMode = table.Column<int>(type: "int", nullable: false),
                    ApplyOn = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    MinAmount = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsOverride = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Frequency = table.Column<int>(type: "int", nullable: true),
                    ProrataMethod = table.Column<int>(type: "int", nullable: true),
                    PostingMode = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeePolicies_Compartments_CompartmentId",
                        column: x => x.CompartmentId,
                        principalTable: "Compartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FeePolicies_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_FeePolicies_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FeePolicies_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeePolicies_CompartmentId",
                table: "FeePolicies",
                column: "CompartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePolicies_ContractId",
                table: "FeePolicies",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePolicies_FinancialSupportId",
                table: "FeePolicies",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePolicies_ProductId",
                table: "FeePolicies",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePolicies_Resolution",
                table: "FeePolicies",
                columns: new[] { "Category", "FeeType", "Scope", "ProductId", "ContractId", "CompartmentId", "FinancialSupportId", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeePolicies");
        }
    }
}
