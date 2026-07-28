using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data.Seed
{
    public static class ProductEnvelopeSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<ProductCategory>().HasData(
                new ProductCategory { Id = 1, Code = "SAVINGS", Name = "Épargne", IsActive = true, CreatedDate = seedDate },
                new ProductCategory { Id = 2, Code = "RETIREMENT", Name = "Retraite", IsActive = true, CreatedDate = seedDate },
                new ProductCategory { Id = 3, Code = "CAPITALIZATION", Name = "Capitalisation", IsActive = true, CreatedDate = seedDate },
                new ProductCategory { Id = 4, Code = "PROTECTION", Name = "Prévoyance / protection", IsActive = true, CreatedDate = seedDate },
                new ProductCategory { Id = 5, Code = "INVESTMENT", Name = "Investissement", IsActive = true, CreatedDate = seedDate }
            );

            modelBuilder.Entity<LegalNature>().HasData(
                new LegalNature { Id = 1, Code = "INSURANCE_CONTRACT", Name = "Contrat d'assurance", IsActive = true, CreatedDate = seedDate },
                new LegalNature { Id = 2, Code = "CAPITALIZATION_CONTRACT", Name = "Contrat de capitalisation", IsActive = true, CreatedDate = seedDate },
                new LegalNature { Id = 3, Code = "RETIREMENT_PLAN", Name = "Plan d'épargne retraite", IsActive = true, CreatedDate = seedDate },
                new LegalNature { Id = 4, Code = "COLLECTIVE_INSURANCE_CONTRACT", Name = "Contrat collectif d'assurance", IsActive = true, CreatedDate = seedDate },
                new LegalNature { Id = 5, Code = "INVESTMENT_ACCOUNT", Name = "Compte d'investissement", IsActive = true, CreatedDate = seedDate }
            );

            modelBuilder.Entity<ProductEnvelope>().HasData(
                new ProductEnvelope
                {
                    Id = 1,
                    Code = "AV",
                    Name = "Assurance-vie",
                    ProductCategoryId = 1,
                    LegalNatureId = 1,
                    DefaultTaxProfileId = 1,
                    IsIndividual = true,
                    SupportsBeneficiaryClause = true,
                    RequiresInsuredPerson = true,
                    IsActive = true,
                    CreatedDate = seedDate
                },
                new ProductEnvelope
                {
                    Id = 2,
                    Code = "CAPI",
                    Name = "Capitalisation",
                    ProductCategoryId = 3,
                    LegalNatureId = 2,
                    DefaultTaxProfileId = 2,
                    IsIndividual = true,
                    SupportsBeneficiaryClause = false,
                    IsActive = true,
                    CreatedDate = seedDate
                },
                new ProductEnvelope
                {
                    Id = 3,
                    Code = "PERIN",
                    Name = "PER individuel",
                    ProductCategoryId = 2,
                    LegalNatureId = 3,
                    DefaultTaxProfileId = 3,
                    IsIndividual = true,
                    SupportsBeneficiaryClause = true,
                    IsActive = true,
                    CreatedDate = seedDate
                },
                new ProductEnvelope
                {
                    Id = 4,
                    Code = "PERCOL",
                    Name = "PER collectif",
                    ProductCategoryId = 2,
                    LegalNatureId = 3,
                    DefaultTaxProfileId = 4,
                    IsCollective = true,
                    SupportsBeneficiaryClause = true,
                    IsActive = true,
                    CreatedDate = seedDate
                },
                new ProductEnvelope
                {
                    Id = 5,
                    Code = "PERO",
                    Name = "PER obligatoire",
                    ProductCategoryId = 2,
                    LegalNatureId = 3,
                    DefaultTaxProfileId = 5,
                    IsCollective = true,
                    SupportsBeneficiaryClause = true,
                    IsActive = true,
                    CreatedDate = seedDate
                },
                new ProductEnvelope
                {
                    Id = 6,
                    Code = "MADELIN",
                    Name = "Contrat Madelin",
                    ProductCategoryId = 2,
                    LegalNatureId = 1,
                    DefaultTaxProfileId = 6,
                    IsIndividual = true,
                    SupportsBeneficiaryClause = true,
                    IsActive = true,
                    CreatedDate = seedDate
                },
                new ProductEnvelope
                {
                    Id = 7,
                    Code = "ART83",
                    Name = "Article 83",
                    ProductCategoryId = 2,
                    LegalNatureId = 4,
                    DefaultTaxProfileId = 7,
                    IsCollective = true,
                    SupportsBeneficiaryClause = true,
                    IsActive = true,
                    CreatedDate = seedDate
                },
                new ProductEnvelope
                {
                    Id = 8,
                    Code = "PEA",
                    Name = "PEA",
                    ProductCategoryId = 5,
                    LegalNatureId = 5,
                    DefaultTaxProfileId = 8,
                    IsIndividual = true,
                    SupportsBeneficiaryClause = false,
                    IsActive = true,
                    CreatedDate = seedDate
                },
                new ProductEnvelope
                {
                    Id = 9,
                    Code = "PREV",
                    Name = "Prévoyance collective",
                    ProductCategoryId = 4,
                    LegalNatureId = 4,
                    DefaultTaxProfileId = 9,
                    IsCollective = true,
                    RequiresInsuredPerson = true,
                    SupportsBeneficiaryClause = true,
                    IsActive = true,
                    CreatedDate = seedDate
                },
                new ProductEnvelope
                {
                    Id = 10,
                    Code = "DEP",
                    Name = "Dépendance",
                    ProductCategoryId = 4,
                    LegalNatureId = 1,
                    DefaultTaxProfileId = 10,
                    IsIndividual = true,
                    RequiresInsuredPerson = true,
                    SupportsBeneficiaryClause = true,
                    IsActive = true,
                    CreatedDate = seedDate
                },
                new ProductEnvelope
                {
                    Id = 11,
                    Code = "HCL",
                    Name = "Homme-clé",
                    ProductCategoryId = 4,
                    LegalNatureId = 1,
                    DefaultTaxProfileId = 11,
                    IsIndividual = false,
                    RequiresInsuredPerson = true,
                    SupportsBeneficiaryClause = true,
                    IsActive = true,
                    CreatedDate = seedDate
                },
                new ProductEnvelope
                {
                    Id = 12,
                    Code = "ART39",
                    Name = "Article 39",
                    ProductCategoryId = 2,
                    LegalNatureId = 4,
                    DefaultTaxProfileId = 12,
                    IsCollective = true,
                    SupportsBeneficiaryClause = true,
                    IsActive = true,
                    CreatedDate = seedDate
                }
            );
        }
    }
}
