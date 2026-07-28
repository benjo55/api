using api.Data;
using api.Dtos.TaxReceipts;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using api.Services.TaxReceipts;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using UglyToad.PdfPig;
using Xunit;

namespace api.Tests;

public sealed class TaxReceiptModuleTests
{
    [Theory]
    [InlineData(0, "ZERO EURO")]
    [InlineData(1, "UN EURO")]
    [InlineData(21, "VINGT ET UN EUROS")]
    [InlineData(80, "QUATRE-VINGTS EUROS")]
    [InlineData(81, "QUATRE-VINGT-UN EUROS")]
    [InlineData(100, "CENT EUROS")]
    [InlineData(1000, "MILLE EUROS")]
    [InlineData(10000, "DIX MILLE EUROS")]
    public void Amount_to_words_handles_reference_values(decimal amount, string expected)
    {
        var service = new AmountToWordsService();

        Assert.Equal(expected, service.ToFrenchEuros(amount));
    }

    [Fact]
    public void Amount_to_words_handles_cents()
    {
        var service = new AmountToWordsService();

        Assert.Equal("CENT VINGT-CINQ EUROS ET CINQUANTE CENTIMES", service.ToFrenchEuros(125.50m));
    }

    [Fact]
    public void Donation_validation_rejects_non_positive_amount()
    {
        var donation = new Donation
        {
            DonorId = 1,
            DonationDate = DateTime.UtcNow,
            Amount = 0,
            DonationForm = DonationForm.PrivateDeed,
            DonationNature = DonationNature.Cash,
            PaymentMethod = DonationPaymentMethod.BankTransfer,
            TaxRegime = DonationTaxRegime.Article200
        };

        Assert.Throws<BusinessException>(() => DonationService.ValidateBusinessRules(donation));
    }

    [Fact]
    public async Task Cerfa_2041_rd_pdf_generation_produces_two_page_pdf()
    {
        var generator = new TaxReceiptPdfGenerator2041Rd(
            new TestEnvironment { ContentRootPath = FindApiRoot() },
            new AmountToWordsService());

        var receipt = BuildReceipt();
        var pdf = await generator.GenerateAsync(receipt);

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));

        using var document = PdfDocument.Open(pdf);
        Assert.Equal(2, document.NumberOfPages);
        Assert.Contains("2026-000001", document.GetPage(1).Text);
        Assert.Contains("DUPONT", document.GetPage(2).Text);
        Assert.Contains("CENT VINGT-CINQ EUROS", document.GetPage(2).Text);
    }

    [Fact]
    public async Task Email_service_records_failure_when_smtp_is_not_configured()
    {
        await using var db = CreateContext();
        var receipt = BuildGeneratedReceipt();
        db.Add(receipt);
        await db.SaveChangesAsync();

        var service = new TaxReceiptEmailService(
            db,
            new FakeTaxReceiptStorage("%PDF-test\n"u8.ToArray()),
            new FakeSmtpMailSender(new SmtpMailSendResult(false, "SmtpNotConfigured", "SMTP non configuré.")),
            NullLogger<TaxReceiptEmailService>.Instance,
            TestEnvironmentWithApiRoot());

        var result = await service.SendAsync(
            receipt.Id,
            new SendTaxReceiptEmailDto("donateur@example.org", null, null),
            "test");

        var history = await db.TaxReceiptEmailHistory.SingleAsync();
        Assert.False(result.Success);
        Assert.Equal(TaxReceiptStatus.EmailFailed, result.Status);
        Assert.Equal(TaxReceiptEmailStatus.Failed, history.Status);
        Assert.Equal("SMTP non configuré.", history.ErrorMessage);
        Assert.Equal(TaxReceiptStatus.EmailFailed, receipt.Status);
        Assert.Equal(1, history.RetryCount);
    }

    [Fact]
    public async Task Email_service_records_success_and_sends_pdf_attachment()
    {
        await using var db = CreateContext();
        var receipt = BuildGeneratedReceipt();
        db.Add(receipt);
        await db.SaveChangesAsync();
        var sender = new FakeSmtpMailSender(new SmtpMailSendResult(true));

        var service = new TaxReceiptEmailService(
            db,
            new FakeTaxReceiptStorage("%PDF-test\n"u8.ToArray()),
            sender,
            NullLogger<TaxReceiptEmailService>.Instance,
            TestEnvironmentWithApiRoot());

        var result = await service.SendAsync(
            receipt.Id,
            new SendTaxReceiptEmailDto("donateur@example.org", null, null),
            "test");

        Assert.True(result.Success);
        Assert.Equal(TaxReceiptStatus.Sent, result.Status);
        Assert.Equal(TaxReceiptEmailStatus.Sent, result.EmailStatus);
        Assert.NotNull(result.SentAt);
        Assert.Single(sender.Messages);
        Assert.Equal("application/pdf", sender.Messages[0].AttachmentContentTypes.Single());
        Assert.Equal("Recu-fiscal-2026-000001.pdf", sender.Messages[0].AttachmentNames.Single());
        Assert.Contains(MediaTypeNames.Text.Plain, sender.Messages[0].AlternateViewContentTypes);
        Assert.Contains(MediaTypeNames.Text.Html, sender.Messages[0].AlternateViewContentTypes);
        Assert.DoesNotContain("<p>", sender.Messages[0].PlainTextBody);
        Assert.Contains("Software Superior by Design", sender.Messages[0].HtmlBody);
    }

    [Fact]
    public async Task Email_service_resends_after_failure_without_changing_receipt_number()
    {
        await using var db = CreateContext();
        var receipt = BuildGeneratedReceipt();
        db.Add(receipt);
        await db.SaveChangesAsync();
        var sender = new FakeSmtpMailSender(
            new SmtpMailSendResult(false, "SmtpException", "first failure"),
            new SmtpMailSendResult(true));

        var service = new TaxReceiptEmailService(
            db,
            new FakeTaxReceiptStorage("%PDF-test\n"u8.ToArray()),
            sender,
            NullLogger<TaxReceiptEmailService>.Instance,
            TestEnvironmentWithApiRoot());

        var first = await service.SendAsync(receipt.Id, new SendTaxReceiptEmailDto("donateur@example.org", null, null), "test");
        var second = await service.SendAsync(receipt.Id, new SendTaxReceiptEmailDto("donateur@example.org", null, null), "test");

        Assert.False(first.Success);
        Assert.True(second.Success);
        Assert.Equal(receipt.Id, second.ReceiptId);
        Assert.Equal("2026-000001", second.ReceiptNumber);
        Assert.Equal(TaxReceiptStatus.Sent, receipt.Status);
        Assert.Equal(2, await db.TaxReceiptEmailHistory.CountAsync());
        Assert.Equal(2, (await db.TaxReceiptEmailHistory.OrderBy(x => x.CreatedAt).LastAsync()).RetryCount);
    }

    [Fact]
    public async Task Email_service_rejects_receipt_owned_by_another_user()
    {
        await using var db = CreateContext();
        var receipt = BuildGeneratedReceipt();
        receipt.Donation.Donor.UserId = 10;
        db.Users.Add(new User
        {
            Id = 11,
            FirstName = "Alice",
            LastName = "Martin",
            Username = "alice",
            NormalizedUsername = "ALICE",
            Email = "alice@example.org",
            NormalizedEmail = "ALICE@EXAMPLE.ORG",
            PhoneNumber = "0102030405",
            PasswordHash = "hash",
            EmailConfirmed = true,
            Status = UserStatus.Active
        });
        db.Add(receipt);
        await db.SaveChangesAsync();
        var sender = new FakeSmtpMailSender(new SmtpMailSendResult(true));

        var service = new TaxReceiptEmailService(
            db,
            new FakeTaxReceiptStorage("%PDF-test\n"u8.ToArray()),
            sender,
            NullLogger<TaxReceiptEmailService>.Instance,
            TestEnvironmentWithApiRoot());

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SendAsync(receipt.Id, new SendTaxReceiptEmailDto(null, null, null), "alice", currentUserId: 11));

        Assert.Equal("TaxReceiptForbidden", ex.Message);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task Email_service_rejects_unconfirmed_account_before_smtp()
    {
        await using var db = CreateContext();
        var receipt = BuildGeneratedReceipt();
        receipt.Donation.Donor.UserId = 12;
        db.Users.Add(new User
        {
            Id = 12,
            FirstName = "Jean",
            LastName = "Dupont",
            Username = "jdupont",
            NormalizedUsername = "JDUPONT",
            Email = "donateur@example.org",
            NormalizedEmail = "DONATEUR@EXAMPLE.ORG",
            PhoneNumber = "0102030405",
            PasswordHash = "hash",
            EmailConfirmed = false,
            Status = UserStatus.PendingEmailConfirmation
        });
        db.Add(receipt);
        await db.SaveChangesAsync();
        var sender = new FakeSmtpMailSender(new SmtpMailSendResult(true));

        var service = new TaxReceiptEmailService(
            db,
            new FakeTaxReceiptStorage("%PDF-test\n"u8.ToArray()),
            sender,
            NullLogger<TaxReceiptEmailService>.Instance,
            TestEnvironmentWithApiRoot());

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SendAsync(receipt.Id, new SendTaxReceiptEmailDto(null, null, null), "jdupont", currentUserId: 12));

        Assert.Equal("EmailNotConfirmed", ex.Message);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task Create_receipt_is_idempotent_with_same_generation_request_key()
    {
        await using var db = CreateContext();
        var donation = BuildReceipt().Donation;
        donation.Id = 200;
        donation.OrganizationId = 10;
        donation.DonorId = 30;
        var organization = BuildReceipt().BeneficiaryOrganization;
        db.Add(organization);
        db.Add(donation.Donor);
        db.Add(donation);
        await db.SaveChangesAsync();
        var service = new TaxReceiptService(
            db,
            new FakeTaxReceiptNumberGenerator("2026-000001", "2026-000002"),
            new[] { new FakeTaxReceiptPdfGenerator() },
            new FakeTaxReceiptStorage("%PDF-test\n"u8.ToArray()));

        var first = await service.CreateForDonationAsync(
            donation.Id,
            new CreateTaxReceiptDto(organization.Id, "2041-RD", "11580*05", "same-key"),
            "test");
        var second = await service.CreateForDonationAsync(
            donation.Id,
            new CreateTaxReceiptDto(organization.Id, "2041-RD", "11580*05", "same-key"),
            "test");

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.ReceiptNumber, second.ReceiptNumber);
        Assert.Equal(1, await db.TaxReceipts.CountAsync());
    }

    private static ApplicationDBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDBContext(options);
    }

    private static TaxReceipt BuildReceipt() =>
        new()
        {
            Id = 42,
            ReceiptNumber = "2026-000001",
            CerfaCode = "2041-RD",
            CerfaVersion = "11580*05",
            Status = TaxReceiptStatus.Ready,
            BeneficiaryOrganization = new BeneficiaryOrganization
            {
                Id = 10,
                Name = "Association Life",
                IdentifierType = BeneficiaryIdentifierType.Rna,
                Identifier = "W123456789",
                StreetNumber = "12",
                StreetName = "rue de la Paix",
                PostalCode = "75002",
                City = "Paris",
                CountryCode = "FR",
                Purpose = "Soutien aux actions d'interet general",
                OrganizationCategory = BeneficiaryOrganizationCategory.GeneralInterestOrganization,
                OrganizationSubCategory = BeneficiaryOrganizationSubCategory.Association1901,
                IsActive = true
            },
            Donation = new Donation
            {
                Id = 20,
                DonationDate = new DateTime(2026, 5, 12),
                Amount = 125.50m,
                DonationForm = DonationForm.PrivateDeed,
                DonationNature = DonationNature.Cash,
                PaymentMethod = DonationPaymentMethod.BankTransfer,
                TaxRegime = DonationTaxRegime.Article200,
                Status = DonationStatus.Validated,
                Donor = new Donor
                {
                    Id = 30,
                    DonorType = DonorType.Individual,
                    LastName = "Dupont",
                    FirstName = "Jean",
                    AddressLine1 = "8 avenue Victor Hugo",
                    StreetNumber = "8",
                    StreetName = "avenue Victor Hugo",
                    PostalCode = "69002",
                    City = "Lyon",
                    CountryCode = "FR",
                    Email = "donateur@example.org"
                }
            }
        };

    private static TaxReceipt BuildGeneratedReceipt()
    {
        var content = "%PDF-test\n"u8.ToArray();
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
        var receipt = BuildReceipt();
        receipt.DocumentArtifact = new DocumentArtifact
        {
            Id = 1,
            Type = DocumentArtifactType.IssuedPdf,
            StorageKey = "receipt.pdf",
            FileName = "receipt.pdf",
            Hash = hash,
            Size = content.Length
        };
        receipt.PdfHash = receipt.DocumentArtifact.Hash;
        receipt.DocumentArtifactId = 1;
        receipt.Status = TaxReceiptStatus.Generated;
        return receipt;
    }

    private static string FindApiRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "api.csproj")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    private static TestEnvironment TestEnvironmentWithApiRoot() =>
        new() { ContentRootPath = FindApiRoot() };

    private sealed class FakeTaxReceiptStorage : IDocumentBinaryStorage
    {
        private readonly byte[] _content;

        public FakeTaxReceiptStorage(byte[] content)
        {
            _content = content;
        }

        public Task<(string StorageKey, string Hash, long Size)> SaveAsync(byte[] content, string extension, CancellationToken cancellationToken = default) =>
            Task.FromResult(("receipt.pdf", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)), (long)content.Length));

        public Task<byte[]> ReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_content);
    }

    private sealed class FakeSmtpMailSender : ISmtpMailSender
    {
        private readonly Queue<SmtpMailSendResult> _sendResults;
        public List<CapturedMailMessage> Messages { get; } = new();

        public FakeSmtpMailSender(params SmtpMailSendResult[] sendResults)
        {
            _sendResults = new Queue<SmtpMailSendResult>(sendResults);
        }

        public Task<SmtpMailSendResult> SendAsync(System.Net.Mail.MailMessage message, string messageType, CancellationToken cancellationToken = default)
        {
            Messages.Add(new CapturedMailMessage(
                message.To.Select(x => x.Address).ToList(),
                message.Attachments.Select(x => x.Name ?? string.Empty).ToList(),
                message.Attachments.Select(x => x.ContentType.MediaType).ToList(),
                message.AlternateViews.Select(x => x.ContentType.MediaType).ToList(),
                ReadAlternateView(message, MediaTypeNames.Text.Plain),
                ReadAlternateView(message, MediaTypeNames.Text.Html)));
            return Task.FromResult(_sendResults.Count == 0 ? new SmtpMailSendResult(true) : _sendResults.Dequeue());
        }

        private static string ReadAlternateView(System.Net.Mail.MailMessage message, string mediaType)
        {
            var view = message.AlternateViews.FirstOrDefault(x => x.ContentType.MediaType == mediaType);
            if (view is null)
            {
                return string.Empty;
            }

            view.ContentStream.Position = 0;
            using var reader = new StreamReader(view.ContentStream, Encoding.UTF8, leaveOpen: true);
            var value = reader.ReadToEnd();
            view.ContentStream.Position = 0;
            return value;
        }
    }

    private sealed record CapturedMailMessage(
        IReadOnlyList<string> Recipients,
        IReadOnlyList<string> AttachmentNames,
        IReadOnlyList<string> AttachmentContentTypes,
        IReadOnlyList<string> AlternateViewContentTypes,
        string PlainTextBody,
        string HtmlBody);

    private sealed class FakeTaxReceiptNumberGenerator : ITaxReceiptNumberGenerator
    {
        private readonly Queue<string> _numbers;

        public FakeTaxReceiptNumberGenerator(params string[] numbers)
        {
            _numbers = new Queue<string>(numbers);
        }

        public Task<string> GenerateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_numbers.Count == 0 ? Guid.NewGuid().ToString("N") : _numbers.Dequeue());
    }

    private sealed class FakeTaxReceiptPdfGenerator : ITaxReceiptPdfGenerator
    {
        public string CerfaCode => "2041-RD";
        public string CerfaVersion => "11580*05";

        public Task<byte[]> GenerateAsync(TaxReceipt receipt, CancellationToken cancellationToken = default) =>
            Task.FromResult("%PDF-test\n"u8.ToArray());
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
