using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;
using api.Models.Configurations;
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
        public DbSet<PaymentDetail> PaymentDetails { get; set; }
        public DbSet<OperationSupportAllocation> OperationSupportAllocations { get; set; }
        public DbSet<ContractSupportHolding> ContractSupportHoldings { get; set; }
        public DbSet<ContractValuation> ContractValuations { get; set; }

        public DbSet<SupportLookthroughAsset> SupportLookthroughAssets { get; set; }

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
            modelBuilder.Entity<FinancialSupport>().ToTable("FinancialSupports");
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
            modelBuilder.Entity<Compartment>().ToTable("Compartments");

            // 🔹 Operations
            modelBuilder.Entity<Operation>().ToTable("Operations");
            modelBuilder.Entity<Operation>().Property(o => o.Amount).HasPrecision(20, 7);

            modelBuilder.Entity<Operation>()
                .HasOne(o => o.Contract)
                .WithMany(c => c.Operations)
                .HasForeignKey(o => o.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Operation>()
                .HasOne(o => o.Compartment)
                .WithMany()
                .HasForeignKey(o => o.CompartmentId)
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

                // 🔹 Compartiment (1-N, désormais obligatoire)
                entity.HasOne(osa => osa.Compartment)
                    .WithMany()
                    .HasForeignKey(osa => osa.CompartmentId)
                    .IsRequired() // ✅ obligatoire : chaque allocation d’opération appartient à un compartiment
                    .OnDelete(DeleteBehavior.Restrict); // sécurité : empêche suppression compartiment avec historiques

                // 🔹 Précision des champs numériques
                entity.Property(o => o.Amount)
                    .HasPrecision(20, 7);

                entity.Property(o => o.NavAtOperation)
                    .HasPrecision(20, 7);

                entity.Property(o => o.Shares)
                    .HasPrecision(20, 7);

                // 🔹 Index logique (opération + support + compartiment) pour éviter doublons internes
                entity.HasIndex(o => new { o.OperationId, o.SupportId, o.CompartmentId })
                    .IsUnique()
                    .HasDatabaseName("UX_OSA_Operation_Support_Compartment");

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

                // 🔒 Un seul compartiment global par contrat
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
            modelBuilder.Entity<WithdrawalDetail>()
                .HasOne(d => d.Operation)
                .WithOne(o => o.WithdrawalDetail)
                .HasForeignKey<WithdrawalDetail>(d => d.OperationId);

            // ArbitrageDetail
            modelBuilder.Entity<ArbitrageDetail>().ToTable("ArbitrageDetails");
            modelBuilder.Entity<ArbitrageDetail>().Property(a => a.Percentage).HasPrecision(18, 4);
            modelBuilder.Entity<ArbitrageDetail>()
                .HasOne(d => d.Operation)
                .WithOne(o => o.ArbitrageDetail)
                .HasForeignKey<ArbitrageDetail>(d => d.OperationId);

            // AdvanceDetail
            modelBuilder.Entity<AdvanceDetail>().ToTable("AdvanceDetails");
            modelBuilder.Entity<AdvanceDetail>().Property(a => a.Amount).HasPrecision(20, 7);
            modelBuilder.Entity<AdvanceDetail>().Property(a => a.InterestRate).HasPrecision(18, 4);
            modelBuilder.Entity<AdvanceDetail>()
                .HasOne(d => d.Operation)
                .WithOne(o => o.AdvanceDetail)
                .HasForeignKey<AdvanceDetail>(d => d.OperationId);

            // 🔹 PaymentDetail
            modelBuilder.Entity<PaymentDetail>().ToTable("PaymentDetails");
            modelBuilder.Entity<PaymentDetail>().Property(p => p.Amount).HasPrecision(20, 7);
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
