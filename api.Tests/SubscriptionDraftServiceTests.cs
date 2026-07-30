using System.Text.Json;
using api.Data;
using api.Models;
using api.Models.Enum;
using api.Services;
using api.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests;

public class SubscriptionDraftServiceTests
{
    [Fact]
    public async Task Complete_subscription_path_reaches_awaiting_signature()
    {
        await using var db = CreateContext();
        var user = new User
        {
            Id = 1,
            FirstName = "Patrick",
            LastName = "Benhamou",
            Username = "alainv",
            NormalizedUsername = "ALAINV",
            Email = "patrick@example.com",
            NormalizedEmail = "PATRICK@EXAMPLE.COM",
            PhoneNumber = "+33123456789",
            PasswordHash = "hash",
            EmailConfirmed = true,
            Status = UserStatus.Active,
        };
        var product = new Product
        {
            Id = 10,
            ProductCode = "GCL5-00003",
            ProductName = "Concordances 2",
            CommercialName = "Concordances 2",
            ContractFamily = ContractFamily.AssuranceVie,
            IsOpenToNewBusiness = true,
            Locked = false,
        };
        db.Users.Add(user);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var draft = await service.CreateAsync(user.Id, CancellationToken.None);

        draft = await service.SaveStepAsync(user.Id, draft.Id, SubscriptionStepKeys.Project, Element(new
        {
            primaryGoal = "retire",
            secondaryGoal = "income",
            horizon = "more8",
            liquidityNeed = "partial",
            targetAmount = "80000",
        }), CancellationToken.None);
        draft = await service.SaveStepAsync(user.Id, draft.Id, SubscriptionStepKeys.Situation, Element(new
        {
            familySituation = "Couple",
            professionalActivity = "Cadre",
            residenceCountry = "France",
            taxResidence = "France",
            annualIncomeRange = "75 000 à 150 000 €",
            annualChargesRange = "25 000 à 75 000 €",
            monthlySavingsCapacity = "500",
            liquidSavingsRange = "25 000 à 75 000 €",
            totalWealthRange = "150 000 à 500 000 €",
            lossCapacity = "10",
            fundOrigins = new[] { "savings" },
        }), CancellationToken.None);
        draft = await service.SaveStepAsync(user.Id, draft.Id, SubscriptionStepKeys.Profile, Element(new
        {
            knownProducts = new[] { "Fonds en euros", "Unités de compte", "ETF" },
            knowledgeChecks = new[] { "Un support peut baisser en valeur.", "La diversification réduit certains risques." },
            experienceLevel = "held",
            riskScenario = "wait",
            managementPreference = "advisor",
        }), CancellationToken.None);
        draft = await service.ComputeInvestorProfileAsync(user.Id, draft.Id, CancellationToken.None);
        draft = await service.SaveStepAsync(user.Id, draft.Id, SubscriptionStepKeys.Solution, Element(new
        {
            selectedContractFamily = (int)ContractFamily.AssuranceVie,
            selectedProductId = product.Id,
            selectedProductLabel = "GCL5-00003 - Concordances 2",
            acceptedRecommendation = true,
        }), CancellationToken.None);
        draft = await service.GenerateRecommendationAsync(user.Id, draft.Id, CancellationToken.None);
        draft = await service.AcceptRecommendationAsync(user.Id, draft.Id, CancellationToken.None);
        draft = await service.SaveStepAsync(user.Id, draft.Id, SubscriptionStepKeys.Investment, Element(new
        {
            initialAmount = "5000",
            scheduledPaymentEnabled = true,
            scheduledAmount = "200",
            scheduledFrequency = "Mensuelle",
            paymentMode = "Prélèvement SEPA",
            managementMode = "Conseil accompagné",
            allocation = new[]
            {
                new { label = "Fonds en euros", percentage = "60", riskLevel = "Faible" },
                new { label = "Unités de compte", percentage = "40", riskLevel = "Modéré" },
            },
            confirmsSavingsCapacityWarning = false,
        }), CancellationToken.None);
        draft = await service.SaveStepAsync(user.Id, draft.Id, SubscriptionStepKeys.Protection, Element(new
        {
            beneficiaryChoice = "standard",
            beneficiaries = Array.Empty<object>(),
        }), CancellationToken.None);
        draft = await service.SaveStepAsync(user.Id, draft.Id, SubscriptionStepKeys.Signature, Element(new
        {
            documentsReceived = true,
            contractTermsAccepted = true,
            informationAccuracyConfirmed = true,
            electronicSignatureConsent = true,
            debitMandateAccepted = true,
        }), CancellationToken.None);

        var submitted = await service.SubmitAsync(user.Id, draft.Id, CancellationToken.None);

        Assert.Equal(SubscriptionDraftStatus.AwaitingSignature, submitted.Status);
        Assert.True(await db.SubscriptionDraftAuditEvents.AnyAsync(e => e.EventType == "SubmittedAwaitingSignature"));
    }

    [Fact]
    public async Task User_cannot_read_another_users_draft()
    {
        await using var db = CreateContext();
        db.Users.AddRange(
            MakeUser(1, "one"),
            MakeUser(2, "two"));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var draft = await service.CreateAsync(1, CancellationToken.None);

        var forbidden = await service.GetByIdAsync(2, draft.Id, CancellationToken.None);

        Assert.Null(forbidden);
    }

    [Fact]
    public async Task Investment_step_rejects_invalid_iban()
    {
        await using var db = CreateContext();
        db.Users.Add(MakeUser(1, "one"));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var draft = await service.CreateAsync(1, CancellationToken.None);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveStepAsync(1, draft.Id, SubscriptionStepKeys.Investment, Element(new
            {
                initialAmount = 1000,
                scheduledPaymentEnabled = false,
                scheduledAmount = 0,
                scheduledFrequency = "Mensuelle",
                ibanLabel = "FR76 3000 6000 0112 3456 7890 180",
                paymentMode = "Prélèvement SEPA",
                managementMode = "Conseil accompagné",
                allocation = new[]
                {
                    new { label = "Fonds en euros", percentage = 60, riskLevel = "Faible" },
                    new { label = "Unités de compte", percentage = 40, riskLevel = "Modéré" },
                },
                confirmsSavingsCapacityWarning = false,
            }), CancellationToken.None));

        Assert.Contains("IBAN", error.Message);
    }

    [Fact]
    public async Task Protection_step_accepts_standard_clause_with_stale_structured_beneficiaries()
    {
        await using var db = CreateContext();
        db.Users.Add(MakeUser(1, "one"));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var draft = await service.CreateAsync(1, CancellationToken.None);
        draft = await service.SaveStepAsync(1, draft.Id, SubscriptionStepKeys.Solution, Element(new
        {
            selectedContractFamily = (int)ContractFamily.AssuranceVie,
            selectedProductId = 10,
            selectedProductLabel = "GCL5-00003 - Concordances 2",
            acceptedRecommendation = true,
        }), CancellationToken.None);

        var saved = await service.SaveStepAsync(1, draft.Id, SubscriptionStepKeys.Protection, Element(new
        {
            beneficiaryChoice = "standard",
            customClause = "",
            beneficiaries = new[]
            {
                new
                {
                    rank = 1,
                    type = "Personne physique",
                    firstName = "",
                    lastName = "",
                    relationship = "",
                    percentage = "80",
                },
            },
        }), CancellationToken.None);

        Assert.Equal(SubscriptionStepKeys.Protection, saved.CurrentStep);
    }

    private static User MakeUser(int id, string username) => new()
    {
        Id = id,
        FirstName = username,
        LastName = "User",
        Username = username,
        NormalizedUsername = username.ToUpperInvariant(),
        Email = $"{username}@example.com",
        NormalizedEmail = $"{username.ToUpperInvariant()}@EXAMPLE.COM",
        PhoneNumber = "+33123456789",
        PasswordHash = "hash",
        EmailConfirmed = true,
        Status = UserStatus.Active,
    };

    private static JsonElement Element<T>(T value) =>
        JsonSerializer.SerializeToElement(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static SubscriptionDraftService CreateService(ApplicationDBContext db) =>
        new(db, new IbanValidator());

    private static ApplicationDBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDBContext(options);
    }
}
