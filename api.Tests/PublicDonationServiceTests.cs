using api.Configuration;
using api.Data;
using api.Dtos.Generic;
using api.Dtos.PublicDonations;
using api.Dtos.TaxReceipts;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using api.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace api.Tests;

public sealed class PublicDonationServiceTests
{
    [Fact]
    public async Task InitializeCheckout_UsesOrganizationHelloAssoConfiguration()
    {
        await using var db = CreateContext();
        db.BeneficiaryOrganizations.Add(new BeneficiaryOrganization
        {
            Name = "ACIC",
            IdentifierType = BeneficiaryIdentifierType.Rna,
            Identifier = "W123456789",
            StreetName = "rue de la Paix",
            PostalCode = "75001",
            City = "Paris",
            Purpose = "Interet general",
            OrganizationCategory = BeneficiaryOrganizationCategory.GeneralInterestOrganization,
            IsActive = true,
            IsDonationEnabled = true,
            IsHelloAssoPaymentEnabled = true,
            HelloAssoOrganizationSlug = "acic-tests",
            HelloAssoCredentialKey = "acic-sandbox",
        });
        await db.SaveChangesAsync();
        var provider = new CapturingPaymentProvider();
        var service = CreateService(db, provider);

        var response = await service.InitializeCheckoutAsync(
            new PublicDonationCheckoutRequest(
                50m,
                new PublicDonationDonorInput(
                    "Jean",
                    "Dupont",
                    "jean.dupont@example.org",
                    "8 avenue Victor Hugo",
                    "69002",
                    "Lyon",
                    "FRA")),
            CancellationToken.None);

        Assert.Equal("https://helloasso.test/checkout", response.RedirectUrl);
        Assert.NotNull(provider.LastCommand);
        Assert.Equal("acic-tests", provider.LastCommand.OrganizationSlug);
        Assert.Equal("acic-sandbox", provider.LastCommand.CredentialKey);
        Assert.Equal(5000, provider.LastCommand.AmountInCents);
        Assert.Equal("FR", provider.LastCommand.Country);
        Assert.StartsWith("http://localhost:5173/donate/return?donation=", provider.LastCommand.ReturnUrl);

        var donation = await db.Donations.Include(x => x.PaymentAttempts).SingleAsync();
        Assert.Equal(DonationStatus.PaymentPending, donation.Status);
        Assert.Equal(response.DonationId, donation.PublicId);
        Assert.Equal("DON-", response.Reference[..4]);
        var attempt = Assert.Single(donation.PaymentAttempts);
        Assert.Equal(PaymentStatus.RedirectReady, attempt.PaymentStatus);
        Assert.Equal("checkout-123", attempt.ProviderCheckoutIntentId);
    }

    private static PublicDonationService CreateService(
        ApplicationDBContext db,
        IPaymentProvider paymentProvider) =>
        new(
            db,
            paymentProvider,
            Options.Create(new HelloAssoOptions
            {
                Enabled = true,
                ItemName = "Don test",
                Credentials = new Dictionary<string, HelloAssoCredentialOptions>(StringComparer.OrdinalIgnoreCase)
                {
                    ["acic-sandbox"] = new()
                    {
                        ClientId = "client-id",
                        ClientSecret = "client-secret",
                    },
                },
            }),
            Options.Create(new DonationCheckoutOptions()),
            new FakeTaxReceiptService(),
            new FakeTaxReceiptEmailService(),
            new FakeDonationReceiptAccessTokenService(),
            NullLogger<PublicDonationService>.Instance,
            new FakePublicOriginResolver());

    private static ApplicationDBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDBContext(options);
    }

    private sealed class CapturingPaymentProvider : IPaymentProvider
    {
        public CreateCheckoutCommand? LastCommand { get; private set; }

        public Task<CreateCheckoutResult> CreateCheckoutAsync(
            CreateCheckoutCommand command,
            CancellationToken cancellationToken)
        {
            LastCommand = command;
            return Task.FromResult(new CreateCheckoutResult(
                true,
                "checkout-123",
                "https://helloasso.test/checkout",
                null,
                null,
                "{}"));
        }

        public Task<PaymentReconciliationResult> ReconcilePaymentAsync(
            PaymentReconciliationCommand command,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WebhookReceptionResult> ReceiveWebhookAsync(
            string rawBody,
            IReadOnlyDictionary<string, string> headers,
            string? remoteIpAddress,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class FakeTaxReceiptService : ITaxReceiptService
    {
        public Task<PagedResult<TaxReceiptDto>> GetAllAsync(QueryObject query, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TaxReceiptDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TaxReceiptDto> CreateForDonationAsync(int donationId, CreateTaxReceiptDto dto, string? userName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TaxReceiptDto?> ValidateAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TaxReceiptGenerationResultDto> GenerateAsync(int id, string? userName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(byte[] Content, string FileName)> GetPdfAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TaxReceiptDto?> CancelAsync(int id, string? reason, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TaxReceiptDto> ReplaceAsync(int id, string? userName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<TaxReceiptEmailHistoryDto>> GetEmailHistoryAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeTaxReceiptEmailService : ITaxReceiptEmailService
    {
        public Task<TaxReceiptEmailSendResultDto> SendAsync(
            int taxReceiptId,
            SendTaxReceiptEmailDto dto,
            string? userName,
            CancellationToken cancellationToken = default,
            int? currentUserId = null,
            bool canAccessAllReceipts = false) =>
            throw new NotImplementedException();
    }

    private sealed class FakeDonationReceiptAccessTokenService : IDonationReceiptAccessTokenService
    {
        public string Create(string publicDonationId, TimeSpan lifetime) => "receipt-token";
        public bool Validate(string publicDonationId, string token) => true;
    }

    private sealed class FakePublicOriginResolver : IPublicOriginResolver
    {
        public ResolvedPublicOrigin ResolveCurrent() =>
            Resolve("localhost");

        public ResolvedPublicOrigin Resolve(string? host) =>
            new(SiteExperience.Donation, "http://localhost:5173", host ?? "localhost", true, UnknownHostPolicy.UseDefaultExperience);

        public string GetOrigin(SiteExperience experience) =>
            "http://localhost:5173";
    }
}
