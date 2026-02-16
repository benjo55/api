using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class SeedContractOptionTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ContractOptionTypes",
                columns: new[] { "Id", "Category", "Code", "DefaultCost", "Label", "Mechanism", "Objective" },
                values: new object[,]
                {
                    { 1, "Gestion financière", "STOP_LOSS_GAIN", "Gratuit ou frais d’arbitrage", "Arbitrage conditionnel (stop-loss/gain)", "Transfert auto vers support sécurisé quand un seuil est atteint", "Sécuriser gains ou limiter pertes" },
                    { 2, "Gestion financière", "SECURISATION_PV", "Souvent gratuit", "Sécurisation des plus-values", "Les plus-values sont transférées régulièrement vers fonds sécurisé", "Mettre à l’abri les gains" },
                    { 3, "Gestion financière", "DYNAMISATION_INTERETS", "Gratuit", "Dynamisation des intérêts", "Les intérêts du fonds € sont investis en UC", "Booster la performance" },
                    { 4, "Gestion financière", "REEQUILIBRAGE_AUTO", "Gratuit ou frais d’arbitrage", "Rééquilibrage automatique", "Arbitrages périodiques pour revenir à la répartition choisie", "Maintenir allocation cible" },
                    { 5, "Gestion financière", "INVEST_PROGRESSIF", "Gratuit", "Investissement progressif", "Transfert programmé du fonds € vers UC", "Lisser le risque d’entrée" },
                    { 6, "Gestion financière", "DESINVEST_PROGRESSIF", "Gratuit", "Désinvestissement progressif", "Transfert programmé d’UC vers fonds €", "Sécuriser progressivement le capital" },
                    { 10, "Garanties complémentaires", "GARANTIE_PLANCHER_SIMPLE", "Prime d’assurance prélevée", "Garantie plancher simple", "Au décès, versement min = primes nettes versées", "Protéger le capital transmis" },
                    { 11, "Garanties complémentaires", "GARANTIE_PLANCHER_INDEXEE", "Plus coûteux que simple", "Garantie plancher indexée", "Plancher = primes + taux garanti annuel", "Protection + rendement minimum" },
                    { 12, "Garanties complémentaires", "GARANTIE_PLANCHER_CLIQUET", "Prime plus élevée", "Garantie plancher cliquet", "Plancher = valeur max atteinte par le contrat", "Sécuriser le plus haut atteint" },
                    { 13, "Garanties complémentaires", "GARANTIE_DECES_MAJ", "Prime d’assurance", "Garantie décès majorée", "Valeur contrat + % supplémentaire", "Augmenter capital transmis" },
                    { 14, "Garanties complémentaires", "GARANTIE_RENTE_PLANCHER", "Prime ou frais intégrés", "Garantie rente plancher", "Garantie d’une rente viagère minimale à la sortie", "Sécuriser un revenu minimal" },
                    { 15, "Garanties complémentaires", "GARANTIE_PLANCHER_PROG", "Moins coûteuse", "Garantie plancher progressive", "Couverture décroissante au fil des ans", "Réduire le coût avec le temps" },
                    { 20, "Autres options", "AVANCE", "Intérêts sur avance", "Avances", "Prêt garanti par contrat avec taux d’intérêt", "Liquidité sans rachat" },
                    { 21, "Autres options", "OPTION_RENTE", "Impacte le montant de la rente", "Options de rente", "Différents modes de rente viagère ou temporaire", "Adapter la sortie" },
                    { 22, "Autres options", "GESTION_SOUS_MANDAT", "0,2 à 0,8 %/an", "Gestion sous mandat", "L’assureur gère selon un profil", "Déléguer totalement la gestion" },
                    { 23, "Autres options", "CLAUSE_DEMEMBREE", "Aucun coût", "Clause bénéficiaire démembrée", "Usufruitier = conjoint / NP = enfants", "Optimiser la fiscalité successorale" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "ContractOptionTypes",
                keyColumn: "Id",
                keyValue: 23);
        }
    }
}
