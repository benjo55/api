using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data.Seed
{
    public static class TaxLawSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaxLaw>().HasData(
                new TaxLaw
                {
                    Id = 1,
                    Code = "FR-1997-AV",
                    Label = "Réforme assurance-vie 26/09/1997",
                    CountryCode = "FR",
                    EffectiveDateStart = new DateTime(1997, 9, 26, 0, 0, 0, DateTimeKind.Utc),
                    EffectiveDateEnd = new DateTime(2017, 9, 26, 0, 0, 0, DateTimeKind.Utc),
                    LawReference = "Loi de finances 1998",
                    Notes = "Modernisation du régime fiscal assurance-vie.",
                    IsActive = false,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new TaxLaw
                {
                    Id = 2,
                    Code = "FR-2017-PFU",
                    Label = "PFU / Flat Tax 2017-2018",
                    CountryCode = "FR",
                    EffectiveDateStart = new DateTime(2017, 9, 27, 0, 0, 0, DateTimeKind.Utc),
                    EffectiveDateEnd = null,
                    LawReference = "LF 2018",
                    Notes = "Mise en place PFU avec maintien des spécificités assurance-vie.",
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new TaxLaw
                {
                    Id = 3,
                    Code = "FR-2005-PS-FAU",
                    Label = "Prélèvements sociaux au fil de l'eau",
                    CountryCode = "FR",
                    EffectiveDateStart = new DateTime(2005, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffectiveDateEnd = null,
                    LawReference = "LF 2005-2006",
                    Notes = "PS annuels sur fonds euro, stock historique à tracer.",
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                }
            );
        }
    }
}
