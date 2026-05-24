using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ProductTypes",
                columns: new[] { "Id", "Category", "Code", "CreatedDate", "DefaultTaxProfileId", "IsActive", "Label", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "Insurance", "AV", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "Assurance-vie", null },
                    { 2, "Insurance", "CAPI", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "Capitalisation", null },
                    { 3, "Insurance", "PERIN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "PER individuel", null },
                    { 4, "Insurance", "PERCOL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "PER collectif", null },
                    { 5, "Insurance", "PERO", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "PER obligatoire", null },
                    { 6, "Insurance", "MADELIN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, "Contrat Madelin", null },
                    { 7, "Insurance", "ART83", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, true, "Article 83", null },
                    { 8, "Banking", "PEA", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, true, "PEA", null },
                    { 9, "Insurance", "PREV", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, true, "Prévoyance collective", null },
                    { 10, "Insurance", "DEP", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, true, "Dépendance", null },
                    { 11, "Insurance", "HCL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 11, true, "Homme-clé", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 11);
        }
    }
}
