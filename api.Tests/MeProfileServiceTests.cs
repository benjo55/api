using api.Data;
using api.Models;
using api.Models.Enum;
using api.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests;

public sealed class MeProfileServiceTests
{
    [Fact]
    public async Task GetDashboard_ComputesConfirmedTotalsAndDocuments()
    {
        await using var db = CreateDbContext();
        var user = new User
        {
            FirstName = "Alain",
            LastName = "Vie",
            Username = "alainv",
            NormalizedUsername = "ALAINV",
            Email = "alain@example.com",
            NormalizedEmail = "ALAIN@EXAMPLE.COM",
            PhoneNumber = "0612345678",
            PasswordHash = "hash",
            EmailConfirmed = true,
            Status = UserStatus.Active
        };
        var donor = new Donor
        {
            User = user,
            FirstName = "Alain",
            LastName = "Vie",
            BirthDate = new DateTime(1964, 9, 29),
            Email = user.Email,
            Phone = user.PhoneNumber,
            AddressLine1 = "1 rue de Paris",
            StreetName = "1 rue de Paris",
            PostalCode = "75001",
            City = "Paris",
            CountryCode = "FR"
        };
        var organization = new BeneficiaryOrganization
        {
            Name = "Fondation Life",
            IdentifierType = BeneficiaryIdentifierType.Siren,
            Identifier = "123456789",
            StreetName = "2 rue du Don",
            PostalCode = "75002",
            City = "Paris",
            CountryCode = "FR",
            Purpose = "Dons",
            OrganizationCategory = BeneficiaryOrganizationCategory.GeneralInterestOrganization,
            IsActive = true,
            IsDonationEnabled = true
        };
        var paidDonation = new Donation
        {
            User = user,
            Donor = donor,
            Organization = organization,
            DonationDate = DateTime.UtcNow.Date,
            Amount = 100m,
            Currency = "EUR",
            DonationForm = DonationForm.Other,
            DonationNature = DonationNature.Cash,
            TaxRegime = DonationTaxRegime.Article200,
            Status = DonationStatus.Paid
        };
        paidDonation.TaxReceipts.Add(new TaxReceipt
        {
            BeneficiaryOrganization = organization,
            ReceiptNumber = "2026-000001",
            Status = TaxReceiptStatus.Generated
        });
        var pendingDonation = new Donation
        {
            User = user,
            Donor = donor,
            Organization = organization,
            DonationDate = DateTime.UtcNow.Date.AddDays(-1),
            Amount = 80m,
            Currency = "EUR",
            DonationForm = DonationForm.Other,
            DonationNature = DonationNature.Cash,
            TaxRegime = DonationTaxRegime.Article200,
            Status = DonationStatus.AwaitingPayment
        };

        db.Users.Add(user);
        db.Donors.Add(donor);
        db.BeneficiaryOrganizations.Add(organization);
        db.Donations.AddRange(paidDonation, pendingDonation);
        await db.SaveChangesAsync();

        var dashboard = await new MeProfileService(db).GetDashboardAsync(user.Id);

        Assert.Equal(2, dashboard.Donations.DonationCount);
        Assert.Equal(100m, dashboard.Donations.ConfirmedTotalAmount);
        Assert.Equal(1, dashboard.Donations.AvailableDocumentCount);
    }

    [Fact]
    public async Task GetDonationOrganizations_ReturnsOnlyActiveDonationEnabledOrganizations()
    {
        await using var db = CreateDbContext();
        db.BeneficiaryOrganizations.AddRange(
            new BeneficiaryOrganization
            {
                Name = "ACIC",
                IdentifierType = BeneficiaryIdentifierType.Rna,
                Identifier = "W784005258",
                StreetName = "Rue de Versailles",
                PostalCode = "78150",
                City = "Le Chesnay Rocquencourt",
                CountryCode = "FR",
                Purpose = "Association Culturelle Israélite du Chesnay",
                OrganizationCategory = BeneficiaryOrganizationCategory.CultAssociationAlsaceMoselle,
                IsActive = true,
                IsDonationEnabled = true,
                IsEligibleForTaxReceipt = true
            },
            new BeneficiaryOrganization
            {
                Name = "Inactive",
                IdentifierType = BeneficiaryIdentifierType.Rna,
                Identifier = "W000000001",
                StreetName = "Rue fermée",
                PostalCode = "75000",
                City = "Paris",
                CountryCode = "FR",
                Purpose = "Non visible",
                OrganizationCategory = BeneficiaryOrganizationCategory.GeneralInterestOrganization,
                IsActive = false,
                IsDonationEnabled = true,
                IsEligibleForTaxReceipt = true
            },
            new BeneficiaryOrganization
            {
                Name = "Disabled",
                IdentifierType = BeneficiaryIdentifierType.Rna,
                Identifier = "W000000002",
                StreetName = "Rue fermée",
                PostalCode = "75000",
                City = "Paris",
                CountryCode = "FR",
                Purpose = "Non visible",
                OrganizationCategory = BeneficiaryOrganizationCategory.GeneralInterestOrganization,
                IsActive = true,
                IsDonationEnabled = false,
                IsEligibleForTaxReceipt = true
            });
        await db.SaveChangesAsync();

        var organizations = await new MeProfileService(db).GetDonationOrganizationsAsync();

        var organization = Assert.Single(organizations);
        Assert.Equal("ACIC", organization.Name);
        Assert.True(organization.IsEligibleForTaxReceipt);
    }

    private static ApplicationDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDBContext(options);
    }
}
