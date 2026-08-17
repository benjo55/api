using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;
using api.Models.Configurations;
using api.Models.Cmdb;
using api.Models.Enum;
using api.Models.Workflow;
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
        public DbSet<InsurerAuthorization> InsurerAuthorizations { get; set; }
        public DbSet<InsurerContactPoint> InsurerContactPoints { get; set; }
        public DbSet<InsurerSolvencyMetric> InsurerSolvencyMetrics { get; set; }
        public DbSet<Notary> Notaries { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<LegalNature> LegalNatures { get; set; }
        public DbSet<ProductEnvelope> ProductEnvelopes { get; set; }
        public DbSet<ProductVersion> ProductVersions { get; set; }
        public DbSet<ProductEligibilityRule> ProductEligibilityRules { get; set; }
        public DbSet<ProductOperationRule> ProductOperationRules { get; set; }
        public DbSet<ProductPaymentRule> ProductPaymentRules { get; set; }
        public DbSet<ProductFeeRule> ProductFeeRules { get; set; }
        public DbSet<ProductGuarantee> ProductGuarantees { get; set; }
        public DbSet<ProductManagementMode> ProductManagementModes { get; set; }
        public DbSet<ProductFinancialSupport> ProductFinancialSupports { get; set; }
        public DbSet<ProductDocument> ProductDocuments { get; set; }
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
        public DbSet<UserSecurityToken> UserSecurityTokens { get; set; }
        public DbSet<UserMfaFactor> UserMfaFactors { get; set; }
        public DbSet<AdminAuditEvent> AdminAuditEvents { get; set; }
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
        public DbSet<LegalDocumentDefinition> LegalDocumentDefinitions { get; set; }
        public DbSet<LegalDocumentRevision> LegalDocumentRevisions { get; set; }
        public DbSet<LegalDocumentNode> LegalDocumentNodes { get; set; }
        public DbSet<ClauseDefinition> ClauseDefinitions { get; set; }
        public DbSet<ClauseRevision> ClauseRevisions { get; set; }
        public DbSet<DocumentLayoutTemplate> DocumentLayoutTemplates { get; set; }
        public DbSet<DocumentArtifact> DocumentArtifacts { get; set; }
        public DbSet<ProductDocumentAssignment> ProductDocumentAssignments { get; set; }
        public DbSet<ContractDocumentInstance> ContractDocumentInstances { get; set; }
        public DbSet<DocumentAuditEvent> DocumentAuditEvents { get; set; }
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
        public DbSet<EuroFundConfiguration> EuroFundConfigurations { get; set; }
        public DbSet<EuroFundFinancialYear> EuroFundFinancialYears { get; set; }
        public DbSet<ReferenceRate> ReferenceRates { get; set; }
        public DbSet<EuroFundLot> EuroFundLots { get; set; }
        public DbSet<EuroFundLotMovement> EuroFundLotMovements { get; set; }
        public DbSet<EuroFundRevaluation> EuroFundRevaluations { get; set; }
        public DbSet<EuroFundRevaluationDetail> EuroFundRevaluationDetails { get; set; }

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
        public DbSet<CmdbImportRun> CmdbImportRuns { get; set; }
        public DbSet<ConfigurationItem> ConfigurationItems { get; set; }
        public DbSet<CartographyNodeLayout> CartographyNodeLayouts { get; set; }
        public DbSet<CartographyDomainDocument> CartographyDomainDocuments { get; set; }
        public DbSet<CartographyDomainDocumentSection> CartographyDomainDocumentSections { get; set; }
        public DbSet<ConfigurationItemApplicationProfile> ConfigurationItemApplicationProfiles { get; set; }
        public DbSet<CiAttributeDefinition> CiAttributeDefinitions { get; set; }
        public DbSet<CiAttributeValue> CiAttributeValues { get; set; }
        public DbSet<CmdbRelationshipType> CmdbRelationshipTypes { get; set; }
        public DbSet<CmdbRelationship> CmdbRelationships { get; set; }
        public DbSet<CiSupportAssignment> CiSupportAssignments { get; set; }
        public DbSet<IntegrationTechnology> IntegrationTechnologies { get; set; }
        public DbSet<ExchangePattern> ExchangePatterns { get; set; }
        public DbSet<IntegrationFlow> IntegrationFlows { get; set; }
        public DbSet<FlowRouteStep> FlowRouteSteps { get; set; }
        public DbSet<ProcessDefinition> ProcessDefinitions { get; set; }
        public DbSet<ProcessVersion> ProcessVersions { get; set; }
        public DbSet<WorkflowLane> WorkflowLanes { get; set; }
        public DbSet<WorkflowTask> WorkflowTasks { get; set; }
        public DbSet<WorkflowTransition> WorkflowTransitions { get; set; }
        public DbSet<ProcessInstance> ProcessInstances { get; set; }
        public DbSet<WorkflowTaskInstance> WorkflowTaskInstances { get; set; }
        public DbSet<WorkflowEventLog> WorkflowEventLogs { get; set; }
        public DbSet<Donor> Donors { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<DonationDonorSnapshot> DonationDonorSnapshots { get; set; }
        public DbSet<BeneficiaryOrganization> BeneficiaryOrganizations { get; set; }
        public DbSet<OrganizationBankAccount> OrganizationBankAccounts { get; set; }
        public DbSet<TaxReceipt> TaxReceipts { get; set; }
        public DbSet<TaxReceiptEmailHistory> TaxReceiptEmailHistory { get; set; }
        public DbSet<TaxReceiptDelivery> TaxReceiptDeliveries { get; set; }
        public DbSet<PaymentAttempt> PaymentAttempts { get; set; }
        public DbSet<PaymentWebhookInbox> PaymentWebhookInbox { get; set; }
        public DbSet<SubscriptionDraft> SubscriptionDrafts { get; set; }
        public DbSet<SubscriptionDraftAuditEvent> SubscriptionDraftAuditEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasSequence<long>("TaxReceiptNumberSequence")
                .StartsAt(1)
                .IncrementsBy(1);

            // 🔹 Convention de nommage pluriel pour toutes les tables
            modelBuilder.Entity<Person>(entity =>
            {
                entity.ToTable("Persons");
                entity.Property(p => p.Email1).HasMaxLength(254);
                entity.Property(p => p.Email2).HasMaxLength(254);
                entity.HasOne(p => p.User)
                    .WithOne(u => u.Person)
                    .HasForeignKey<Person>(p => p.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(p => p.UserId)
                    .IsUnique()
                    .HasFilter("[UserId] IS NOT NULL")
                    .HasDatabaseName("UX_Persons_UserId");
            });
            modelBuilder.Entity<Insurer>(entity =>
            {
                entity.ToTable("Insurers");
                entity.Property(i => i.Name).IsRequired();
                entity.Property(i => i.LegalName).HasMaxLength(250);
                entity.Property(i => i.TradeName).HasMaxLength(200);
                entity.Property(i => i.Acronym).HasMaxLength(40);
                entity.Property(i => i.InternalCode).HasMaxLength(80);
                entity.Property(i => i.LegalForm).HasMaxLength(120);
                entity.Property(i => i.InsurerType).HasMaxLength(60);
                entity.Property(i => i.IncorporationCountryCode).HasMaxLength(2);
                entity.Property(i => i.Siren).HasMaxLength(9);
                entity.Property(i => i.HeadquartersSiret).HasMaxLength(14);
                entity.Property(i => i.RcsCity).HasMaxLength(120);
                entity.Property(i => i.RcsNumber).HasMaxLength(120);
                entity.Property(i => i.VatNumber).HasMaxLength(40);
                entity.Property(i => i.Lei).HasMaxLength(20);
                entity.Property(i => i.ApeNafCode).HasMaxLength(20);
                entity.Property(i => i.HomeCountryCode).HasMaxLength(2);
                entity.Property(i => i.SupervisoryAuthorityName).HasMaxLength(200);
                entity.Property(i => i.SupervisoryAuthorityCountryCode).HasMaxLength(2);
                entity.Property(i => i.SupervisoryRegisterName).HasMaxLength(200);
                entity.Property(i => i.SupervisoryRegisterId).HasMaxLength(120);
                entity.Property(i => i.EiopaRegisterId).HasMaxLength(120);
                entity.Property(i => i.ExerciseRegime).HasMaxLength(60);
                entity.Property(i => i.RegulatoryStatus).HasMaxLength(60);
                entity.Property(i => i.ShortDescription).HasMaxLength(500);
                entity.Property(i => i.AssetsUnderManagement).HasPrecision(20, 2);
                entity.Property(i => i.ParentLei).HasMaxLength(20);
                entity.Property(i => i.UltimateParentLei).HasMaxLength(20);
                entity.Property(i => i.OwnershipPercentage).HasPrecision(5, 2);
                entity.Property(i => i.RatingAgency).HasMaxLength(120);
                entity.Property(i => i.Rating).HasMaxLength(40);
                entity.Property(i => i.RatingOutlook).HasMaxLength(80);
                entity.Property(i => i.DataSourceType).HasMaxLength(60);
                entity.Property(i => i.SourceName).HasMaxLength(160);
                entity.Property(i => i.SourceReference).HasMaxLength(160);
                entity.Property(i => i.VerifiedBy).HasMaxLength(120);
                entity.Property(i => i.VerificationStatus).HasMaxLength(60);

                entity.HasIndex(i => i.Siren)
                    .IsUnique()
                    .HasFilter("[Siren] IS NOT NULL")
                    .HasDatabaseName("UX_Insurers_Siren");
                entity.HasIndex(i => i.Lei)
                    .IsUnique()
                    .HasFilter("[Lei] IS NOT NULL")
                    .HasDatabaseName("UX_Insurers_Lei");
                entity.HasIndex(i => i.InternalCode)
                    .HasDatabaseName("IX_Insurers_InternalCode");
                entity.HasIndex(i => i.RegulatoryStatus)
                    .HasDatabaseName("IX_Insurers_RegulatoryStatus");

                entity.HasMany(i => i.Authorizations)
                    .WithOne(a => a.Insurer)
                    .HasForeignKey(a => a.InsurerId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(i => i.ContactPoints)
                    .WithOne(c => c.Insurer)
                    .HasForeignKey(c => c.InsurerId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(i => i.SolvencyMetrics)
                    .WithOne(s => s.Insurer)
                    .HasForeignKey(s => s.InsurerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<InsurerAuthorization>(entity =>
            {
                entity.ToTable("InsurerAuthorizations");
                entity.Property(a => a.AuthorityName).HasMaxLength(200);
                entity.Property(a => a.AuthorityCountryCode).HasMaxLength(2);
                entity.Property(a => a.RegisterName).HasMaxLength(200);
                entity.Property(a => a.RegisterReference).HasMaxLength(120);
                entity.Property(a => a.AuthorizationType).HasMaxLength(80);
                entity.Property(a => a.InsuranceBranchCode).HasMaxLength(40);
                entity.Property(a => a.InsuranceBranchLabel).HasMaxLength(200);
                entity.Property(a => a.BusinessCategory).HasMaxLength(40);
                entity.Property(a => a.HostCountryCode).HasMaxLength(2);
                entity.Property(a => a.ExerciseRegime).HasMaxLength(60);
                entity.Property(a => a.Status).HasMaxLength(60);
                entity.HasIndex(a => new { a.InsurerId, a.HostCountryCode, a.InsuranceBranchCode })
                    .HasDatabaseName("IX_InsurerAuthorizations_Insurer_Country_Branch");
            });
            modelBuilder.Entity<InsurerContactPoint>(entity =>
            {
                entity.ToTable("InsurerContactPoints");
                entity.Property(c => c.ContactType).HasMaxLength(60);
                entity.Property(c => c.Label).HasMaxLength(160);
                entity.Property(c => c.DepartmentName).HasMaxLength(160);
                entity.Property(c => c.ContactName).HasMaxLength(160);
                entity.Property(c => c.AddressLine1).HasMaxLength(250);
                entity.Property(c => c.AddressLine2).HasMaxLength(250);
                entity.Property(c => c.PostalCode).HasMaxLength(30);
                entity.Property(c => c.City).HasMaxLength(120);
                entity.Property(c => c.Region).HasMaxLength(120);
                entity.Property(c => c.CountryCode).HasMaxLength(2);
                entity.Property(c => c.Phone).HasMaxLength(40);
                entity.Property(c => c.Email).HasMaxLength(254);
                entity.HasIndex(c => new { c.InsurerId, c.ContactType, c.IsPrimary })
                    .HasDatabaseName("IX_InsurerContactPoints_Insurer_Type_Primary");
            });
            modelBuilder.Entity<InsurerSolvencyMetric>(entity =>
            {
                entity.ToTable("InsurerSolvencyMetrics");
                entity.Property(s => s.EligibleOwnFunds).HasPrecision(20, 2);
                entity.Property(s => s.SolvencyCapitalRequirement).HasPrecision(20, 2);
                entity.Property(s => s.ScrCoverageRatio).HasPrecision(9, 4);
                entity.Property(s => s.MinimumCapitalRequirement).HasPrecision(20, 2);
                entity.Property(s => s.McrCoverageRatio).HasPrecision(9, 4);
                entity.Property(s => s.Currency).HasMaxLength(3);
                entity.HasIndex(s => new { s.InsurerId, s.ReportingYear, s.IsGroupReport })
                    .HasDatabaseName("IX_InsurerSolvencyMetrics_Insurer_Year_Group");
            });
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
            modelBuilder.Entity<ProductCategory>().ToTable("ProductCategories");
            modelBuilder.Entity<LegalNature>().ToTable("LegalNatures");
            modelBuilder.Entity<ProductEnvelope>().ToTable("ProductEnvelopes");
            modelBuilder.Entity<ProductVersion>().ToTable("ProductVersions");
            modelBuilder.Entity<ProductEligibilityRule>().ToTable("ProductEligibilityRules");
            modelBuilder.Entity<ProductOperationRule>().ToTable("ProductOperationRules");
            modelBuilder.Entity<ProductPaymentRule>().ToTable("ProductPaymentRules");
            modelBuilder.Entity<ProductFeeRule>().ToTable("ProductFeeRules");
            modelBuilder.Entity<ProductGuarantee>().ToTable("ProductGuarantees");
            modelBuilder.Entity<ProductManagementMode>().ToTable("ProductManagementModes");
            modelBuilder.Entity<ProductFinancialSupport>().ToTable("ProductFinancialSupports");
            modelBuilder.Entity<ProductDocument>().ToTable("ProductDocuments");
            modelBuilder.Entity<ProductType>().ToTable("ProductTypes");
            modelBuilder.Entity<ProductFeature>().ToTable("ProductFeatures");
            modelBuilder.Entity<ProductTaxOverride>().ToTable("ProductTaxOverrides");
            modelBuilder.Entity<ProductManagementFeePolicy>().ToTable("ProductManagementFeePolicies");
            modelBuilder.Entity<FeePolicy>().ToTable("FeePolicies");
            modelBuilder.Entity<ContractManagementFeeAccrual>().ToTable("ContractManagementFeeAccruals");
            modelBuilder.Entity<ContractSupportFeeApplication>().ToTable("ContractSupportFeeApplications");
            modelBuilder.Entity<Brand>().ToTable("Brands");
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Username).HasMaxLength(100).IsRequired();
                entity.Property(u => u.NormalizedUsername).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Email).HasMaxLength(254).IsRequired();
                entity.Property(u => u.NormalizedEmail).HasMaxLength(254).IsRequired();
                entity.Property(u => u.PhoneNumber).HasMaxLength(32).IsRequired();
                entity.Property(u => u.CreatedDate).IsRequired();
                entity.Property(u => u.EmailConfirmed).IsRequired();
                entity.Property(u => u.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
                entity.Property(u => u.SuspensionReason).HasMaxLength(500);
                entity.Property(u => u.RevocationReason).HasMaxLength(500);
                entity.Property(u => u.RowVersion).IsRowVersion();

                entity.HasIndex(u => u.NormalizedUsername)
                    .IsUnique()
                    .HasDatabaseName("UX_Users_NormalizedUsername");

                entity.HasIndex(u => u.NormalizedEmail)
                    .IsUnique()
                    .HasDatabaseName("UX_Users_NormalizedEmail");

                entity.HasIndex(u => new { u.Status, u.EmailConfirmed })
                    .HasDatabaseName("IX_Users_Status_EmailConfirmed");
            });
            modelBuilder.Entity<UserSecurityToken>(entity =>
            {
                entity.ToTable("UserSecurityTokens");
                entity.Property(t => t.TokenType).HasMaxLength(40).IsRequired();
                entity.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
                entity.Property(t => t.CreatedByIpAddress).HasMaxLength(64);
                entity.Property(t => t.CreatedAt).IsRequired();
                entity.Property(t => t.ExpiresAt).IsRequired();

                entity.HasOne(t => t.User)
                    .WithMany(u => u.SecurityTokens)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(t => new { t.UserId, t.TokenType, t.TokenHash })
                    .HasDatabaseName("IX_UserSecurityTokens_User_Type_Hash");

                entity.HasIndex(t => new { t.TokenType, t.ExpiresAt })
                    .HasDatabaseName("IX_UserSecurityTokens_Type_ExpiresAt");
            });
            modelBuilder.Entity<UserMfaFactor>(entity =>
            {
                entity.ToTable("UserMfaFactors");
                entity.Property(f => f.FactorType).HasMaxLength(40).IsRequired();
                entity.Property(f => f.DisplayName).HasMaxLength(120).IsRequired();
                entity.Property(f => f.ProtectedSecret).IsRequired();
                entity.Property(f => f.CreatedAt).IsRequired();

                entity.HasOne(f => f.User)
                    .WithMany(u => u.MfaFactors)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(f => new { f.UserId, f.FactorType, f.RevokedAt })
                    .HasDatabaseName("IX_UserMfaFactors_User_Type_RevokedAt");
            });
            modelBuilder.Entity<AdminAuditEvent>(entity =>
            {
                entity.ToTable("AdminAuditEvents");
                entity.Property(e => e.Action).HasMaxLength(80).IsRequired();
                entity.Property(e => e.ActingUsername).HasMaxLength(100);
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.Property(e => e.ResultCode).HasMaxLength(60);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasIndex(e => new { e.TargetUserId, e.CreatedAt })
                    .HasDatabaseName("IX_AdminAuditEvents_TargetUser_Date");
                entity.HasIndex(e => new { e.Action, e.CreatedAt })
                    .HasDatabaseName("IX_AdminAuditEvents_Action_Date");
            });
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.Property(r => r.RoleCode).HasMaxLength(50).IsRequired();
                entity.Property(r => r.RoleName).HasMaxLength(100).IsRequired();
                entity.HasIndex(r => r.RoleCode)
                    .IsUnique()
                    .HasDatabaseName("UX_Roles_RoleCode")
                    .HasFilter("[RoleCode] <> ''");
            });
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

            modelBuilder.Entity<EuroFundConfiguration>(entity =>
            {
                entity.ToTable("EuroFundConfigurations");
                entity.Property(e => e.AccrualMethod).HasConversion<string>().HasMaxLength(40);
                entity.Property(e => e.ProvisionalRateMethod).HasConversion<string>().HasMaxLength(40);
                entity.Property(e => e.EarlyExitRateMethod).HasConversion<string>().HasMaxLength(40);
                entity.Property(e => e.LotConsumptionMethod).HasConversion<string>().HasMaxLength(40);
                entity.Property(e => e.ValueDateRule).HasConversion<string>().HasMaxLength(40);
                entity.Property(e => e.RateNature).HasConversion<string>().HasMaxLength(40);
                entity.Property(e => e.ManagementFeeTreatment).HasConversion<string>().HasMaxLength(40);
                entity.HasOne(e => e.FinancialSupport)
                    .WithMany()
                    .HasForeignKey(e => e.FinancialSupportId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.FinancialSupportId)
                    .IsUnique()
                    .HasDatabaseName("UX_EuroFundConfigurations_FinancialSupport");
            });

            modelBuilder.Entity<EuroFundFinancialYear>(entity =>
            {
                entity.ToTable("EuroFundFinancialYears");
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
                entity.Property(e => e.RateNature).HasConversion<string>().HasMaxLength(40);
                entity.HasOne(e => e.FinancialSupport)
                    .WithMany()
                    .HasForeignKey(e => e.FinancialSupportId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.FinancialSupportId, e.Year })
                    .IsUnique()
                    .HasDatabaseName("UX_EuroFundFinancialYears_Fund_Year");
            });

            modelBuilder.Entity<ReferenceRate>(entity =>
            {
                entity.ToTable("ReferenceRates");
                entity.Property(e => e.RateType).HasConversion<string>().HasMaxLength(30);
                entity.Property(e => e.Source).HasMaxLength(120);
                entity.HasIndex(e => new { e.RateType, e.RateDate, e.Source })
                    .IsUnique()
                    .HasDatabaseName("UX_ReferenceRates_Type_Date_Source");
            });

            modelBuilder.Entity<EuroFundLot>(entity =>
            {
                entity.ToTable("EuroFundLots");
                entity.Property(e => e.BonusRuleId).HasMaxLength(80);
                entity.HasOne(e => e.Contract)
                    .WithMany()
                    .HasForeignKey(e => e.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.FinancialSupport)
                    .WithMany()
                    .HasForeignKey(e => e.FinancialSupportId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.SourceOperation)
                    .WithMany()
                    .HasForeignKey(e => e.SourceOperationId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.ContractId, e.FinancialSupportId })
                    .HasDatabaseName("IX_EuroFundLots_Contract_Fund");
                entity.HasIndex(e => e.ValueDate)
                    .HasDatabaseName("IX_EuroFundLots_ValueDate");
                entity.HasIndex(e => e.SourceOperationId)
                    .HasDatabaseName("IX_EuroFundLots_SourceOperation");
                entity.HasIndex(e => new { e.SourceOperationId, e.ContractId, e.FinancialSupportId })
                    .IsUnique()
                    .HasFilter("[SourceOperationId] IS NOT NULL")
                    .HasDatabaseName("UX_EuroFundLots_SourceOperation_Contract_Fund");
            });

            modelBuilder.Entity<EuroFundLotMovement>(entity =>
            {
                entity.ToTable("EuroFundLotMovements");
                entity.Property(e => e.MovementType).HasConversion<string>().HasMaxLength(40);
                entity.HasOne(e => e.EuroFundLot)
                    .WithMany(l => l.Movements)
                    .HasForeignKey(e => e.EuroFundLotId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Operation)
                    .WithMany()
                    .HasForeignKey(e => e.OperationId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.ContractId, e.FinancialSupportId, e.MovementDate })
                    .HasDatabaseName("IX_EuroFundLotMovements_Contract_Fund_Date");
                entity.HasIndex(e => e.OperationId)
                    .HasDatabaseName("IX_EuroFundLotMovements_Operation");
            });

            modelBuilder.Entity<EuroFundRevaluation>(entity =>
            {
                entity.ToTable("EuroFundRevaluations");
                entity.HasOne(e => e.Operation)
                    .WithMany()
                    .HasForeignKey(e => e.OperationId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Contract)
                    .WithMany()
                    .HasForeignKey(e => e.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.FinancialSupport)
                    .WithMany()
                    .HasForeignKey(e => e.FinancialSupportId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.ContractId, e.FinancialSupportId, e.FinancialYear })
                    .IsUnique()
                    .HasDatabaseName("UX_EuroFundRevaluations_Contract_Fund_Year");
                entity.HasIndex(e => e.OperationId)
                    .IsUnique()
                    .HasDatabaseName("UX_EuroFundRevaluations_Operation");
            });

            modelBuilder.Entity<EuroFundRevaluationDetail>(entity =>
            {
                entity.ToTable("EuroFundRevaluationDetails");
                entity.HasOne(e => e.EuroFundRevaluation)
                    .WithMany(r => r.Details)
                    .HasForeignKey(e => e.EuroFundRevaluationId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.EuroFundLot)
                    .WithMany()
                    .HasForeignKey(e => e.EuroFundLotId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.EuroFundRevaluationId, e.PeriodStart })
                    .HasDatabaseName("IX_EuroFundRevaluationDetails_Revaluation_Period");
            });
            modelBuilder.Entity<ProductManagementFeePolicy>(entity =>
            {
                entity.HasOne(p => p.Product)
                    .WithOne(p => p.ManagementFeePolicy)
                    .HasForeignKey<ProductManagementFeePolicy>(p => p.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.ProductVersion)
                    .WithMany()
                    .HasForeignKey(p => p.ProductVersionId)
                    .OnDelete(DeleteBehavior.Restrict);

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

                entity.HasOne(p => p.ProductVersion)
                    .WithMany()
                    .HasForeignKey(p => p.ProductVersionId)
                    .OnDelete(DeleteBehavior.Restrict);

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

                entity.HasOne(p => p.ProductVersion)
                    .WithMany()
                    .HasForeignKey(p => p.ProductVersionId)
                    .OnDelete(DeleteBehavior.Restrict);

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
                    p.ProductVersionId,
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

            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.HasIndex(x => x.Code).IsUnique();
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            });

            modelBuilder.Entity<LegalNature>(entity =>
            {
                entity.HasIndex(x => x.Code).IsUnique();
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            });

            modelBuilder.Entity<ProductEnvelope>(entity =>
            {
                entity.HasIndex(x => x.Code).IsUnique();
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();

                entity.HasOne(x => x.ProductCategory)
                    .WithMany(x => x.ProductEnvelopes)
                    .HasForeignKey(x => x.ProductCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.LegalNature)
                    .WithMany(x => x.ProductEnvelopes)
                    .HasForeignKey(x => x.LegalNatureId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.DefaultTaxProfile)
                    .WithMany()
                    .HasForeignKey(x => x.DefaultTaxProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.ProductCode).HasMaxLength(50).IsRequired();
                entity.Property(p => p.ProductName).HasMaxLength(200).IsRequired();
                entity.Property(p => p.CommercialName).HasMaxLength(200);
                entity.Property(p => p.Description).HasMaxLength(2000);
                entity.HasIndex(p => new { p.InsurerId, p.ProductCode })
                    .IsUnique()
                    .HasFilter("[InsurerId] IS NOT NULL");

                entity.HasOne(p => p.ProductEnvelope)
                    .WithMany(e => e.Products)
                    .HasForeignKey(p => p.ProductEnvelopeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.ProductType)
                    .WithMany(t => t.Products)
                    .HasForeignKey(p => p.ProductTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.TaxProfile)
                    .WithMany()
                    .HasForeignKey(p => p.TaxProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductVersion>(entity =>
            {
                entity.Property(x => x.VersionCode).HasMaxLength(50).IsRequired();
                entity.Property(x => x.VersionName).HasMaxLength(200);
                entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
                entity.HasIndex(x => new { x.ProductId, x.VersionCode }).IsUnique();
                entity.HasIndex(x => new { x.ProductId, x.Status, x.EffectiveFrom, x.EffectiveTo });

                entity.HasOne(x => x.Product)
                    .WithMany(x => x.Versions)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.TaxProfile)
                    .WithMany()
                    .HasForeignKey(x => x.TaxProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Contract>(entity =>
            {
                entity.HasOne(c => c.ProductVersion)
                    .WithMany(v => v.Contracts)
                    .HasForeignKey(c => c.ProductVersionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductEligibilityRule>(entity =>
            {
                entity.HasIndex(x => new { x.ProductVersionId, x.RuleType });
                entity.HasOne(x => x.ProductVersion)
                    .WithMany(x => x.EligibilityRules)
                    .HasForeignKey(x => x.ProductVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductOperationRule>(entity =>
            {
                entity.HasIndex(x => new { x.ProductVersionId, x.OperationType }).IsUnique();
                entity.HasOne(x => x.ProductVersion)
                    .WithMany(x => x.OperationRules)
                    .HasForeignKey(x => x.ProductVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductPaymentRule>(entity =>
            {
                entity.HasIndex(x => new { x.ProductVersionId, x.PaymentType, x.Frequency }).IsUnique();
                entity.HasOne(x => x.ProductVersion)
                    .WithMany(x => x.PaymentRules)
                    .HasForeignKey(x => x.ProductVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductFeeRule>(entity =>
            {
                entity.HasIndex(x => new { x.ProductVersionId, x.FeeType, x.EffectiveFrom, x.EffectiveTo });
                entity.HasOne(x => x.ProductVersion)
                    .WithMany(x => x.FeeRules)
                    .HasForeignKey(x => x.ProductVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductGuarantee>(entity =>
            {
                entity.HasIndex(x => new { x.ProductVersionId, x.GuaranteeType });
                entity.HasOne(x => x.ProductVersion)
                    .WithMany(x => x.Guarantees)
                    .HasForeignKey(x => x.ProductVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductManagementMode>(entity =>
            {
                entity.HasIndex(x => new { x.ProductVersionId, x.ManagementModeType }).IsUnique();
                entity.HasOne(x => x.ProductVersion)
                    .WithMany(x => x.ManagementModes)
                    .HasForeignKey(x => x.ProductVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductFinancialSupport>(entity =>
            {
                entity.HasIndex(x => new { x.ProductVersionId, x.FinancialSupportId }).IsUnique();
                entity.HasOne(x => x.ProductVersion)
                    .WithMany(x => x.FinancialSupports)
                    .HasForeignKey(x => x.ProductVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.FinancialSupport)
                    .WithMany()
                    .HasForeignKey(x => x.FinancialSupportId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductDocument>(entity =>
            {
                entity.HasIndex(x => new { x.ProductVersionId, x.DocumentType, x.IsCurrent });
                entity.HasOne(x => x.ProductVersion)
                    .WithMany(x => x.Documents)
                    .HasForeignKey(x => x.ProductVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductFeature>(entity =>
            {
                entity.HasIndex(f => new { f.ProductId, f.FeatureKey, f.ValidFrom });

                entity.HasOne(f => f.Product)
                    .WithMany(p => p.Features)
                    .HasForeignKey(f => f.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.ProductVersion)
                    .WithMany()
                    .HasForeignKey(f => f.ProductVersionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductTaxOverride>(entity =>
            {
                entity.HasIndex(o => new { o.ProductId, o.ParameterKey, o.ValidFrom });

                entity.HasOne(o => o.Product)
                    .WithMany(p => p.TaxOverrides)
                    .HasForeignKey(o => o.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(o => o.ProductVersion)
                    .WithMany()
                    .HasForeignKey(o => o.ProductVersionId)
                    .OnDelete(DeleteBehavior.Restrict);
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

            modelBuilder.Entity<Permission>()
                .Property(p => p.PermissionCode)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Origin)
                    .HasConversion<int>()
                    .HasDefaultValue(UserOrigin.Life)
                    .IsRequired();
            });

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.PermissionCode)
                .IsUnique()
                .HasDatabaseName("UX_Permissions_PermissionCode")
                .HasFilter("[PermissionCode] <> ''");

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
            modelBuilder.ApplyConfiguration(new LegalDocumentDefinitionConfiguration());
            modelBuilder.ApplyConfiguration(new LegalDocumentRevisionConfiguration());
            modelBuilder.ApplyConfiguration(new LegalDocumentNodeConfiguration());
            modelBuilder.ApplyConfiguration(new ClauseDefinitionConfiguration());
            modelBuilder.ApplyConfiguration(new ClauseRevisionConfiguration());
            modelBuilder.ApplyConfiguration(new DocumentLayoutTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new DocumentArtifactConfiguration());
            modelBuilder.ApplyConfiguration(new ProductDocumentAssignmentConfiguration());
            modelBuilder.ApplyConfiguration(new ContractDocumentInstanceConfiguration());
            modelBuilder.ApplyConfiguration(new DocumentAuditEventConfiguration());
            modelBuilder.ApplyConfiguration(new CmdbImportRunConfiguration());
            modelBuilder.ApplyConfiguration(new ConfigurationItemConfiguration());
            modelBuilder.ApplyConfiguration(new CartographyNodeLayoutConfiguration());
            modelBuilder.ApplyConfiguration(new CartographyDomainDocumentConfiguration());
            modelBuilder.ApplyConfiguration(new CartographyDomainDocumentSectionConfiguration());
            modelBuilder.ApplyConfiguration(new ConfigurationItemApplicationProfileConfiguration());
            modelBuilder.ApplyConfiguration(new CiAttributeDefinitionConfiguration());
            modelBuilder.ApplyConfiguration(new CiAttributeValueConfiguration());
            modelBuilder.ApplyConfiguration(new CmdbRelationshipTypeConfiguration());
            modelBuilder.ApplyConfiguration(new CmdbRelationshipConfiguration());
            modelBuilder.ApplyConfiguration(new CiSupportAssignmentConfiguration());
            modelBuilder.ApplyConfiguration(new IntegrationTechnologyConfiguration());
            modelBuilder.ApplyConfiguration(new ExchangePatternConfiguration());
            modelBuilder.ApplyConfiguration(new IntegrationFlowConfiguration());
            modelBuilder.ApplyConfiguration(new FlowRouteStepConfiguration());
            modelBuilder.ApplyConfiguration(new ProcessDefinitionConfiguration());
            modelBuilder.ApplyConfiguration(new ProcessVersionConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowLaneConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowTaskConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowTransitionConfiguration());
            modelBuilder.ApplyConfiguration(new ProcessInstanceConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowTaskInstanceConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowEventLogConfiguration());
            modelBuilder.ApplyConfiguration(new DonorConfiguration());
            modelBuilder.ApplyConfiguration(new DonationConfiguration());
            modelBuilder.ApplyConfiguration(new DonationDonorSnapshotConfiguration());
            modelBuilder.ApplyConfiguration(new BeneficiaryOrganizationConfiguration());
            modelBuilder.ApplyConfiguration(new OrganizationBankAccountConfiguration());
            modelBuilder.ApplyConfiguration(new TaxReceiptConfiguration());
            modelBuilder.ApplyConfiguration(new TaxReceiptEmailHistoryConfiguration());
            modelBuilder.ApplyConfiguration(new TaxReceiptDeliveryConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentAttemptConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentWebhookInboxConfiguration());
            modelBuilder.Entity<SubscriptionDraft>(entity =>
            {
                entity.ToTable("SubscriptionDrafts");
                entity.Property(d => d.CurrentStep).HasMaxLength(40).IsRequired();
                entity.Property(d => d.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
                entity.Property(d => d.ProductType).HasConversion<int?>();
                entity.Property(d => d.ProjectDataJson).HasColumnType("nvarchar(max)");
                entity.Property(d => d.SituationDataJson).HasColumnType("nvarchar(max)");
                entity.Property(d => d.InvestorProfileDataJson).HasColumnType("nvarchar(max)");
                entity.Property(d => d.RecommendationDataJson).HasColumnType("nvarchar(max)");
                entity.Property(d => d.InvestmentDataJson).HasColumnType("nvarchar(max)");
                entity.Property(d => d.ProtectionDataJson).HasColumnType("nvarchar(max)");
                entity.Property(d => d.StepStatusesJson).HasColumnType("nvarchar(max)");
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(d => d.Product)
                    .WithMany()
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(d => new { d.UserId, d.Status, d.UpdatedAt })
                    .HasDatabaseName("IX_SubscriptionDrafts_User_Status_UpdatedAt");
            });

            modelBuilder.Entity<SubscriptionDraftAuditEvent>(entity =>
            {
                entity.ToTable("SubscriptionDraftAuditEvents");
                entity.Property(e => e.EventType).HasMaxLength(80).IsRequired();
                entity.Property(e => e.StepKey).HasMaxLength(40);
                entity.Property(e => e.RulesVersion).HasMaxLength(40).IsRequired();
                entity.HasOne(e => e.SubscriptionDraft)
                    .WithMany(d => d.AuditEvents)
                    .HasForeignKey(e => e.SubscriptionDraftId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.SubscriptionDraftId, e.CreatedAt })
                    .HasDatabaseName("IX_SubscriptionDraftAuditEvents_Draft_Date");
            });

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
            // 📌 Seed du nouveau référentiel ProductEnvelope compatible avec les ProductTypes historiques
            Data.Seed.ProductEnvelopeSeeder.Seed(modelBuilder);
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
