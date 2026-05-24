using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddTemporalTaxEngineFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractTaxStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    ContractFamily = table.Column<int>(type: "int", nullable: false),
                    NetPremiums = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    TotalGainStock = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    SocialChargesAlreadyPaid = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    SocialChargesRemainingDue = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    LastValuationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractTaxStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractTaxStates_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxLaws",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EffectiveDateStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveDateEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LawReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxLaws", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractTaxStateId = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: true),
                    TaxComputationId = table.Column<int>(type: "int", nullable: true),
                    EventKind = table.Column<int>(type: "int", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxEvents_ContractTaxStates_ContractTaxStateId",
                        column: x => x.ContractTaxStateId,
                        principalTable: "ContractTaxStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaxEvents_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxEvents_TaxComputations_TaxComputationId",
                        column: x => x.TaxComputationId,
                        principalTable: "TaxComputations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TaxGenerations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxLawId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    TaxRuleType = table.Column<int>(type: "int", nullable: false),
                    TaxCompartmentType = table.Column<int>(type: "int", nullable: false),
                    EffectiveDateStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveDateEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiresPaymentDateSplit = table.Column<bool>(type: "bit", nullable: false),
                    RequiresHoldingDuration = table.Column<bool>(type: "bit", nullable: false),
                    UsesHistoricalSocialRate = table.Column<bool>(type: "bit", nullable: false),
                    FormulaMetadataJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxGenerations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxGenerations_TaxLaws_TaxLawId",
                        column: x => x.TaxLawId,
                        principalTable: "TaxLaws",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GainLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractTaxStateId = table.Column<int>(type: "int", nullable: false),
                    TaxGenerationId = table.Column<int>(type: "int", nullable: true),
                    GainDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GainAmount = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    RemainingGainAmount = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    SocialChargesAlreadyPaid = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    ApplicableSocialRate = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    SupportNature = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GainLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GainLots_ContractTaxStates_ContractTaxStateId",
                        column: x => x.ContractTaxStateId,
                        principalTable: "ContractTaxStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GainLots_TaxGenerations_TaxGenerationId",
                        column: x => x.TaxGenerationId,
                        principalTable: "TaxGenerations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PremiumLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractTaxStateId = table.Column<int>(type: "int", nullable: false),
                    TaxGenerationId = table.Column<int>(type: "int", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrossPremium = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    NetPremium = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    RemainingNetPremium = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    SocialChargesPaid = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    AgeAtPayment = table.Column<int>(type: "int", nullable: true),
                    TaxCompartmentType = table.Column<int>(type: "int", nullable: false),
                    SupportNature = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PremiumLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PremiumLots_ContractTaxStates_ContractTaxStateId",
                        column: x => x.ContractTaxStateId,
                        principalTable: "ContractTaxStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PremiumLots_TaxGenerations_TaxGenerationId",
                        column: x => x.TaxGenerationId,
                        principalTable: "TaxGenerations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxCalculationAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxComputationId = table.Column<int>(type: "int", nullable: true),
                    ContractTaxStateId = table.Column<int>(type: "int", nullable: true),
                    TaxGenerationId = table.Column<int>(type: "int", nullable: true),
                    StepCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(20,7)", nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(6,3)", nullable: true),
                    ComputedAmount = table.Column<decimal>(type: "decimal(20,7)", nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCalculationAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxCalculationAudits_ContractTaxStates_ContractTaxStateId",
                        column: x => x.ContractTaxStateId,
                        principalTable: "ContractTaxStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TaxCalculationAudits_TaxComputations_TaxComputationId",
                        column: x => x.TaxComputationId,
                        principalTable: "TaxComputations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaxCalculationAudits_TaxGenerations_TaxGenerationId",
                        column: x => x.TaxGenerationId,
                        principalTable: "TaxGenerations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PsHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractTaxStateId = table.Column<int>(type: "int", nullable: false),
                    GainLotId = table.Column<int>(type: "int", nullable: true),
                    LevyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaxableBase = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    AppliedRate = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(20,7)", nullable: false),
                    SupportNature = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PsHistory_ContractTaxStates_ContractTaxStateId",
                        column: x => x.ContractTaxStateId,
                        principalTable: "ContractTaxStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PsHistory_GainLots_GainLotId",
                        column: x => x.GainLotId,
                        principalTable: "GainLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TaxLaws",
                columns: new[] { "Id", "Code", "CountryCode", "CreatedDate", "EffectiveDateEnd", "EffectiveDateStart", "IsActive", "Label", "LawReference", "Notes" },
                values: new object[,]
                {
                    { 1, "FR-1997-AV", "FR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 9, 26, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1997, 9, 26, 0, 0, 0, 0, DateTimeKind.Utc), false, "Réforme assurance-vie 26/09/1997", "Loi de finances 1998", "Modernisation du régime fiscal assurance-vie." },
                    { 2, "FR-2017-PFU", "FR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2017, 9, 27, 0, 0, 0, 0, DateTimeKind.Utc), true, "PFU / Flat Tax 2017-2018", "LF 2018", "Mise en place PFU avec maintien des spécificités assurance-vie." },
                    { 3, "FR-2005-PS-FAU", "FR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2005, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Prélèvements sociaux au fil de l'eau", "LF 2005-2006", "PS annuels sur fonds euro, stock historique à tracer." }
                });

            migrationBuilder.InsertData(
                table: "TaxGenerations",
                columns: new[] { "Id", "Code", "CreatedDate", "EffectiveDateEnd", "EffectiveDateStart", "FormulaMetadataJson", "Label", "ProductType", "RequiresHoldingDuration", "RequiresPaymentDateSplit", "TaxCompartmentType", "TaxLawId", "TaxRuleType", "UsesHistoricalSocialRate" },
                values: new object[,]
                {
                    { 1, "AV-PRE-1997", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1997, 9, 25, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"regime\":\"legacy\"}", "Assurance-vie primes avant 26/09/1997", 0, true, true, 0, 1, 0, true },
                    { 2, "AV-1997-2017", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2017, 9, 26, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1997, 9, 26, 0, 0, 0, 0, DateTimeKind.Utc), "{\"regime\":\"pfl\"}", "Assurance-vie primes 26/09/1997 -> 26/09/2017", 0, true, true, 0, 1, 0, true },
                    { 3, "AV-PFU-2017", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2017, 9, 27, 0, 0, 0, 0, DateTimeKind.Utc), "{\"regime\":\"pfu\"}", "Assurance-vie primes après 27/09/2017", 0, true, true, 0, 2, 0, false },
                    { 4, "PS-HISTORICAL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(1996, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"mode\":\"historical-rate\"}", "Prélèvements sociaux historiques", 0, false, false, 0, 3, 1, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractTaxStates_ContractId",
                table: "ContractTaxStates",
                column: "ContractId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GainLots_ContractTaxStateId_GainDate",
                table: "GainLots",
                columns: new[] { "ContractTaxStateId", "GainDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GainLots_TaxGenerationId",
                table: "GainLots",
                column: "TaxGenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_PremiumLots_ContractTaxStateId_PaymentDate",
                table: "PremiumLots",
                columns: new[] { "ContractTaxStateId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PremiumLots_TaxGenerationId",
                table: "PremiumLots",
                column: "TaxGenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_PsHistory_ContractTaxStateId_LevyDate",
                table: "PsHistory",
                columns: new[] { "ContractTaxStateId", "LevyDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PsHistory_GainLotId",
                table: "PsHistory",
                column: "GainLotId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCalculationAudits_ContractTaxStateId",
                table: "TaxCalculationAudits",
                column: "ContractTaxStateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCalculationAudits_TaxComputationId_CreatedDate",
                table: "TaxCalculationAudits",
                columns: new[] { "TaxComputationId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxCalculationAudits_TaxGenerationId",
                table: "TaxCalculationAudits",
                column: "TaxGenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxEvents_ContractTaxStateId_EventDate",
                table: "TaxEvents",
                columns: new[] { "ContractTaxStateId", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxEvents_OperationId",
                table: "TaxEvents",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxEvents_TaxComputationId",
                table: "TaxEvents",
                column: "TaxComputationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxGenerations_ProductType_TaxRuleType_EffectiveDateStart_EffectiveDateEnd",
                table: "TaxGenerations",
                columns: new[] { "ProductType", "TaxRuleType", "EffectiveDateStart", "EffectiveDateEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxGenerations_TaxLawId",
                table: "TaxGenerations",
                column: "TaxLawId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxLaws_CountryCode_EffectiveDateStart_EffectiveDateEnd",
                table: "TaxLaws",
                columns: new[] { "CountryCode", "EffectiveDateStart", "EffectiveDateEnd" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PremiumLots");

            migrationBuilder.DropTable(
                name: "PsHistory");

            migrationBuilder.DropTable(
                name: "TaxCalculationAudits");

            migrationBuilder.DropTable(
                name: "TaxEvents");

            migrationBuilder.DropTable(
                name: "GainLots");

            migrationBuilder.DropTable(
                name: "ContractTaxStates");

            migrationBuilder.DropTable(
                name: "TaxGenerations");

            migrationBuilder.DropTable(
                name: "TaxLaws");
        }
    }
}
