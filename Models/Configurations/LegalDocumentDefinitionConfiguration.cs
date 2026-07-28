using api.Models.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Models.Configurations
{
    public class LegalDocumentDefinitionConfiguration : IEntityTypeConfiguration<LegalDocumentDefinition>
    {
        public void Configure(EntityTypeBuilder<LegalDocumentDefinition> entity)
        {
            entity.ToTable("LegalDocumentDefinitions");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(60);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasOne(x => x.CurrentDraftRevision)
                .WithMany()
                .HasForeignKey(x => x.CurrentDraftRevisionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CurrentPublishedRevision)
                .WithMany()
                .HasForeignKey(x => x.CurrentPublishedRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class LegalDocumentRevisionConfiguration : IEntityTypeConfiguration<LegalDocumentRevision>
    {
        public void Configure(EntityTypeBuilder<LegalDocumentRevision> entity)
        {
            entity.ToTable("LegalDocumentRevisions");
            entity.HasIndex(x => new { x.LegalDocumentDefinitionId, x.MajorVersion, x.MinorVersion }).IsUnique();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ChangeSummary).HasMaxLength(2000);
            entity.Property(x => x.ValidationComment).HasMaxLength(2000);
            entity.Property(x => x.ContentHash).HasMaxLength(128);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.ValidatedBy).HasMaxLength(120);
            entity.Property(x => x.PublishedBy).HasMaxLength(120);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasOne(x => x.LegalDocumentDefinition)
                .WithMany(x => x.Revisions)
                .HasForeignKey(x => x.LegalDocumentDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.BasedOnRevision)
                .WithMany()
                .HasForeignKey(x => x.BasedOnRevisionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DocumentLayoutTemplate)
                .WithMany()
                .HasForeignKey(x => x.DocumentLayoutTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class LegalDocumentNodeConfiguration : IEntityTypeConfiguration<LegalDocumentNode>
    {
        public void Configure(EntityTypeBuilder<LegalDocumentNode> entity)
        {
            entity.ToTable("LegalDocumentNodes");
            entity.HasIndex(x => x.LegalDocumentRevisionId);
            entity.HasIndex(x => x.ParentNodeId);
            entity.HasIndex(x => new { x.ParentNodeId, x.SortOrder });
            entity.HasIndex(x => new { x.LegalDocumentRevisionId, x.StableKey }).IsUnique();
            entity.Property(x => x.StableKey).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.BusinessCode).HasMaxLength(80);
            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.NumberingStyle).HasMaxLength(40);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasOne(x => x.LegalDocumentRevision)
                .WithMany(x => x.Nodes)
                .HasForeignKey(x => x.LegalDocumentRevisionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ParentNode)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SourceClauseRevision)
                .WithMany()
                .HasForeignKey(x => x.SourceClauseRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ClauseDefinitionConfiguration : IEntityTypeConfiguration<ClauseDefinition>
    {
        public void Configure(EntityTypeBuilder<ClauseDefinition> entity)
        {
            entity.ToTable("ClauseDefinitions");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.RowVersion).IsRowVersion();
        }
    }

    public class ClauseRevisionConfiguration : IEntityTypeConfiguration<ClauseRevision>
    {
        public void Configure(EntityTypeBuilder<ClauseRevision> entity)
        {
            entity.ToTable("ClauseRevisions");
            entity.HasIndex(x => new { x.ClauseDefinitionId, x.MajorVersion, x.MinorVersion }).IsUnique();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ContentHash).HasMaxLength(128);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasOne(x => x.ClauseDefinition)
                .WithMany(x => x.Revisions)
                .HasForeignKey(x => x.ClauseDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class DocumentLayoutTemplateConfiguration : IEntityTypeConfiguration<DocumentLayoutTemplate>
    {
        public void Configure(EntityTypeBuilder<DocumentLayoutTemplate> entity)
        {
            entity.ToTable("DocumentLayoutTemplates");
            entity.HasIndex(x => new { x.Code, x.TemplateVersion }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PageFormat).HasMaxLength(20).IsRequired();
            entity.Property(x => x.MarginTopMm).HasPrecision(8, 2);
            entity.Property(x => x.MarginRightMm).HasPrecision(8, 2);
            entity.Property(x => x.MarginBottomMm).HasPrecision(8, 2);
            entity.Property(x => x.MarginLeftMm).HasPrecision(8, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.RowVersion).IsRowVersion();
        }
    }

    public class DocumentArtifactConfiguration : IEntityTypeConfiguration<DocumentArtifact>
    {
        public void Configure(EntityTypeBuilder<DocumentArtifact> entity)
        {
            entity.ToTable("DocumentArtifacts");
            entity.HasIndex(x => x.StorageKey).IsUnique();
            entity.HasIndex(x => x.CacheKey).HasFilter("[CacheKey] IS NOT NULL");
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.StorageKey).HasMaxLength(260).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.Hash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.GeneratedBy).HasMaxLength(120);
            entity.Property(x => x.CacheKey).HasMaxLength(256);

            entity.HasOne(x => x.LegalDocumentRevision)
                .WithMany(x => x.Artifacts)
                .HasForeignKey(x => x.LegalDocumentRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ProductDocumentAssignmentConfiguration : IEntityTypeConfiguration<ProductDocumentAssignment>
    {
        public void Configure(EntityTypeBuilder<ProductDocumentAssignment> entity)
        {
            entity.ToTable("ProductDocumentAssignments");
            entity.HasIndex(x => new { x.ProductId, x.Role, x.ValidFrom, x.ValidTo });
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.LegalDocumentRevision)
                .WithMany()
                .HasForeignKey(x => x.LegalDocumentRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ContractDocumentInstanceConfiguration : IEntityTypeConfiguration<ContractDocumentInstance>
    {
        public void Configure(EntityTypeBuilder<ContractDocumentInstance> entity)
        {
            entity.ToTable("ContractDocumentInstances");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ContentHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.IssuedBy).HasMaxLength(120);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasOne(x => x.Contract)
                .WithMany()
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.TemplateRevision)
                .WithMany()
                .HasForeignKey(x => x.TemplateRevisionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ApplicableGeneralTermsRevision)
                .WithMany()
                .HasForeignKey(x => x.ApplicableGeneralTermsRevisionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.PdfArtifact)
                .WithMany()
                .HasForeignKey(x => x.PdfArtifactId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class DocumentAuditEventConfiguration : IEntityTypeConfiguration<DocumentAuditEvent>
    {
        public void Configure(EntityTypeBuilder<DocumentAuditEvent> entity)
        {
            entity.ToTable("DocumentAuditEvents");
            entity.HasIndex(x => new { x.LegalDocumentRevisionId, x.CreatedAt });
            entity.Property(x => x.Action).HasConversion<string>().HasMaxLength(60);
            entity.Property(x => x.CreatedBy).HasMaxLength(120);
        }
    }
}
