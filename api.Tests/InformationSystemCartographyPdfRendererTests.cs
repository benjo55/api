using api.Dtos.Documents;
using api.Interfaces.Documents;
using api.Services.Documents.Models;
using api.Services.Documents.Renderers;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;
using Xunit;

namespace api.Tests
{
    public sealed class InformationSystemCartographyPdfRendererTests
    {
        [Fact]
        public async Task RenderAsync_keeps_portrait_orientation_and_embedded_section_images()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var imageData =
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAFgwJ/lz0eJwAAAABJRU5ErkJggg==";
            var model = new InformationSystemCartographyDocumentModel(
                "APICIL ASSET MANAGEMENT",
                new DateTime(2026, 8, 6),
                "Interne",
                new[]
                {
                    new CartographyDomainSectionModel(
                        "Introduction",
                        1,
                        1,
                        "Texte avant image",
                        $"""
                        <p>Texte avant image</p>
                        <img src="data:image/png;base64,{imageData}" width="240px" alt="Cartographie insérée" />
                        <p>Texte après image</p>
                        """)
                },
                new[]
                {
                    new CartographyApplicationModel(
                        1,
                        "CMDB00001",
                        "Application de test",
                        "Domaine",
                        "Responsable",
                        "High",
                        "Description",
                        "Cloud")
                },
                new[]
                {
                    new CartographyConfigurationItemModel(
                        1,
                        "CMDB00001",
                        "Application de test",
                        "Application Métier",
                        "APPLICATION",
                        "Production",
                        "Responsable")
                },
                Array.Empty<CartographyFlowModel>());

            var definition = new DocumentDefinition(
                "information-system-cartography",
                "Cartographie du SI",
                "questpdf-cmdb-cartography-v1",
                "Cartographie_SI_{subjectId}_{date}.pdf",
                "A3",
                "Portrait",
                null,
                SupportsPreview: true,
                SupportsDownload: true,
                SupportsArchive: false,
                SupportsEmail: false,
                typeof(object),
                typeof(InformationSystemCartographyQuestPdfRenderer));

            var context = new DocumentGenerationContext(
                new System.Security.Claims.ClaimsPrincipal(),
                null,
                null,
                "fr-FR",
                "Europe/Paris",
                DateTimeOffset.UtcNow,
                DocumentDeliveryMode.Download,
                "test",
                "2026-08-06");

            var rendered = await new InformationSystemCartographyQuestPdfRenderer()
                .RenderAsync(model, definition, context);
            using var pdf = PdfDocument.Open(rendered.Content);
            var firstPage = pdf.GetPage(1);

            Assert.True(firstPage.Width < firstPage.Height);
            Assert.Contains("Texte avant image", firstPage.Text);
            Assert.Contains("Texte après image", firstPage.Text);
            Assert.NotEmpty(firstPage.GetImages());
        }
    }
}
