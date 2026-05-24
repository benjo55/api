using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data.Seed
{
    /// <summary>
    /// Seed des types de produit d'épargne/retraite/prévoyance.
    /// DefaultTaxProfileId pointe vers le TaxProfile canonique de la famille fiscale correspondante.
    /// TaxProfile IDs: 1=AV, 2=CAPI, 3=PERIN, 4=PERCOL, 5=PERO, 6=Madelin, 7=Art83,
    ///                 8=PEA, 9=PrévoyanceCollective, 10=Dépendance, 11=Homme-clé
    /// </summary>
    public static class ProductTypeSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductType>().HasData(

                // ── Assurance épargne ──────────────────────────────────────────────
                new ProductType
                {
                    Id = 1,
                    Code = "AV",
                    Label = "Assurance-vie",
                    Category = "Insurance",
                    DefaultTaxProfileId = 1,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new ProductType
                {
                    Id = 2,
                    Code = "CAPI",
                    Label = "Capitalisation",
                    Category = "Insurance",
                    DefaultTaxProfileId = 2,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },

                // ── PER ───────────────────────────────────────────────────────────
                new ProductType
                {
                    Id = 3,
                    Code = "PERIN",
                    Label = "PER individuel",
                    Category = "Insurance",
                    DefaultTaxProfileId = 3,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new ProductType
                {
                    Id = 4,
                    Code = "PERCOL",
                    Label = "PER collectif",
                    Category = "Insurance",
                    DefaultTaxProfileId = 4,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new ProductType
                {
                    Id = 5,
                    Code = "PERO",
                    Label = "PER obligatoire",
                    Category = "Insurance",
                    DefaultTaxProfileId = 5,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },

                // ── Ancien régime retraite ─────────────────────────────────────────
                new ProductType
                {
                    Id = 6,
                    Code = "MADELIN",
                    Label = "Contrat Madelin",
                    Category = "Insurance",
                    DefaultTaxProfileId = 6,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new ProductType
                {
                    Id = 7,
                    Code = "ART83",
                    Label = "Article 83",
                    Category = "Insurance",
                    DefaultTaxProfileId = 7,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },

                // ── Epargne financière ────────────────────────────────────────────
                new ProductType
                {
                    Id = 8,
                    Code = "PEA",
                    Label = "PEA",
                    Category = "Banking",
                    DefaultTaxProfileId = 8,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },

                // ── Prévoyance / Protection ───────────────────────────────────────
                new ProductType
                {
                    Id = 9,
                    Code = "PREV",
                    Label = "Prévoyance collective",
                    Category = "Insurance",
                    DefaultTaxProfileId = 9,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new ProductType
                {
                    Id = 10,
                    Code = "DEP",
                    Label = "Dépendance",
                    Category = "Insurance",
                    DefaultTaxProfileId = 10,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new ProductType
                {
                    Id = 11,
                    Code = "HCL",
                    Label = "Homme-clé",
                    Category = "Insurance",
                    DefaultTaxProfileId = 11,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                }
            );
        }
    }
}
