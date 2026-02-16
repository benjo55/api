using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Models.Configurations
{
    public class SupportDocumentConfiguration : IEntityTypeConfiguration<SupportDocument>
    {
        public void Configure(EntityTypeBuilder<SupportDocument> builder)
        {
            builder.ToTable("SupportDocuments");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.DocumentType).IsRequired().HasMaxLength(100);
            builder.Property(d => d.Url).IsRequired();
            builder.Property(d => d.PublicationDate).IsRequired(false);
            builder.HasOne(d => d.FinancialSupport)
                   .WithMany(f => f.Documents)
                   .HasForeignKey(d => d.FinancialSupportId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class SupportHistoricalDataConfiguration : IEntityTypeConfiguration<SupportHistoricalData>
    {
        public void Configure(EntityTypeBuilder<SupportHistoricalData> builder)
        {
            builder.ToTable("SupportHistoricalData");
            builder.HasKey(h => h.Id);
            builder.Property(h => h.Date).IsRequired();
            builder.Property(h => h.Nav).HasColumnType("decimal(18,4)");
            builder.HasOne(h => h.FinancialSupport)
                   .WithMany(f => f.HistoricalData)
                   .HasForeignKey(h => h.FinancialSupportId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class SupportFeeDetailConfiguration : IEntityTypeConfiguration<SupportFeeDetail>
    {
        public void Configure(EntityTypeBuilder<SupportFeeDetail> builder)
        {
            builder.ToTable("SupportFeeDetails");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.FeeType).IsRequired().HasMaxLength(100);
            builder.Property(f => f.Rate).HasColumnType("decimal(10,4)");
            builder.Property(f => f.Conditions).HasMaxLength(500);
            builder.Property(f => f.Currency).HasMaxLength(3);
            builder.HasOne(f => f.FinancialSupport)
                   .WithMany(s => s.FeeDetails)
                   .HasForeignKey(f => f.FinancialSupportId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class SupportLookthroughAssetConfiguration : IEntityTypeConfiguration<SupportLookthroughAsset>
    {
        public void Configure(EntityTypeBuilder<SupportLookthroughAsset> builder)
        {
            builder.ToTable("SupportLookthroughAssets");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.AssetName).IsRequired().HasMaxLength(200);
            builder.Property(a => a.ISIN).HasMaxLength(12);
            builder.Property(a => a.AssetClass).HasMaxLength(100);
            builder.Property(a => a.Weight).HasColumnType("decimal(5,2)");
            builder.HasOne(a => a.FinancialSupport)
                   .WithMany(s => s.LookthroughAssets)
                   .HasForeignKey(a => a.FinancialSupportId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
