using api.Models.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Models.Configurations
{
    public class DonorConfiguration : IEntityTypeConfiguration<Donor>
    {
        public void Configure(EntityTypeBuilder<Donor> entity)
        {
            entity.ToTable("Donors");
            entity.Property(x => x.UserId);
            entity.Property(x => x.DonorType).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Title).HasMaxLength(30);
            entity.Property(x => x.LastName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CompanyName).HasMaxLength(250);
            entity.Property(x => x.BirthDate).HasColumnType("date");
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.AddressLine1).HasMaxLength(300).IsRequired();
            entity.Property(x => x.AddressGeoJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.AddressLine2).HasMaxLength(300);
            entity.Property(x => x.StreetNumber).HasMaxLength(30);
            entity.Property(x => x.StreetName).HasMaxLength(250).IsRequired();
            entity.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.City).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => x.Email).HasDatabaseName("IX_Donors_Email");
            entity.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL").HasDatabaseName("UX_Donors_UserId");
            entity.HasIndex(x => new { x.LastName, x.FirstName, x.PostalCode }).HasDatabaseName("IX_Donors_DuplicateLookup");

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class DonationConfiguration : IEntityTypeConfiguration<Donation>
    {
        public void Configure(EntityTypeBuilder<Donation> entity)
        {
            entity.ToTable("Donations");
            entity.Property(x => x.UserId);
            entity.Property(x => x.PublicId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(120);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Article200Amount).HasPrecision(18, 2);
            entity.Property(x => x.Article978Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.DonationForm).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.OtherFormDescription).HasMaxLength(500);
            entity.Property(x => x.DonationNature).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.OtherNatureDescription).HasMaxLength(500);
            entity.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.TaxRegime).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Purpose).HasMaxLength(500);
            entity.Property(x => x.ExternalReference).HasMaxLength(120);
            entity.Property(x => x.Comments).HasMaxLength(2000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.LegacyDonationLinkStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ConfirmedPaymentProvider).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.PostPaymentProcessingError).HasMaxLength(2000);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("UX_Donations_PublicId");
            entity.HasIndex(x => x.Reference).IsUnique().HasFilter("[Reference] IS NOT NULL").HasDatabaseName("UX_Donations_Reference");
            entity.HasIndex(x => x.UserId).HasDatabaseName("IX_Donations_UserId");
            entity.HasIndex(x => x.DonationDate).HasDatabaseName("IX_Donations_DonationDate");
            entity.HasIndex(x => new { x.DonorId, x.Status }).HasDatabaseName("IX_Donations_Donor_Status");

            entity.HasOne(x => x.Donor)
                .WithMany(x => x.Donations)
                .HasForeignKey(x => x.DonorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Organization)
                .WithMany(x => x.Donations)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DonorSnapshot)
                .WithOne(x => x.Donation)
                .HasForeignKey<DonationDonorSnapshot>(x => x.DonationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class BeneficiaryOrganizationConfiguration : IEntityTypeConfiguration<BeneficiaryOrganization>
    {
        public void Configure(EntityTypeBuilder<BeneficiaryOrganization> entity)
        {
            entity.ToTable("BeneficiaryOrganizations");
            entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
            entity.Property(x => x.LegalName).HasMaxLength(250);
            entity.Property(x => x.IdentifierType).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Identifier).HasMaxLength(80).IsRequired();
            entity.Property(x => x.RnaNumber).HasMaxLength(30);
            entity.Property(x => x.Siret).HasMaxLength(20);
            entity.Property(x => x.StreetNumber).HasMaxLength(30);
            entity.Property(x => x.StreetName).HasMaxLength(250).IsRequired();
            entity.Property(x => x.AddressGeoJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.AddressLine2).HasMaxLength(300);
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.City).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.FiscalArticle).HasMaxLength(50);
            entity.Property(x => x.HelloAssoOrganizationSlug).HasMaxLength(120);
            entity.Property(x => x.HelloAssoEnvironment).HasMaxLength(30);
            entity.Property(x => x.HelloAssoCredentialKey).HasMaxLength(120);
            entity.Property(x => x.HelloAssoConnectionStatus).HasMaxLength(80);
            entity.Property(x => x.HelloAssoConnectionError).HasMaxLength(1000);
            entity.Property(x => x.PayPalMerchantAlias).HasMaxLength(120);
            entity.Property(x => x.PayPalMerchantId).HasMaxLength(120);
            entity.Property(x => x.PayPalEnvironment).HasMaxLength(30);
            entity.Property(x => x.PayPalCredentialKey).HasMaxLength(120);
            entity.Property(x => x.Purpose).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.OrganizationCategory).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.OrganizationSubCategory).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.OtherCategoryDescription).HasMaxLength(1000);
            entity.HasIndex(x => new { x.IdentifierType, x.Identifier }).IsUnique();
            entity.HasIndex(x => x.IsActive).HasDatabaseName("IX_BeneficiaryOrganizations_IsActive");
            entity.HasIndex(x => x.HelloAssoOrganizationSlug).HasDatabaseName("IX_BeneficiaryOrganizations_HelloAssoSlug");
        }
    }

    public class OrganizationBankAccountConfiguration : IEntityTypeConfiguration<OrganizationBankAccount>
    {
        public void Configure(EntityTypeBuilder<OrganizationBankAccount> entity)
        {
            entity.ToTable("OrganizationBankAccounts");
            entity.Property(x => x.AccountHolder).HasMaxLength(250).IsRequired();
            entity.Property(x => x.BankName).HasMaxLength(250);
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.EncryptedIban).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.IbanLastFour).HasMaxLength(4).IsRequired();
            entity.Property(x => x.EncryptedBic).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.BicLastFour).HasMaxLength(4).IsRequired();
            entity.Property(x => x.Instructions).HasMaxLength(2000);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.BeneficiaryOrganizationId, x.IsActive, x.ValidFrom })
                .HasDatabaseName("IX_OrganizationBankAccounts_Organization_Active");

            entity.HasOne(x => x.BeneficiaryOrganization)
                .WithMany(x => x.BankAccounts)
                .HasForeignKey(x => x.BeneficiaryOrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class DonationDonorSnapshotConfiguration : IEntityTypeConfiguration<DonationDonorSnapshot>
    {
        public void Configure(EntityTypeBuilder<DonationDonorSnapshot> entity)
        {
            entity.ToTable("DonationDonorSnapshots");
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.BirthDate).HasColumnType("date");
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.AddressLine1).HasMaxLength(300).IsRequired();
            entity.Property(x => x.AddressLine2).HasMaxLength(300);
            entity.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.City).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Country).HasMaxLength(2).IsRequired();
            entity.HasIndex(x => x.DonationId).IsUnique().HasDatabaseName("UX_DonationDonorSnapshots_DonationId");
            entity.HasIndex(x => x.UserId).HasDatabaseName("IX_DonationDonorSnapshots_UserId");

            entity.HasOne(x => x.Donation)
                .WithOne(x => x.DonorSnapshot)
                .HasForeignKey<DonationDonorSnapshot>(x => x.DonationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class TaxReceiptDeliveryConfiguration : IEntityTypeConfiguration<TaxReceiptDelivery>
    {
        public void Configure(EntityTypeBuilder<TaxReceiptDelivery> entity)
        {
            entity.ToTable("TaxReceiptDeliveries");
            entity.Property(x => x.RecipientEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.DeliveryType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.DeliveryStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.HasIndex(x => new { x.TaxReceiptId, x.DeliveryType, x.RecipientEmail, x.CreatedAt })
                .HasDatabaseName("IX_TaxReceiptDeliveries_Receipt_Type_Email_Date");
            entity.HasIndex(x => x.TaxReceiptId).HasDatabaseName("IX_TaxReceiptDeliveries_ReceiptId");

            entity.HasOne(x => x.TaxReceipt)
                .WithMany()
                .HasForeignKey(x => x.TaxReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class TaxReceiptConfiguration : IEntityTypeConfiguration<TaxReceipt>
    {
        public void Configure(EntityTypeBuilder<TaxReceipt> entity)
        {
            entity.ToTable("TaxReceipts");
            entity.Property(x => x.ReceiptNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CerfaCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CerfaVersion).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.GenerationRequestKey).HasMaxLength(120);
            entity.Property(x => x.GeneratedFileName).HasMaxLength(260);
            entity.Property(x => x.PdfHash).HasMaxLength(128);
            entity.Property(x => x.GeneratedBy).HasMaxLength(120);
            entity.Property(x => x.SentToEmail).HasMaxLength(320);
            entity.Property(x => x.LastEmailStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.CancellationReason).HasMaxLength(1000);
            entity.HasIndex(x => x.ReceiptNumber).IsUnique().HasDatabaseName("UX_TaxReceipts_ReceiptNumber");
            entity.HasIndex(x => new { x.BeneficiaryOrganizationId, x.ReceiptNumber })
                .IsUnique()
                .HasDatabaseName("UX_TaxReceipts_Organization_ReceiptNumber");
            entity.HasIndex(x => x.Status).HasDatabaseName("IX_TaxReceipts_Status");
            entity.HasIndex(x => x.GenerationRequestKey)
                .IsUnique()
                .HasFilter("[GenerationRequestKey] IS NOT NULL")
                .HasDatabaseName("UX_TaxReceipts_GenerationRequestKey");
            entity.HasIndex(x => x.DonationId)
                .IsUnique()
                .HasFilter("[Status] IN ('Ready', 'Generated', 'Sent', 'EmailFailed')")
                .HasDatabaseName("UX_TaxReceipts_ActiveDonation");

            entity.HasOne(x => x.Donation)
                .WithMany(x => x.TaxReceipts)
                .HasForeignKey(x => x.DonationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.BeneficiaryOrganization)
                .WithMany(x => x.TaxReceipts)
                .HasForeignKey(x => x.BeneficiaryOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DocumentArtifact)
                .WithMany()
                .HasForeignKey(x => x.DocumentArtifactId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ReplacementReceipt)
                .WithMany()
                .HasForeignKey(x => x.ReplacementReceiptId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
    {
        public void Configure(EntityTypeBuilder<PaymentAttempt> entity)
        {
            entity.ToTable("PaymentAttempts");
            entity.Property(x => x.Provider).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ProviderCheckoutIntentId).HasMaxLength(120);
            entity.Property(x => x.ProviderOrderId).HasMaxLength(120);
            entity.Property(x => x.ProviderPaymentId).HasMaxLength(120);
            entity.Property(x => x.ProviderPaymentState).HasMaxLength(80);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.RedirectUrl).HasMaxLength(1000);
            entity.Property(x => x.InternalReference).HasMaxLength(80).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(120);
            entity.Property(x => x.FailureCode).HasMaxLength(120);
            entity.Property(x => x.FailureMessage).HasMaxLength(1000);
            entity.Property(x => x.DonorTransferDeclarationComment).HasMaxLength(1000);
            entity.Property(x => x.AdminNote).HasMaxLength(1000);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.DonationId).HasDatabaseName("IX_PaymentAttempts_DonationId");
            entity.HasIndex(x => x.InternalReference)
                .IsUnique()
                .HasDatabaseName("UX_PaymentAttempts_InternalReference");
            entity.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL")
                .HasDatabaseName("UX_PaymentAttempts_IdempotencyKey");
            entity.HasIndex(x => x.ProviderCheckoutIntentId)
                .IsUnique()
                .HasFilter("[ProviderCheckoutIntentId] IS NOT NULL")
                .HasDatabaseName("UX_PaymentAttempts_CheckoutIntentId");
            entity.HasIndex(x => x.ProviderPaymentId)
                .HasDatabaseName("IX_PaymentAttempts_ProviderPaymentId");

            entity.HasOne(x => x.Donation)
                .WithMany(x => x.PaymentAttempts)
                .HasForeignKey(x => x.DonationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PaymentWebhookInboxConfiguration : IEntityTypeConfiguration<PaymentWebhookInbox>
    {
        public void Configure(EntityTypeBuilder<PaymentWebhookInbox> entity)
        {
            entity.ToTable("PaymentWebhookInbox");
            entity.Property(x => x.Provider).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.PayloadHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(100);
            entity.Property(x => x.ExternalObjectId).HasMaxLength(120);
            entity.Property(x => x.RawPayload).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.ProcessingStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.HasIndex(x => new { x.Provider, x.PayloadHash })
                .IsUnique()
                .HasDatabaseName("UX_PaymentWebhookInbox_Provider_PayloadHash");
            entity.HasIndex(x => new { x.ProcessingStatus, x.ReceivedAt })
                .HasDatabaseName("IX_PaymentWebhookInbox_Status_ReceivedAt");
        }
    }

    public class TaxReceiptEmailHistoryConfiguration : IEntityTypeConfiguration<TaxReceiptEmailHistory>
    {
        public void Configure(EntityTypeBuilder<TaxReceiptEmailHistory> entity)
        {
            entity.ToTable("TaxReceiptEmailHistory");
            entity.Property(x => x.RecipientEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(x => new { x.TaxReceiptId, x.CreatedAt }).HasDatabaseName("IX_TaxReceiptEmailHistory_Receipt_CreatedAt");

            entity.HasOne(x => x.TaxReceipt)
                .WithMany(x => x.EmailHistory)
                .HasForeignKey(x => x.TaxReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
