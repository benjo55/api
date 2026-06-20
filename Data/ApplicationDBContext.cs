using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;
using api.Models.Configurations;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        // 🔹 Déclaration de tous les DbSet
        public DbSet<Person> Persons { get; set; }
        public DbSet<Insurer> Insurers { get; set; }
        public DbSet<Notary> Notaries { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<ProductFeature> ProductFeatures { get; set; }
        public DbSet<ProductTaxOverride> ProductTaxOverrides { get; set; }
        public DbSet<ProductManagementFeePolicy> ProductManagementFeePolicies { get; set; }
        public DbSet<ProductOperationFeePolicy> ProductOperationFeePolicies { get; set; }
        public DbSet<FeePolicy> FeePolicies { get; set; }
        public DbSet<ContractManagementFeeAccrual> ContractManagementFeeAccruals { get; set; }
        public DbSet<ContractSupportFeeApplication> ContractSupportFeeApplications { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<EntityHistory> EntityHistories { get; set; }
        public DbSet<BeneficiaryClause> BeneficiaryClauses { get; set; }
        public DbSet<BeneficiaryClausePerson> BeneficiaryClausePersons { get; set; }
        public DbSet<FieldDescription> FieldDescriptions { get; set; }
        public DbSet<FinancialSupport> FinancialSupports { get; set; }
        public DbSet<SupportValuation> SupportValuations { get; set; }
        public DbSet<SupportRegulation> SupportRegulations { get; set; }
        public DbSet<SupportRiskProfile> SupportRiskProfiles { get; set; }
        public DbSet<SupportDistribution> SupportDistributions { get; set; }
        public DbSet<ESGDetail> ESGDetails { get; set; }
        public DbSet<DistributionChannel> DistributionChannels { get; set; }
        public DbSet<ShareClass> ShareClasses { get; set; }
        public DbSet<FundLifeCycle> FundLifeCycles { get; set; }
        public DbSet<FundScenario> FundScenarios { get; set; }
        public DbSet<MarketingTarget> MarketingTargets { get; set; }
        public DbSet<MultilingualDocument> MultilingualDocuments { get; set; }
        public DbSet<TaxData> TaxDatas { get; set; }
        public DbSet<ClientTypeCompliance> ClientTypeCompliances { get; set; }
        public DbSet<SupportTechnical> SupportTechnicals { get; set; }
        public DbSet<SupportPortfolioLink> SupportPortfolioLinks { get; set; }
        public DbSet<SupportDocument> SupportDocuments { get; set; }
        public DbSet<SupportHistoricalData> SupportHistoricalDatas { get; set; }
        public DbSet<SupportFeeDetail> SupportFeeDetails { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<FinancialSupportAllocation> FinancialSupportAllocations { get; set; }
        public DbSet<ContractOption> ContractOptions { get; set; }
        public DbSet<ContractOptionType> ContractOptionTypes { get; set; }
        public DbSet<ContractInsuredPerson> ContractInsuredPersons { get; set; }
        public DbSet<Compartment> Compartments { get; set; }
        public DbSet<Operation> Operations { get; set; }
        public DbSet<WithdrawalDetail> WithdrawalDetails { get; set; }
        public DbSet<ArbitrageDetail> ArbitrageDetails { get; set; }
        public DbSet<AdvanceDetail> AdvanceDetails { get; set; }
        public DbSet<Advance> Advances { get; set; }
        public DbSet<AdvanceTransaction> AdvanceTransactions { get; set; }
        public DbSet<PaymentDetail> PaymentDetails { get; set; }
        public DbSet<OperationSupportAllocation> OperationSupportAllocations { get; set; }
        public DbSet<ContractSupportHolding> ContractSupportHoldings { get; set; }
        public DbSet<ContractValuation> ContractValuations { get; set; }

        public DbSet<SupportLookthroughAsset> SupportLookthroughAssets { get; set; }
        public DbSet<TaxProfile> TaxProfiles { get; set; }
        public DbSet<TaxRuleVersion> TaxRuleVersions { get; set; }
        public DbSet<TaxComputation> TaxComputations { get; set; }
        public DbSet<FiscalEvent> FiscalEvents { get; set; }
        public DbSet<TaxLaw> TaxLaws { get; set; }
        public DbSet<TaxGeneration> TaxGenerations { get; set; }
        public DbSet<ContractTaxState> ContractTaxStates { get; set; }
        public DbSet<PremiumLot> PremiumLots { get; set; }
        public DbSet<GainLot> GainLots { get; set; }
        public DbSet<PsHistory> PsHistoryItems { get; set; }
        public DbSet<TaxEvent> TaxEvents { get; set; }
        public DbSet<TaxCalculationAudit> TaxCalculationAudits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔹 Convention de nommage pluriel pour toutes les tables
            modelBuilder.Entity<Person>().ToTable("Persons");
            modelBuilder.Entity<Insurer>().ToTable("Insurers");
            modelBuilder.Entity<Notary>().ToTable("Notaries");
            modelBuilder.Entity<Contract>().ToTable("Contracts");
            modelBuilder.Entity<Contract>().Property(c => c.InitialPremium).HasPrecision(20, 7);
            modelBuilder.Entity<Contract>().Property(c => c.TotalPaidPremiums).HasPrecision(20, 7);
            modelBuilder.Entity<Contract>().Property(c => c.CurrentValue).HasPrecision(20, 7);
            modelBuilder.Entity<Contract>().Property(c => c.RedemptionValue).HasPrecision(20, 7);
            modelBuilder.Entity<Contract>().Property(c => c.EntryFeesRate).HasPrecision(5, 2);
            modelBuilder.Entity<Contract>().Property(c => c.ManagementFeesRate).HasPrecision(5, 2);
            modelBuilder.Entity<Contract>().Property(c => c.ExitFeesRate).HasPrecision(5, 2);
            modelBuilder.Entity<Contract>().Property(c => c.ScheduledPayment).HasPrecision(20, 7);
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<ProductType>().ToTable("ProductTypes");
            modelBuilder.Entity<ProductFeature>().ToTable("ProductFeatures");
            modelBuilder.Entity<ProductTaxOverride>().ToTable("ProductTaxOverrides");
            modelBuilder.Entity<ProductManagementFeePolicy>().ToTable("ProductManagementFeePolicies");
            modelBuilder.Entity<FeePolicy>().ToTable("FeePolicies");
            modelBuilder.Entity<ContractManagementFeeAccrual>().ToTable("ContractManagementFeeAccruals");
            modelBuilder.Entity<ContractSupportFeeApplication>().ToTable("ContractSupportFeeApplications");
            modelBuilder.Entity<Brand>().ToTable("Brands");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<UserRole>().ToTable("UserRoles");
            modelBuilder.Entity<Permission>().ToTable("Permissions");
            modelBuilder.Entity<RolePermission>().ToTable("RolePermissions");
            modelBuilder.Entity<EntityHistory>().ToTable("EntityHistories");
            modelBuilder.Entity<BeneficiaryClause>().ToTable("BeneficiaryClauses");
            modelBuilder.Entity<BeneficiaryClausePerson>().ToTable("BeneficiaryClausePersons");
            modelBuilder.Entity<FieldDescription>().ToTable("FieldDescriptions");
            modelBuilder.Entity<FieldDescription>()
                .HasIndex(f => new { f.EntityName, f.FieldName })
                .IsUnique();
            modelBuilder.Entity<FinancialSupport>().ToTable("FinancialSupports");
            modelBuilder.Entity<FinancialSupport>()
                .Property(fs => fs.SupportNature)
                .HasConversion<string>()
                .HasMaxLength(30);
            modelBuilder.Entity<ProductManagementFeePolicy>(entity =>
            {
                entity.HasOne(p => p.Product)
                    .WithOne(p => p.ManagementFeePolicy)
                    .HasForeignKey<ProductManagementFeePolicy>(p => p.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.ProductId)
                    .IsUnique();

                entity.Property(p => p.AnnualRate).HasPrecision(18, 5);
            });

            modelBuilder.Entity<ProductOperationFeePolicy>(entity =>
            {
                entity.HasOne(p => p.Product)
                    .WithMany()
                    .HasForeignKey(p => p.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => new { p.ProductId, p.FeeType, p.ApplyOn })
                    .IsUnique();

                entity.Property(p => p.Rate).HasPrecision(18, 5);
                entity.Property(p => p.FixedAmount).HasPrecision(18, 5);
            });

            modelBuilder.Entity<FeePolicy>(entity =>
            {
                entity.Property(p => p.Rate).HasPrecision(18, 5);
                entity.Property(p => p.FixedAmount).HasPrecision(18, 5);
                entity.Property(p => p.MinAmount).HasPrecision(18, 5);
                entity.Property(p => p.MaxAmount).HasPrecision(18, 5);

                entity.HasOne(p => p.Product)
                    .WithMany(p => p.FeePolicies)
                    .HasForeignKey(p => p.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Contract)
                    .WithMany(c => c.FeePolicies)
                    .HasForeignKey(p => p.ContractId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(p => p.Compartment)
                    .WithMany()
                    .HasForeignKey(p => p.CompartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.FinancialSupport)
                    .WithMany()
                    .HasForeignKey(p => p.FinancialSupportId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(p => new
                {
                    p.Category,
                    p.FeeType,
                    p.Scope,
                    p.ProductId,
                    p.ContractId,
                    p.CompartmentId,
                    p.FinancialSupportId,
                    p.Priority
                }).HasDatabaseName("IX_FeePolicies_Resolution");
            });

            modelBuilder.Entity<ProductType>(entity =>
            {
                entity.HasIndex(t => t.Code).IsUnique();

                entity.HasOne(t => t.DefaultTaxProfile)
                    .WithMany()
                    .HasForeignKey(t => t.DefaultTaxProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasOne(p => p.ProductType)
                    .WithMany(t => t.Products)
                    .HasForeignKey(p => p.ProductTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.TaxProfile)
                    .WithMany()
                    .HasForeignKey(p => p.TaxProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductFeature>(entity =>
            {
                entity.HasIndex(f => new { f.ProductId, f.FeatureKey, f.ValidFrom });

                entity.HasOne(f => f.Product)
                    .WithMany(p => p.Features)
                    .HasForeignKey(f => f.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductTaxOverride>(entity =>
            {
                entity.HasIndex(o => new { o.ProductId, o.ParameterKey, o.ValidFrom });

                entity.HasOne(o => o.Product)
                    .WithMany(p => p.TaxOverrides)
                    .HasForeignKey(o => o.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ContractManagementFeeAccrual>(entity =>
            {
                entity.HasOne(a => a.Contract)
                    .WithMany()
                    .HasForeignKey(a => a.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Support)
                    .WithMany()
                    .HasForeignKey(a => a.SupportId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Compartment)
                    .WithMany()
                    .HasForeignKey(a => a.CompartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(a => a.AccruedAmount).HasPrecision(20, 7);

                entity.HasIndex(a => new { a.ContractId, a.SupportId, a.CompartmentId })
                    .IsUnique()
                    .HasDatabaseName("UX_ContractManagementFeeAccrual_Contract_Support_Compartment");
            });

            modelBuilder.Entity<ContractSupportFeeApplication>(entity =>
            {
                entity.HasOne(f => f.Contract)
                    .WithMany()
                    .HasForeignKey(f => f.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.FeeOperation)
                    .WithMany()
                    .HasForeignKey(f => f.FeeOperationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.SourceOperation)
                    .WithMany()
                    .HasForeignKey(f => f.SourceOperationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Compartment)
                    .WithMany()
                    .HasForeignKey(f => f.CompartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Support)
                    .WithMany()
                    .HasForeignKey(f => f.SupportId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(f => f.BaseAmount).HasPrecision(20, 7);
                entity.Property(f => f.FeeAmount).HasPrecision(20, 7);
                entity.Property(f => f.FeeShares).HasPrecision(20, 7);
                entity.Property(f => f.NavUsed).HasPrecision(20, 7);
                entity.Property(f => f.PolicySource).HasMaxLength(100);

                entity.HasIndex(f => new { f.ContractId, f.EffectiveDate })
                    .HasDatabaseName("IX_ContractSupportFeeApplications_Contract_Date");

                entity.HasIndex(f => new { f.ContractId, f.CompartmentId, f.SupportId, f.FeeNature })
                    .HasDatabaseName("IX_ContractSupportFeeApplications_Contract_Compartment_Support_Nature");

                entity.HasIndex(f => f.FeeOperationId)
                    .HasDatabaseName("IX_ContractSupportFeeApplications_FeeOperation");
            });
            modelBuilder.Entity<SupportValuation>().ToTable("SupportValuations");
            modelBuilder.Entity<SupportRegulation>().ToTable("SupportRegulations");
            modelBuilder.Entity<SupportRiskProfile>().ToTable("SupportRiskProfiles");
            modelBuilder.Entity<SupportDistribution>().ToTable("SupportDistributions");
            modelBuilder.Entity<ESGDetail>().ToTable("ESGDetails");
            modelBuilder.Entity<DistributionChannel>().ToTable("DistributionChannels");
            modelBuilder.Entity<ShareClass>().ToTable("ShareClasses");
            modelBuilder.Entity<FundLifeCycle>().ToTable("FundLifeCycles");
            modelBuilder.Entity<FundScenario>().ToTable("FundScenarios");
            modelBuilder.Entity<MarketingTarget>().ToTable("MarketingTargets");
            modelBuilder.Entity<MultilingualDocument>().ToTable("MultilingualDocuments");
            modelBuilder.Entity<TaxData>().ToTable("TaxDatas");
            modelBuilder.Entity<ClientTypeCompliance>().ToTable("ClientTypeCompliances");
            modelBuilder.Entity<SupportTechnical>().ToTable("SupportTechnicals");
            modelBuilder.Entity<SupportPortfolioLink>().ToTable("SupportPortfolioLinks");
            modelBuilder.Entity<SupportDocument>().ToTable("SupportDocuments");
            modelBuilder.Entity<SupportHistoricalData>().ToTable("SupportHistoricalDatas");
            modelBuilder.Entity<SupportFeeDetail>().ToTable("SupportFeeDetails");
            modelBuilder.Entity<SupportLookthroughAsset>().ToTable("SupportLookthroughAssets");
            modelBuilder.Entity<TaxProfile>().ToTable("TaxProfiles");
            modelBuilder.Entity<TaxRuleVersion>().ToTable("TaxRuleVersions");
            modelBuilder.Entity<TaxComputation>().ToTable("TaxComputations");
            modelBuilder.Entity<FiscalEvent>().ToTable("FiscalEvents");
            modelBuilder.Entity<TaxLaw>().ToTable("TaxLaws");
            modelBuilder.Entity<TaxGeneration>().ToTable("TaxGenerations");
            modelBuilder.Entity<ContractTaxState>().ToTable("ContractTaxStates");
            modelBuilder.Entity<PremiumLot>().ToTable("PremiumLots");
            modelBuilder.Entity<GainLot>().ToTable("GainLots");
            modelBuilder.Entity<PsHistory>().ToTable("PsHistory");
            modelBuilder.Entity<TaxEvent>().ToTable("TaxEvents");
            modelBuilder.Entity<TaxCalculationAudit>().ToTable("TaxCalculationAudits");
            modelBuilder.Entity<Compartment>().ToTable("Compartments");

            modelBuilder.Entity<TaxRuleVersion>()
                .HasIndex(v => new { v.IsActive, v.EffectiveFrom });

            modelBuilder.Entity<TaxComputation>()
                .HasIndex(c => c.CreatedDate);

            modelBuilder.Entity<TaxComputation>()
                .HasOne(c => c.TaxProfile)
                .WithMany()
                .HasForeignKey(c => c.TaxProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaxComputation>()
                .HasOne(c => c.TaxRuleVersion)
                .WithMany()
                .HasForeignKey(c => c.TaxRuleVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FiscalEvent>()
                .HasOne(e => e.TaxComputation)
                .WithMany()
                .HasForeignKey(e => e.TaxComputationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaxLaw>()
                .HasIndex(x => new { x.CountryCode, x.EffectiveDateStart, x.EffectiveDateEnd });

            modelBuilder.Entity<TaxGeneration>()
                .HasIndex(x => new { x.ProductType, x.TaxRuleType, x.EffectiveDateStart, x.EffectiveDateEnd });

            modelBuilder.Entity<TaxGeneration>()
                .HasOne(x => x.TaxLaw)
                .WithMany()
                .HasForeignKey(x => x.TaxLawId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContractTaxState>()
                .HasIndex(x => x.ContractId)
                .IsUnique();

            modelBuilder.Entity<ContractTaxState>()
                .HasOne(x => x.Contract)
                .WithMany()
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PremiumLot>()
                .HasIndex(x => new { x.ContractTaxStateId, x.PaymentDate });

            modelBuilder.Entity<PremiumLot>()
                .HasOne(x => x.ContractTaxState)
                .WithMany(x => x.PremiumLots)
                .HasForeignKey(x => x.ContractTaxStateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PremiumLot>()
                .HasOne(x => x.TaxGeneration)
                .WithMany()
                .HasForeignKey(x => x.TaxGenerationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GainLot>()
                .HasIndex(x => new { x.ContractTaxStateId, x.GainDate });

            modelBuilder.Entity<GainLot>()
                .HasOne(x => x.ContractTaxState)
                .WithMany(x => x.GainLots)
                .HasForeignKey(x => x.ContractTaxStateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GainLot>()
                .HasOne(x => x.TaxGeneration)
                .WithMany()
                .HasForeignKey(x => x.TaxGenerationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PsHistory>()
                .HasIndex(x => new { x.ContractTaxStateId, x.LevyDate });

            modelBuilder.Entity<PsHistory>()
                .HasOne(x => x.ContractTaxState)
                .WithMany(x => x.PsHistoryItems)
                .HasForeignKey(x => x.ContractTaxStateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PsHistory>()
                .HasOne(x => x.GainLot)
                .WithMany()
                .HasForeignKey(x => x.GainLotId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaxEvent>()
                .HasIndex(x => new { x.ContractTaxStateId, x.EventDate });

            modelBuilder.Entity<TaxEvent>()
                .HasOne(x => x.ContractTaxState)
                .WithMany()
                .HasForeignKey(x => x.ContractTaxStateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaxEvent>()
                .HasOne(x => x.Operation)
                .WithMany()
                .HasForeignKey(x => x.OperationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaxEvent>()
                .HasOne(x => x.TaxComputation)
                .WithMany()
                .HasForeignKey(x => x.TaxComputationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaxCalculationAudit>()
                .HasIndex(x => new { x.TaxComputationId, x.CreatedDate });

            modelBuilder.Entity<TaxCalculationAudit>()
                .HasOne(x => x.TaxComputation)
                .WithMany()
                .HasForeignKey(x => x.TaxComputationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaxCalculationAudit>()
                .HasOne(x => x.ContractTaxState)
                .WithMany()
                .HasForeignKey(x => x.ContractTaxStateId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaxCalculationAudit>()
                .HasOne(x => x.TaxGeneration)
                .WithMany()
                .HasForeignKey(x => x.TaxGenerationId)
                .OnDelete(DeleteBehavior.SetNull);

            // 🔹 Operations
            modelBuilder.Entity<Operation>().ToTable("Operations");
            modelBuilder.Entity<Operation>().Property(o => o.Amount).HasPrecision(20, 7);

            modelBuilder.Entity<Operation>()
                .HasOne(o => o.Contract)
                .WithMany(c => c.Operations)
                .HasForeignKey(o => o.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Operation>()
                .HasOne(o => o.SourceOperation)
                .WithMany(o => o.GeneratedFeeOperations)
                .HasForeignKey(o => o.SourceOperationId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================================
            // 🔗 OperationSupportAllocation — Relations et contraintes
            // ==========================================================
            modelBuilder.Entity<OperationSupportAllocation>(entity =>
            {
                entity.ToTable("OperationSupportAllocations");

                // 🔹 Opération (1-N)
                entity.HasOne(osa => osa.Operation)
                    .WithMany(o => o.Allocations)
                    .HasForeignKey(osa => osa.OperationId)
                    .OnDelete(DeleteBehavior.Cascade); // cohérent : si l’opération est supprimée, ses allocations aussi

                // 🔹 Support financier (1-N)
                entity.HasOne(osa => osa.Support)
                    .WithMany()
                    .HasForeignKey(osa => osa.SupportId)
                    .OnDelete(DeleteBehavior.Restrict); // empêche la suppression d’un support utilisé

                // 🔹 Poche (1-N, désormais obligatoire)
                entity.HasOne(osa => osa.Compartment)
                    .WithMany()
                    .HasForeignKey(osa => osa.CompartmentId)
                    .IsRequired() // ✅ obligatoire : chaque allocation d’opération appartient à une poche
                    .OnDelete(DeleteBehavior.Restrict); // sécurité : empêche suppression poche avec historiques

                // 🔹 Précision des champs numériques
                entity.Property(o => o.Amount)
                    .HasPrecision(20, 7);

                entity.Property(o => o.NavAtOperation)
                    .HasPrecision(20, 7);

                entity.Property(o => o.Shares)
                    .HasPrecision(20, 7);

                // 🔹 Index logique (opération + support + poche + flow)
                // Permet SOURCE et TARGET sur le même support/poche dans une même opération.
                entity.HasIndex(o => new { o.OperationId, o.SupportId, o.CompartmentId, o.Flow })
                    .IsUnique()
                    .HasFilter(null)
                    .HasDatabaseName("UX_OSA_Operation_Support_Compartment_Flow");

            });

            modelBuilder.Entity<ContractSupportHolding>(entity =>
            {
                entity.ToTable("ContractSupportHoldings");

                entity.HasOne(h => h.Contract)
                    .WithMany(c => c.ContractSupportHoldings)
                    .HasForeignKey(h => h.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.Support)
                    .WithMany()
                    .HasForeignKey(h => h.SupportId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 🔥 AJOUT MANQUANT
                entity.HasOne(h => h.Compartment)
                    .WithMany()
                    .HasForeignKey(h => h.CompartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(h => h.Pru).HasPrecision(20, 7);
                entity.Property(h => h.TotalShares).HasPrecision(20, 7);
                entity.Property(h => h.TotalInvested).HasPrecision(20, 7);

                // 🔥 CORRECTION MAJEURE
                entity.HasIndex(h => new { h.ContractId, h.CompartmentId, h.SupportId })
                    .IsUnique()
                    .HasDatabaseName("UX_Holding_Contract_Compartment_Support");
            });


            // ==========================================================
            // 🔗 Relation Compartment ↔ FinancialSupportAllocation
            // ==========================================================
            modelBuilder.Entity<Compartment>(entity =>
            {
                entity.ToTable("Compartments");

                // 🔒 Un seul poche globale par contrat
                entity.HasIndex(c => new { c.ContractId, c.IsDefault })
                    .IsUnique()
                    .HasFilter("[IsDefault] = 1");

                // 🏷️ Label
                entity.Property(c => c.Label)
                    .HasMaxLength(100)
                    .IsRequired(); // 🔥 recommandé

                // 💰 Valeur
                entity.Property(c => c.CurrentValue)
                    .HasPrecision(20, 7);

                // 🔗 Relation explicite avec Contract (souvent oubliée)
                entity.HasOne(c => c.Contract)
                    .WithMany(c => c.Compartments)
                    .HasForeignKey(c => c.ContractId)
                    .OnDelete(DeleteBehavior.Cascade); // OK ici
            });

            // WithdrawalDetail
            modelBuilder.Entity<WithdrawalDetail>().ToTable("WithdrawalDetails");
            modelBuilder.Entity<WithdrawalDetail>().Property(w => w.GrossAmount).HasPrecision(20, 7);
            modelBuilder.Entity<WithdrawalDetail>().Property(w => w.ScheduleGroupId).HasMaxLength(64);
            modelBuilder.Entity<WithdrawalDetail>()
                .HasOne(d => d.Operation)
                .WithOne(o => o.WithdrawalDetail)
                .HasForeignKey<WithdrawalDetail>(d => d.OperationId);

            // ArbitrageDetail
            modelBuilder.Entity<ArbitrageDetail>().ToTable("ArbitrageDetails");
            modelBuilder.Entity<ArbitrageDetail>().Property(a => a.Percentage).HasPrecision(18, 4);
            modelBuilder.Entity<ArbitrageDetail>().Property(a => a.ScheduleGroupId).HasMaxLength(64);
            modelBuilder.Entity<ArbitrageDetail>()
                .HasOne(d => d.Operation)
                .WithOne(o => o.ArbitrageDetail)
                .HasForeignKey<ArbitrageDetail>(d => d.OperationId);

            // AdvanceDetail
            modelBuilder.Entity<AdvanceDetail>().ToTable("AdvanceDetails");
            modelBuilder.Entity<AdvanceDetail>().Property(a => a.Amount).HasPrecision(20, 7);
            modelBuilder.Entity<AdvanceDetail>().Property(a => a.InterestRate).HasPrecision(18, 4);
            modelBuilder.Entity<AdvanceDetail>().Property(a => a.TransactionType).HasConversion<string>().HasMaxLength(40);
            modelBuilder.Entity<AdvanceDetail>().Property(a => a.Comment).HasMaxLength(500);
            modelBuilder.Entity<AdvanceDetail>()
                .HasOne(d => d.Operation)
                .WithOne(o => o.AdvanceDetail)
                .HasForeignKey<AdvanceDetail>(d => d.OperationId);
            modelBuilder.Entity<AdvanceDetail>()
                .HasOne(d => d.Advance)
                .WithMany()
                .HasForeignKey(d => d.AdvanceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Advance>(entity =>
            {
                entity.ToTable("Advances");

                entity.HasOne(a => a.Contract)
                    .WithMany(c => c.Advances)
                    .HasForeignKey(a => a.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(a => a.AdvanceNumber)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(a => a.RequestedAmount).HasPrecision(20, 7);
                entity.Property(a => a.ApprovedAmount).HasPrecision(20, 7);
                entity.Property(a => a.OutstandingCapital).HasPrecision(20, 7);
                entity.Property(a => a.InterestRate).HasPrecision(18, 4);
                entity.Property(a => a.Reason).HasMaxLength(500);
                entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);
                entity.Property(a => a.RowVersion).IsRowVersion();

                entity.HasIndex(a => a.AdvanceNumber)
                    .IsUnique()
                    .HasDatabaseName("UX_Advances_AdvanceNumber");

                entity.HasIndex(a => new { a.ContractId, a.Status })
                    .HasDatabaseName("IX_Advances_Contract_Status");
            });

            modelBuilder.Entity<AdvanceTransaction>(entity =>
            {
                entity.ToTable("AdvanceTransactions");

                entity.HasOne(t => t.Advance)
                    .WithMany(a => a.Transactions)
                    .HasForeignKey(t => t.AdvanceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.Operation)
                    .WithMany()
                    .HasForeignKey(t => t.OperationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(t => t.Type).HasConversion<string>().HasMaxLength(40);
                entity.Property(t => t.Amount).HasPrecision(20, 7);
                entity.Property(t => t.Comment).HasMaxLength(500);

                entity.HasIndex(t => new { t.AdvanceId, t.OperationDate })
                    .HasDatabaseName("IX_AdvanceTransactions_Advance_Date");

                entity.HasIndex(t => t.OperationId)
                    .IsUnique()
                    .HasFilter("[OperationId] IS NOT NULL")
                    .HasDatabaseName("UX_AdvanceTransactions_OperationId");
            });

            // 🔹 PaymentDetail
            modelBuilder.Entity<PaymentDetail>().ToTable("PaymentDetails");
            modelBuilder.Entity<PaymentDetail>().Property(p => p.Amount).HasPrecision(20, 7);
            modelBuilder.Entity<PaymentDetail>().Property(p => p.ScheduleGroupId).HasMaxLength(64);
            modelBuilder.Entity<PaymentDetail>()
                .HasOne(d => d.Operation)
                .WithOne(o => o.PaymentDetail)
                .HasForeignKey<PaymentDetail>(d => d.OperationId);

            // ⚙️ Catalogue des options
            modelBuilder.Entity<ContractOptionType>().ToTable("ContractOptionTypes");
            modelBuilder.Entity<ContractOptionType>()
                .HasIndex(t => t.Code)
                .IsUnique();

            // 🔹 Configuration des relations Many-to-Many et composite keys
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<BeneficiaryClausePerson>()
                .HasKey(bcp => new { bcp.ClauseId, bcp.PersonId });

            modelBuilder.Entity<BeneficiaryClausePerson>()
                .HasOne(bcp => bcp.BeneficiaryClause)
                .WithMany(bc => bc.Beneficiaries)
                .HasForeignKey(bcp => bcp.ClauseId);

            modelBuilder.Entity<BeneficiaryClausePerson>()
                .HasOne(bcp => bcp.Person)
                .WithMany(p => p.BeneficiaryClausePersons)
                .HasForeignKey(bcp => bcp.PersonId);

            modelBuilder.Entity<FinancialSupport>()
                .HasIndex(fs => fs.ISIN)
                .IsUnique();

            // 📎 Document → Contract (optional, one-to-many)
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Contract)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.SetNull);

            // ==========================================================
            // 📊 FinancialSupportAllocation — Relations et contraintes
            // ==========================================================
            modelBuilder.Entity<FinancialSupportAllocation>(entity =>
            {
                entity.HasOne(fsa => fsa.Contract)
                    .WithMany(c => c.Supports)
                    .HasForeignKey(fsa => fsa.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(fsa => fsa.Support)
                    .WithMany()
                    .HasForeignKey(fsa => fsa.SupportId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fsa => fsa.Compartment)
                    .WithMany(c => c.Supports)
                    .HasForeignKey(fsa => fsa.CompartmentId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict); // 🔥 IMPORTANT

                entity.HasIndex(f => new { f.ContractId, f.CompartmentId, f.SupportId })
                    .IsUnique()
                    .HasDatabaseName("UX_FSA_Contract_Compartment_Support");

                entity.Property(f => f.AllocationPercentage).HasPrecision(18, 4);
                entity.Property(f => f.CurrentShares).HasPrecision(20, 7);
                entity.Property(f => f.CurrentAmount).HasPrecision(20, 7);
            });

            // 🧠 ContractOption → Contract & Type
            modelBuilder.Entity<ContractOption>()
                .HasOne(o => o.Contract)
                .WithMany(c => c.Options)
                .HasForeignKey(o => o.ContractId);

            modelBuilder.Entity<ContractOption>()
                .HasOne(o => o.ContractOptionType)
                .WithMany(t => t.ContractOptions)
                .HasForeignKey(o => o.ContractOptionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // 👥 ContractInsuredPerson → many-to-many Contract ↔ Person
            modelBuilder.Entity<ContractInsuredPerson>()
                .HasKey(x => new { x.ContractId, x.PersonId });

            modelBuilder.Entity<ContractInsuredPerson>()
                .HasOne(x => x.Contract)
                .WithMany(c => c.InsuredLinks)
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContractInsuredPerson>()
                .HasOne(x => x.Person)
                .WithMany()
                .HasForeignKey(x => x.PersonId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔹 Configurations avancées
            modelBuilder.ApplyConfiguration(new SupportDocumentConfiguration());
            modelBuilder.ApplyConfiguration(new SupportHistoricalDataConfiguration());
            modelBuilder.ApplyConfiguration(new SupportFeeDetailConfiguration());
            modelBuilder.ApplyConfiguration(new SupportLookthroughAssetConfiguration());

            // 📌 Seed du catalogue d’options
            Data.Seed.ContractOptionTypeSeeder.Seed(modelBuilder);
            // 📌 Seed des profils fiscaux par famille de contrat
            Data.Seed.TaxProfileSeeder.Seed(modelBuilder);
            // 📌 Seed des versions de règles fiscales
            Data.Seed.TaxRuleVersionSeeder.Seed(modelBuilder);
            // 📌 Seed des lois fiscales temporelles
            Data.Seed.TaxLawSeeder.Seed(modelBuilder);
            // 📌 Seed des générations fiscales temporelles
            Data.Seed.TaxGenerationSeeder.Seed(modelBuilder);
            // 📌 Seed des types de produit (AV, CAPI, PERIN, PERCOL, PERO, Madelin, Art83, PEA…)
            Data.Seed.ProductTypeSeeder.Seed(modelBuilder);
        }

        public override int SaveChanges()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Person && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    ((Person)entry.Entity).CreatedDate = DateTime.Now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    ((Person)entry.Entity).UpdatedDate = DateTime.Now;
                }
            }

            return base.SaveChanges();
        }
    }
}
