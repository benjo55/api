using api.Models.Cmdb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Models.Configurations;

public sealed class ConfigurationItemConfiguration : IEntityTypeConfiguration<ConfigurationItem>
{
    public void Configure(EntityTypeBuilder<ConfigurationItem> entity)
    {
        entity.ToTable("ConfigurationItems", "cmdb");
        entity.HasIndex(x => x.ExternalCiNumber).IsUnique();
        entity.HasIndex(x => x.Name);
        entity.HasIndex(x => new { x.Model, x.Category, x.Status, x.IsCurrent });
        entity.Property(x => x.ExternalCiNumber).HasMaxLength(32).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
        entity.Property(x => x.Label).HasMaxLength(500);
        entity.Property(x => x.Model).HasMaxLength(150).IsRequired();
        entity.Property(x => x.Category).HasMaxLength(150);
        entity.Property(x => x.Status).HasMaxLength(80);
        entity.Property(x => x.ApplicationCode).HasMaxLength(50);
        entity.Property(x => x.Version).HasMaxLength(200);
        entity.Property(x => x.DatabaseCode).HasMaxLength(50);
        entity.Property(x => x.EntityPath).HasMaxLength(500);
        entity.Property(x => x.ResponsibleEmployer).HasMaxLength(250);
        entity.Property(x => x.ApplicationDomain).HasMaxLength(200);
        entity.Property(x => x.PlatformType).HasMaxLength(100);
        entity.Property(x => x.PlatformName).HasMaxLength(200);
        entity.Property(x => x.BudgetCode).HasMaxLength(30);
        entity.Property(x => x.OwnerName).HasMaxLength(200);
        entity.Property(x => x.Rto).HasMaxLength(30);
        entity.Property(x => x.Rpo).HasMaxLength(30);
    }
}

public sealed class ConfigurationItemApplicationProfileConfiguration :
    IEntityTypeConfiguration<ConfigurationItemApplicationProfile>
{
    public void Configure(EntityTypeBuilder<ConfigurationItemApplicationProfile> entity)
    {
        entity.ToTable("ApplicationProfiles", "cmdb");
        entity.HasKey(x => x.ConfigurationItemId);
        entity.Property(x => x.ShortDescription).HasMaxLength(2000);
        entity.Property(x => x.DetailedDescription).HasMaxLength(8000);
        entity.Property(x => x.MainFunctionalProcesses).HasMaxLength(8000);
        entity.Property(x => x.GeneralTechnicalFramework).HasMaxLength(8000);
        entity.Property(x => x.OverallArchitecture).HasMaxLength(8000);
        entity.Property(x => x.ApplicationCriticality).HasMaxLength(20);
        entity.Property(x => x.ApplicationNature).HasMaxLength(40);
        entity.Property(x => x.LegalOwnerEntity).HasMaxLength(250);
        entity.Property(x => x.OtherStakeholders).HasMaxLength(2000);
        entity.Property(x => x.HostingMode).HasMaxLength(30);
        entity.Property(x => x.HostingProvider).HasMaxLength(150);
        entity.Property(x => x.CloudServiceModel).HasMaxLength(20);
        entity.Property(x => x.HostingNetworkZone).HasMaxLength(250);
        entity.Property(x => x.AuthenticationMode).HasMaxLength(30);
        entity.Property(x => x.IamSolution).HasMaxLength(150);
        entity.Property(x => x.StandalonePasswordRules).HasMaxLength(2000);
        entity.Property(x => x.LastAccessRemediationPercentage).HasPrecision(5, 2);
        entity.Property(x => x.PreviousAccessRemediationPercentage).HasPrecision(5, 2);
        entity.Property(x => x.SecurityComments).HasMaxLength(4000);
        entity.Property(x => x.LastRestorationTestResult).HasMaxLength(30);
        entity.Property(x => x.PersonalDataPseudonymization).HasMaxLength(20);

        foreach (var property in new[]
        {
            nameof(ConfigurationItemApplicationProfile.LastAccessRecertificationDate),
            nameof(ConfigurationItemApplicationProfile.PreviousAccessRecertificationDate),
            nameof(ConfigurationItemApplicationProfile.LastPentestDate),
            nameof(ConfigurationItemApplicationProfile.PreviousPentestDate),
            nameof(ConfigurationItemApplicationProfile.LastRedTeamDate),
            nameof(ConfigurationItemApplicationProfile.LastBugBountyDate),
            nameof(ConfigurationItemApplicationProfile.LastFailoverTestDate),
            nameof(ConfigurationItemApplicationProfile.PreviousFailoverTestDate),
        })
        {
            entity.Property<DateTime?>(property).HasColumnType("date");
        }

        entity.HasOne(x => x.ConfigurationItem)
            .WithOne(x => x.ApplicationProfile)
            .HasForeignKey<ConfigurationItemApplicationProfile>(x => x.ConfigurationItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CartographyNodeLayoutConfiguration :
    IEntityTypeConfiguration<CartographyNodeLayout>
{
    public void Configure(EntityTypeBuilder<CartographyNodeLayout> entity)
    {
        entity.ToTable("CartographyNodeLayouts", "cmdb");
        entity.Property(x => x.ScopeType).HasMaxLength(30).IsRequired();
        entity.Property(x => x.ScopeKey).HasMaxLength(250).IsRequired();
        entity.Property(x => x.UserName).HasMaxLength(150).IsRequired();
        entity.HasIndex(x => new
        {
            x.ScopeType,
            x.ScopeKey,
            x.UserName,
            x.ConfigurationItemId,
        }).IsUnique();
        entity.HasOne(x => x.ConfigurationItem)
            .WithMany(x => x.CartographyLayouts)
            .HasForeignKey(x => x.ConfigurationItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CartographyDomainDocumentConfiguration :
    IEntityTypeConfiguration<CartographyDomainDocument>
{
    public void Configure(EntityTypeBuilder<CartographyDomainDocument> entity)
    {
        entity.ToTable("CartographyDomainDocuments", "cmdb");
        entity.HasIndex(x => x.EmployerEntity).IsUnique();
        entity.Property(x => x.EmployerEntity).HasMaxLength(250).IsRequired();
        entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
    }
}

public sealed class CartographyDomainDocumentSectionConfiguration :
    IEntityTypeConfiguration<CartographyDomainDocumentSection>
{
    public void Configure(EntityTypeBuilder<CartographyDomainDocumentSection> entity)
    {
        entity.ToTable("CartographyDomainDocumentSections", "cmdb");
        entity.HasIndex(x => new { x.CartographyDomainDocumentId, x.SectionKey }).IsUnique();
        entity.Property(x => x.SectionKey).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
        entity.Property(x => x.ContentHtml).HasColumnType("nvarchar(max)");
        entity.Property(x => x.PlainText).HasColumnType("nvarchar(max)");
        entity.Property(x => x.EditorJson).HasColumnType("nvarchar(max)");
        entity.HasOne(x => x.CartographyDomainDocument)
            .WithMany(x => x.Sections)
            .HasForeignKey(x => x.CartographyDomainDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CiAttributeDefinitionConfiguration : IEntityTypeConfiguration<CiAttributeDefinition>
{
    public void Configure(EntityTypeBuilder<CiAttributeDefinition> entity)
    {
        entity.ToTable("AttributeDefinitions", "cmdb");
        entity.HasIndex(x => x.Code).IsUnique();
        entity.Property(x => x.Code).HasMaxLength(150).IsRequired();
        entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.DataType).HasMaxLength(20).IsRequired();
        entity.Property(x => x.Unit).HasMaxLength(30);
    }
}

public sealed class CiAttributeValueConfiguration : IEntityTypeConfiguration<CiAttributeValue>
{
    public void Configure(EntityTypeBuilder<CiAttributeValue> entity)
    {
        entity.ToTable("AttributeValues", "cmdb");
        entity.HasIndex(x => new { x.ConfigurationItemId, x.AttributeDefinitionId }).IsUnique();
        entity.Property(x => x.NumberValue).HasPrecision(28, 8);
        entity.Property(x => x.StringValue).HasMaxLength(2000);
        entity.HasOne(x => x.ConfigurationItem).WithMany(x => x.AttributeValues)
            .HasForeignKey(x => x.ConfigurationItemId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.AttributeDefinition).WithMany(x => x.Values)
            .HasForeignKey(x => x.AttributeDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CmdbRelationshipTypeConfiguration : IEntityTypeConfiguration<CmdbRelationshipType>
{
    public void Configure(EntityTypeBuilder<CmdbRelationshipType> entity)
    {
        entity.ToTable("RelationshipTypes", "cmdb");
        entity.HasIndex(x => x.Code).IsUnique();
        entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
        entity.Property(x => x.Family).HasMaxLength(80);
    }
}

public sealed class CmdbRelationshipConfiguration : IEntityTypeConfiguration<CmdbRelationship>
{
    public void Configure(EntityTypeBuilder<CmdbRelationship> entity)
    {
        entity.ToTable("Relationships", "cmdb");
        entity.HasIndex(x => new { x.SourceCiId, x.TargetCiId, x.RelationshipTypeId, x.SourceSystem }).IsUnique();
        entity.HasIndex(x => new { x.SourceCiId, x.IsCurrent });
        entity.HasIndex(x => new { x.TargetCiId, x.IsCurrent });
        entity.Property(x => x.SourceSystem).HasMaxLength(50).IsRequired();
        entity.HasOne(x => x.SourceCi).WithMany(x => x.OutgoingRelationships)
            .HasForeignKey(x => x.SourceCiId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.TargetCi).WithMany(x => x.IncomingRelationships)
            .HasForeignKey(x => x.TargetCiId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.RelationshipType).WithMany(x => x.Relationships)
            .HasForeignKey(x => x.RelationshipTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CiSupportAssignmentConfiguration : IEntityTypeConfiguration<CiSupportAssignment>
{
    public void Configure(EntityTypeBuilder<CiSupportAssignment> entity)
    {
        entity.ToTable("SupportAssignments", "cmdb");
        entity.HasIndex(x => new { x.ConfigurationItemId, x.GroupName, x.RoleName }).IsUnique();
        entity.Property(x => x.GroupName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.RoleName).HasMaxLength(150).IsRequired();
        entity.Property(x => x.ManagerName).HasMaxLength(200);
        entity.Property(x => x.ManagerEntity).HasMaxLength(300);
        entity.Property(x => x.ManagerTeam).HasMaxLength(200);
        entity.HasOne(x => x.ConfigurationItem).WithMany(x => x.SupportAssignments)
            .HasForeignKey(x => x.ConfigurationItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CmdbImportRunConfiguration : IEntityTypeConfiguration<CmdbImportRun>
{
    public void Configure(EntityTypeBuilder<CmdbImportRun> entity)
    {
        entity.ToTable("ImportRuns", "cmdb");
        entity.Property(x => x.SourceSystem).HasMaxLength(50).IsRequired();
        entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
    }
}

public sealed class IntegrationTechnologyConfiguration : IEntityTypeConfiguration<IntegrationTechnology>
{
    public void Configure(EntityTypeBuilder<IntegrationTechnology> entity)
    {
        entity.ToTable("Technologies", "integration");
        entity.HasIndex(x => x.Code).IsUnique();
        entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
        entity.Property(x => x.Family).HasMaxLength(80);
        entity.HasData(
            new IntegrationTechnology { Id = 1, Code = "REST", Name = "API REST", Family = "API" },
            new IntegrationTechnology { Id = 2, Code = "KAFKA", Name = "Apache Kafka", Family = "Messaging" },
            new IntegrationTechnology { Id = 3, Code = "SFTP", Name = "SFTP", Family = "FileTransfer" },
            new IntegrationTechnology { Id = 4, Code = "SSIS", Name = "SQL Server Integration Services", Family = "ETL" },
            new IntegrationTechnology { Id = 5, Code = "TALEND", Name = "Talend", Family = "ETL" },
            new IntegrationTechnology { Id = 6, Code = "RABBITMQ", Name = "RabbitMQ", Family = "Messaging" },
            new IntegrationTechnology { Id = 7, Code = "JDBC", Name = "JDBC / accès base", Family = "Database" });
    }
}

public sealed class ExchangePatternConfiguration : IEntityTypeConfiguration<ExchangePattern>
{
    public void Configure(EntityTypeBuilder<ExchangePattern> entity)
    {
        entity.ToTable("ExchangePatterns", "integration");
        entity.HasIndex(x => x.Code).IsUnique();
        entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
        entity.Property(x => x.Family).HasMaxLength(80).IsRequired();
        entity.Property(x => x.InteractionMode).HasMaxLength(30).IsRequired();
        entity.Property(x => x.TriggerMode).HasMaxLength(30).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(2000);
        entity.Property(x => x.TypicalUses).HasMaxLength(2000);
        entity.HasOne(x => x.DefaultTechnology).WithMany(x => x.ExchangePatterns)
            .HasForeignKey(x => x.DefaultTechnologyId).OnDelete(DeleteBehavior.SetNull);
        entity.HasData(
            new ExchangePattern
            {
                Id = 1, Code = "API_SYNC", Name = "API synchrone", Family = "API",
                InteractionMode = "Synchronous", TriggerMode = "OnDemand",
                DefaultTechnologyId = 1, Description = "Requête/réponse synchrone.", IsSystem = true,
                CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ExchangePattern
            {
                Id = 2, Code = "API_ASYNC_CALLBACK", Name = "API asynchrone avec callback", Family = "API",
                InteractionMode = "Asynchronous", TriggerMode = "OnDemand",
                DefaultTechnologyId = 1, Description = "Appel asynchrone avec notification de résultat.", IsSystem = true,
                CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ExchangePattern
            {
                Id = 3, Code = "KAFKA_EVENT", Name = "Événement Kafka", Family = "Messaging",
                InteractionMode = "Asynchronous", TriggerMode = "EventDriven",
                DefaultTechnologyId = 2, Description = "Publication et consommation événementielles.", IsSystem = true,
                CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ExchangePattern
            {
                Id = 4, Code = "ETL_BATCH", Name = "ETL batch", Family = "ETL",
                InteractionMode = "Asynchronous", TriggerMode = "Scheduled",
                DefaultTechnologyId = 4, Description = "Extraction, transformation et chargement planifiés.", IsSystem = true,
                CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ExchangePattern
            {
                Id = 5, Code = "ELT_BATCH", Name = "ELT batch", Family = "ELT",
                InteractionMode = "Asynchronous", TriggerMode = "Scheduled",
                Description = "Extraction, chargement puis transformation planifiés.", IsSystem = true,
                CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ExchangePattern
            {
                Id = 6, Code = "SFTP_BATCH", Name = "Fichier SFTP", Family = "FileTransfer",
                InteractionMode = "Asynchronous", TriggerMode = "Scheduled",
                DefaultTechnologyId = 3, Description = "Dépôt ou collecte planifiée de fichiers.", IsSystem = true,
                CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ExchangePattern
            {
                Id = 7, Code = "CDC_STREAM", Name = "CDC continu", Family = "CDC",
                InteractionMode = "Asynchronous", TriggerMode = "Continuous",
                Description = "Capture continue des changements de données.", IsSystem = true,
                CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
    }
}

public sealed class IntegrationFlowConfiguration : IEntityTypeConfiguration<IntegrationFlow>
{
    public void Configure(EntityTypeBuilder<IntegrationFlow> entity)
    {
        entity.ToTable("IntegrationFlows", "integration");
        entity.HasIndex(x => x.Code).IsUnique();
        entity.HasIndex(x => new { x.SourceCiId, x.Status });
        entity.HasIndex(x => new { x.TargetCiId, x.Status });
        entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(4000);
        entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
        entity.Property(x => x.Criticality).HasMaxLength(30);
        entity.Property(x => x.TransportProtocol).HasMaxLength(50);
        entity.Property(x => x.ChannelName).HasMaxLength(300);
        entity.Property(x => x.EndpointReference).HasMaxLength(1000);
        entity.Property(x => x.DataClassification).HasMaxLength(80);
        entity.Property(x => x.FlowGroupCode).HasMaxLength(80);
        entity.Property(x => x.AveragePayloadKb).HasPrecision(18, 3);
        entity.HasOne(x => x.SourceCi).WithMany()
            .HasForeignKey(x => x.SourceCiId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.TargetCi).WithMany()
            .HasForeignKey(x => x.TargetCiId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.BrokerCi).WithMany()
            .HasForeignKey(x => x.BrokerCiId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.ExchangePattern).WithMany(x => x.IntegrationFlows)
            .HasForeignKey(x => x.ExchangePatternId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Technology).WithMany(x => x.IntegrationFlows)
            .HasForeignKey(x => x.TechnologyId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(x => x.CmdbRelationship).WithMany()
            .HasForeignKey(x => x.CmdbRelationshipId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class FlowRouteStepConfiguration : IEntityTypeConfiguration<FlowRouteStep>
{
    public void Configure(EntityTypeBuilder<FlowRouteStep> entity)
    {
        entity.ToTable("FlowRouteSteps", "integration");
        entity.HasIndex(x => new { x.IntegrationFlowId, x.StepOrder }).IsUnique();
        entity.Property(x => x.StepKind).HasMaxLength(50).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.HasOne(x => x.IntegrationFlow).WithMany(x => x.RouteSteps)
            .HasForeignKey(x => x.IntegrationFlowId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.ConfigurationItem).WithMany()
            .HasForeignKey(x => x.ConfigurationItemId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Technology).WithMany()
            .HasForeignKey(x => x.TechnologyId).OnDelete(DeleteBehavior.SetNull);
    }
}
