using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data.Seed
{
    public static class TaxRuleVersionSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaxRuleVersion>().HasData(
                new TaxRuleVersion
                {
                    Id = 1,
                    Code = "FR-ASSURANCE-2024",
                    Label = "Référentiel fiscal France Assurance 2024+",
                    EffectiveFrom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffectiveTo = null,
                    IsActive = true,
                    Notes = "Version initiale du moteur fiscal (PFU, seuil 8 ans AV, art. 990I/757B, PER).",
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                }
            );
        }
    }
}
