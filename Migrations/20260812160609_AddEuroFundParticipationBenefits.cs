using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddEuroFundParticipationBenefits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EuroFundConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    AccrualMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AnnualCreditMonth = table.Column<int>(type: "int", nullable: false),
                    AnnualCreditDay = table.Column<int>(type: "int", nullable: false),
                    ProvisionalRateMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProvisionalRatePercentage = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: true),
                    FixedProvisionalRate = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: true),
                    PreviousFinalRatePercentage = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: true),
                    EarlyExitRateMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LotConsumptionMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RateNature = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ManagementFeeTreatment = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MinimumGuaranteedRate = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: true),
                    RateFloor = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: true),
                    RateCap = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EuroFundConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EuroFundConfigurations_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EuroFundFinancialYears",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    TmeRate = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: true),
                    AssetYield = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: true),
                    OpeningPpbReserve = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    PpbAllocation = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    PpbRelease = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    ClosingPpbReserve = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    FinalServedRate = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: true),
                    RateNature = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EuroFundFinancialYears", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EuroFundFinancialYears_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EuroFundLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    SourceOperationId = table.Column<int>(type: "int", nullable: true),
                    InitialAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    ValueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BonusRuleId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    BonusRate = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EuroFundLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EuroFundLots_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EuroFundLots_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EuroFundLots_Operations_SourceOperationId",
                        column: x => x.SourceOperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EuroFundRevaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    FinancialYear = table.Column<int>(type: "int", nullable: false),
                    FinalServedRate = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: false),
                    BookValueBeforeCredit = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    WeightedExposure = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    InterestAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    YearBasis = table.Column<int>(type: "int", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EuroFundRevaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EuroFundRevaluations_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EuroFundRevaluations_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EuroFundRevaluations_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RateType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RateValue = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EuroFundLotMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EuroFundLotId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: true),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EuroFundLotMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EuroFundLotMovements_EuroFundLots_EuroFundLotId",
                        column: x => x.EuroFundLotId,
                        principalTable: "EuroFundLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EuroFundLotMovements_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EuroFundRevaluationDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EuroFundRevaluationId = table.Column<int>(type: "int", nullable: false),
                    EuroFundLotId = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: false),
                    BonusRate = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: false),
                    ApplicableRate = table.Column<decimal>(type: "decimal(18,7)", precision: 18, scale: 7, nullable: false),
                    DayCount = table.Column<int>(type: "int", nullable: false),
                    YearBasis = table.Column<int>(type: "int", nullable: false),
                    InterestAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EuroFundRevaluationDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EuroFundRevaluationDetails_EuroFundLots_EuroFundLotId",
                        column: x => x.EuroFundLotId,
                        principalTable: "EuroFundLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EuroFundRevaluationDetails_EuroFundRevaluations_EuroFundRevaluationId",
                        column: x => x.EuroFundRevaluationId,
                        principalTable: "EuroFundRevaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_EuroFundConfigurations_FinancialSupport",
                table: "EuroFundConfigurations",
                column: "FinancialSupportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_EuroFundFinancialYears_Fund_Year",
                table: "EuroFundFinancialYears",
                columns: new[] { "FinancialSupportId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EuroFundLotMovements_Contract_Fund_Date",
                table: "EuroFundLotMovements",
                columns: new[] { "ContractId", "FinancialSupportId", "MovementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EuroFundLotMovements_EuroFundLotId",
                table: "EuroFundLotMovements",
                column: "EuroFundLotId");

            migrationBuilder.CreateIndex(
                name: "IX_EuroFundLotMovements_Operation",
                table: "EuroFundLotMovements",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_EuroFundLots_Contract_Fund",
                table: "EuroFundLots",
                columns: new[] { "ContractId", "FinancialSupportId" });

            migrationBuilder.CreateIndex(
                name: "IX_EuroFundLots_FinancialSupportId",
                table: "EuroFundLots",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_EuroFundLots_SourceOperation",
                table: "EuroFundLots",
                column: "SourceOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_EuroFundLots_ValueDate",
                table: "EuroFundLots",
                column: "ValueDate");

            migrationBuilder.CreateIndex(
                name: "IX_EuroFundRevaluationDetails_EuroFundLotId",
                table: "EuroFundRevaluationDetails",
                column: "EuroFundLotId");

            migrationBuilder.CreateIndex(
                name: "IX_EuroFundRevaluationDetails_Revaluation_Period",
                table: "EuroFundRevaluationDetails",
                columns: new[] { "EuroFundRevaluationId", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_EuroFundRevaluations_FinancialSupportId",
                table: "EuroFundRevaluations",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "UX_EuroFundRevaluations_Contract_Fund_Year",
                table: "EuroFundRevaluations",
                columns: new[] { "ContractId", "FinancialSupportId", "FinancialYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_EuroFundRevaluations_Operation",
                table: "EuroFundRevaluations",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ReferenceRates_Type_Date_Source",
                table: "ReferenceRates",
                columns: new[] { "RateType", "RateDate", "Source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EuroFundConfigurations");

            migrationBuilder.DropTable(
                name: "EuroFundFinancialYears");

            migrationBuilder.DropTable(
                name: "EuroFundLotMovements");

            migrationBuilder.DropTable(
                name: "EuroFundRevaluationDetails");

            migrationBuilder.DropTable(
                name: "ReferenceRates");

            migrationBuilder.DropTable(
                name: "EuroFundLots");

            migrationBuilder.DropTable(
                name: "EuroFundRevaluations");
        }
    }
}
