using api.Data;
using api.Models;
using api.Models.Enum;
using api.Services;
using api.Services.LegalDocuments;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests
{
    public class SubscriptionDocumentServiceTests
    {
        [Fact]
        public async Task GenerateDossier_uses_product_document_assignments_and_secures_download()
        {
            await using var db = CreateDbContext();
            var user = new User
            {
                FirstName = "Patrick",
                LastName = "Benhamou",
                Username = "alainv",
                NormalizedUsername = "ALAINV",
                Email = "patrick@example.test",
                NormalizedEmail = "PATRICK@EXAMPLE.TEST",
                PhoneNumber = "+33100000000",
                EmailConfirmed = true,
                Status = UserStatus.Active,
            };
            var otherUser = new User
            {
                FirstName = "Other",
                LastName = "User",
                Username = "other",
                NormalizedUsername = "OTHER",
                Email = "other@example.test",
                NormalizedEmail = "OTHER@EXAMPLE.TEST",
                PhoneNumber = "+33100000001",
                EmailConfirmed = true,
                Status = UserStatus.Active,
            };
            var product = new Product
            {
                ProductCode = "FL-AV",
                ProductName = "Assurance-vie Sérénité",
                ContractFamily = ContractFamily.AssuranceVie,
            };
            var definition = new LegalDocumentDefinition
            {
                Code = "CG",
                Name = "Conditions générales",
                Type = LegalDocumentType.ProductGeneralTerms,
            };
            var revision = new LegalDocumentRevision
            {
                LegalDocumentDefinition = definition,
                Status = DocumentRevisionStatus.Published,
                MajorVersion = 1,
                MinorVersion = 0,
                ContentHash = "content-hash",
            };
            var assignment = new ProductDocumentAssignment
            {
                Product = product,
                LegalDocumentRevision = revision,
                Role = ProductDocumentRole.GeneralTerms,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                IsCurrent = true,
            };
            var draft = new SubscriptionDraft
            {
                User = user,
                Product = product,
                ProductType = ContractFamily.AssuranceVie,
                CurrentStep = "signature",
                Status = SubscriptionDraftStatus.InProgress,
                InvestmentDataJson = """{"initialAmount":"10000","managementMode":"Gestion pilotée"}""",
                ProtectionDataJson = """{"beneficiaryChoice":"standard"}""",
            };

            db.Users.AddRange(user, otherUser);
            db.ProductDocumentAssignments.Add(assignment);
            db.SubscriptionDrafts.Add(draft);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var dossier = await service.GenerateDossierAsync(user.Id, draft.Id, user.Username, CancellationToken.None);

            Assert.True(dossier.IsComplete);
            var document = Assert.Single(dossier.Documents);
            Assert.Equal("Conditions générales", document.RoleLabel);
            Assert.NotNull(document.ArtifactId);

            var file = await service.GetDocumentFileAsync(user.Id, draft.Id, document.ArtifactId!.Value, CancellationToken.None);
            Assert.Equal("application/pdf", file.ContentType);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetDocumentFileAsync(otherUser.Id, draft.Id, document.ArtifactId.Value, CancellationToken.None));
        }

        [Fact]
        public async Task GetDossier_warns_when_no_product_is_selected()
        {
            await using var db = CreateDbContext();
            var user = new User
            {
                FirstName = "Patrick",
                LastName = "Benhamou",
                Username = "alainv",
                NormalizedUsername = "ALAINV",
                Email = "patrick@example.test",
                NormalizedEmail = "PATRICK@EXAMPLE.TEST",
                PhoneNumber = "+33100000000",
                EmailConfirmed = true,
                Status = UserStatus.Active,
            };
            var draft = new SubscriptionDraft
            {
                User = user,
                CurrentStep = "solution",
                Status = SubscriptionDraftStatus.InProgress,
            };
            db.SubscriptionDrafts.Add(draft);
            await db.SaveChangesAsync();

            var dossier = await CreateService(db).GetDossierAsync(user.Id, draft.Id, CancellationToken.None);

            Assert.False(dossier.IsComplete);
            Assert.Empty(dossier.Documents);
            Assert.Contains(dossier.Warnings, x => x.Contains("Sélectionnez un produit", StringComparison.Ordinal));
        }

        [Fact]
        public async Task GenerateDossier_creates_summary_when_product_has_no_document_assignments()
        {
            await using var db = CreateDbContext();
            var user = new User
            {
                FirstName = "Patrick",
                LastName = "Benhamou",
                Username = "alainv",
                NormalizedUsername = "ALAINV",
                Email = "patrick@example.test",
                NormalizedEmail = "PATRICK@EXAMPLE.TEST",
                PhoneNumber = "+33100000000",
                EmailConfirmed = true,
                Status = UserStatus.Active,
            };
            var product = new Product
            {
                ProductCode = "GC8L",
                ProductName = "Concordances 4",
                ContractFamily = ContractFamily.AssuranceVie,
            };
            var draft = new SubscriptionDraft
            {
                User = user,
                Product = product,
                ProductType = ContractFamily.AssuranceVie,
                CurrentStep = "signature",
                Status = SubscriptionDraftStatus.InProgress,
            };
            db.SubscriptionDrafts.Add(draft);
            await db.SaveChangesAsync();

            var dossier = await CreateService(db).GenerateDossierAsync(user.Id, draft.Id, user.Username, CancellationToken.None);

            Assert.True(dossier.IsComplete);
            Assert.Contains(dossier.Warnings, x => x.Contains("Aucun document réglementaire", StringComparison.Ordinal));
            var document = Assert.Single(dossier.Documents);
            Assert.Null(document.LegalDocumentRevisionId);
            Assert.Equal("Dossier de souscription", document.DocumentName);
            Assert.NotNull(document.ArtifactId);
        }

        private static SubscriptionDocumentService CreateService(ApplicationDBContext db)
        {
            var audit = new FakeAuditService();
            return new SubscriptionDocumentService(
                db,
                new ProductDocumentAssignmentService(db, audit),
                new FakeRenderService(),
                new FakePdfGenerationService(),
                new FakeBinaryStorage(),
                audit);
        }

        private static ApplicationDBContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDBContext(options);
        }
    }
}
