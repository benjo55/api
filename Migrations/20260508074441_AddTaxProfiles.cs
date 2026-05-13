using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractFamily = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EntryDeductible = table.Column<bool>(type: "bit", nullable: false),
                    EntryDeductionCap = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DurationThresholdYears = table.Column<int>(type: "int", nullable: false),
                    IrRateBeforeThreshold = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IrRateAfterThreshold = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ContributionCapForReducedRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IrRateAboveContributionCap = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SocialChargesRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    GainAllowanceSingle = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GainAllowanceCouple = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IrExemptAfterThreshold = table.Column<bool>(type: "bit", nullable: false),
                    SocialChargesExemptAfterThreshold = table.Column<bool>(type: "bit", nullable: false),
                    HasDeathTaxArticle990I = table.Column<bool>(type: "bit", nullable: false),
                    Death990I_AllowancePerBeneficiary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Death990I_Rate1 = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Death990I_Rate1Threshold = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Death990I_Rate2 = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    HasDeathTaxArticle757B = table.Column<bool>(type: "bit", nullable: false),
                    Death757B_GlobalAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExitMode = table.Column<int>(type: "int", nullable: false),
                    RenteTaxedAsPension = table.Column<bool>(type: "bit", nullable: false),
                    RentePartImposable = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CanChooseBareme = table.Column<bool>(type: "bit", nullable: false),
                    HasSuccessionBenefit = table.Column<bool>(type: "bit", nullable: false),
                    Locked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxProfiles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TaxProfiles",
                columns: new[] { "Id", "CanChooseBareme", "ContractFamily", "ContributionCapForReducedRate", "CreatedDate", "Death757B_GlobalAllowance", "Death990I_AllowancePerBeneficiary", "Death990I_Rate1", "Death990I_Rate1Threshold", "Death990I_Rate2", "Description", "DurationThresholdYears", "EntryDeductible", "EntryDeductionCap", "ExitMode", "GainAllowanceCouple", "GainAllowanceSingle", "HasDeathTaxArticle757B", "HasDeathTaxArticle990I", "HasSuccessionBenefit", "IrExemptAfterThreshold", "IrRateAboveContributionCap", "IrRateAfterThreshold", "IrRateBeforeThreshold", "Label", "Locked", "RentePartImposable", "RenteTaxedAsPension", "SocialChargesExemptAfterThreshold", "SocialChargesRate", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, true, 0, 150000m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30500m, 152500m, 20m, 700000m, 31.25m, "Contrat patrimonial individuel en fonds € et/ou UC. Avantage successoral Art. 990 I / 757 B.", 8, false, null, 2, 9200m, 4600m, true, true, true, false, 12.8m, 7.5m, 12.8m, "Assurance-vie individuelle", true, null, false, false, 17.2m, null },
                    { 2, true, 1, 150000m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Même fiscalité des rachats que l'AV. Pas d'avantage successoral spécifique. Détention possible par personnes morales.", 8, false, null, 2, 9200m, 4600m, false, false, false, false, 12.8m, 7.5m, 12.8m, "Contrat de capitalisation", true, null, false, false, 17.2m, null },
                    { 3, true, 2, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30500m, 152500m, 20m, 700000m, 31.25m, "Versements volontaires déductibles du revenu imposable. Sortie en capital (IR + PS sur gains) ou rente (pension).", 0, true, null, 2, null, null, true, true, false, false, 12.8m, 0m, 0m, "PER individuel (PERIN)", true, null, true, false, 17.2m, null },
                    { 4, false, 3, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Successeur du PERCO. Abondement exonéré, participation/intéressement sous conditions. Sortie capital souvent exonérée d'IR.", 0, true, null, 2, null, null, false, false, false, true, 12.8m, 0m, 0m, "PER collectif (PERCOL)", true, null, true, false, 17.2m, null },
                    { 5, false, 4, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Contrat collectif obligatoire. Sortie principalement en rente imposée comme pension.", 0, true, null, 1, null, null, false, false, false, false, 12.8m, 0m, 0m, "PER obligatoire (PERO)", true, null, true, false, 17.2m, null },
                    { 6, false, 5, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Retraite des travailleurs non-salariés. Cotisations déductibles. Sortie uniquement en rente imposée comme pension.", 0, true, null, 1, null, null, false, false, false, false, 0m, 0m, 0m, "Contrat Madelin", true, null, true, false, 17.2m, null },
                    { 7, false, 6, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Ancien régime retraite entreprise. Cotisations déductibles. Sortie principalement en rente imposée comme pension.", 0, true, null, 1, null, null, false, false, false, false, 0m, 0m, 0m, "Article 83", true, null, true, false, 17.2m, null },
                    { 8, false, 7, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Exonération d'IR après 5 ans. Prélèvements sociaux maintenus. PFU 30 % avant 5 ans.", 5, false, null, 0, null, null, false, false, false, true, 12.8m, 0m, 12.8m, "PEA (Plan d'Épargne en Actions)", true, null, false, false, 17.2m, null },
                    { 9, false, 8, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Couverture décès/invalidité/incapacité. Cotisations employeur exonérées sous plafonds.", 0, true, null, 2, null, null, false, false, false, false, 0m, 0m, 0m, "Prévoyance collective", true, null, true, false, 17.2m, null },
                    { 10, false, 9, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Rentes souvent exonérées d'IR selon la structure du contrat.", 0, false, null, 1, null, null, false, false, false, true, 0m, 0m, 0m, "Contrat dépendance", true, null, false, false, 17.2m, null },
                    { 11, false, 10, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Souscrit par une société sur un dirigeant ou salarié clé. Indemnité imposable à l'IS.", 8, false, null, 0, null, null, false, false, false, false, 12.8m, 12.8m, 12.8m, "Homme-clé / Assurance-vie entreprise", true, null, false, false, 0m, null },
                    { 12, false, 11, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, "Très encadrée. Forte fiscalité sociale. Sortie en rente imposée comme pension.", 0, true, null, 1, null, null, false, false, false, false, 0m, 0m, 0m, "Article 39 (retraite à prestations définies)", true, null, true, false, 17.2m, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxProfiles");
        }
    }
}
