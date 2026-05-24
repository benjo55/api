using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Data.Seed
{
    public static class TaxGenerationSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaxGeneration>().HasData(
                new TaxGeneration
                {
                    Id = 1,
                    TaxLawId = 1,
                    Code = "AV-PRE-1997",
                    Label = "Assurance-vie primes avant 26/09/1997",
                    ProductType = ContractFamily.AssuranceVie,
                    TaxRuleType = TaxRuleType.IncomeTax,
                    TaxCompartmentType = TaxCompartmentType.General,
                    EffectiveDateStart = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffectiveDateEnd = new DateTime(1997, 9, 25, 0, 0, 0, DateTimeKind.Utc),
                    RequiresPaymentDateSplit = true,
                    RequiresHoldingDuration = true,
                    UsesHistoricalSocialRate = true,
                    FormulaMetadataJson = "{\"regime\":\"legacy\"}",
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new TaxGeneration
                {
                    Id = 2,
                    TaxLawId = 1,
                    Code = "AV-1997-2017",
                    Label = "Assurance-vie primes 26/09/1997 -> 26/09/2017",
                    ProductType = ContractFamily.AssuranceVie,
                    TaxRuleType = TaxRuleType.IncomeTax,
                    TaxCompartmentType = TaxCompartmentType.General,
                    EffectiveDateStart = new DateTime(1997, 9, 26, 0, 0, 0, DateTimeKind.Utc),
                    EffectiveDateEnd = new DateTime(2017, 9, 26, 0, 0, 0, DateTimeKind.Utc),
                    RequiresPaymentDateSplit = true,
                    RequiresHoldingDuration = true,
                    UsesHistoricalSocialRate = true,
                    FormulaMetadataJson = "{\"regime\":\"pfl\"}",
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new TaxGeneration
                {
                    Id = 3,
                    TaxLawId = 2,
                    Code = "AV-PFU-2017",
                    Label = "Assurance-vie primes après 27/09/2017",
                    ProductType = ContractFamily.AssuranceVie,
                    TaxRuleType = TaxRuleType.IncomeTax,
                    TaxCompartmentType = TaxCompartmentType.General,
                    EffectiveDateStart = new DateTime(2017, 9, 27, 0, 0, 0, DateTimeKind.Utc),
                    EffectiveDateEnd = null,
                    RequiresPaymentDateSplit = true,
                    RequiresHoldingDuration = true,
                    UsesHistoricalSocialRate = false,
                    FormulaMetadataJson = "{\"regime\":\"pfu\"}",
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new TaxGeneration
                {
                    Id = 4,
                    TaxLawId = 3,
                    Code = "PS-HISTORICAL",
                    Label = "Prélèvements sociaux historiques",
                    ProductType = ContractFamily.AssuranceVie,
                    TaxRuleType = TaxRuleType.SocialCharges,
                    TaxCompartmentType = TaxCompartmentType.General,
                    EffectiveDateStart = new DateTime(1996, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffectiveDateEnd = null,
                    RequiresPaymentDateSplit = false,
                    RequiresHoldingDuration = false,
                    UsesHistoricalSocialRate = true,
                    FormulaMetadataJson = "{\"mode\":\"historical-rate\"}",
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                }
            );
        }
    }
}
