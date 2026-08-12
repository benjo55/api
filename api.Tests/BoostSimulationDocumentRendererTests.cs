using System.Security.Claims;
using System.Text.Json;
using api.Dtos.Documents;
using api.Exceptions;
using api.Interfaces;
using api.Interfaces.Documents;
using api.Services.Documents.Models;
using api.Services.Documents.Providers;
using api.Services.Documents.Renderers;
using Xunit;

namespace api.Tests;

public sealed class BoostSimulationDocumentRendererTests
{
    [Fact]
    public async Task Provider_builds_boost_simulation_from_parameters()
    {
        var provider = new BoostSimulationDocumentDataProvider();
        using var parameters = JsonDocument.Parse("""
        {
          "fileName": "simulation-boost-client",
          "collecte": {
            "id": 4,
            "descriptionCollecte": "Campagne Boost 2026",
            "tauxCollecte1": 1.5,
            "tauxCollecte2": 2.25,
            "prenomClient": "Alice",
            "nomClient": "Martin"
          },
          "operations": [
            {
              "id": 2,
              "descriptionOperation": "Versement",
              "dateOperation": "2026-07-15T00:00:00.000Z",
              "montantOperation": 12000,
              "categorieOperation": "Investissement",
              "eligibleS1": 0,
              "eligibleS2": 12000,
              "montantBoost": 126.42
            }
          ]
        }
        """);

        var model = (BoostSimulationDocumentModel)await provider.BuildModelAsync(
            BuildDefinition(),
            new GenerateDocumentRequestDto
            {
                SubjectId = "4",
                Parameters = parameters.RootElement.Clone()
            },
            BuildContext());

        Assert.Equal("simulation-boost-client", model.FileName);
        Assert.Equal("Campagne Boost 2026", model.Collecte.DescriptionCollecte);
        Assert.Single(model.Operations);
        Assert.Equal(126.42m, model.Operations[0].MontantBoost);
    }

    [Fact]
    public async Task Provider_requires_parameters()
    {
        var provider = new BoostSimulationDocumentDataProvider();

        await Assert.ThrowsAsync<BusinessException>(() =>
            provider.BuildModelAsync(
                BuildDefinition(),
                new GenerateDocumentRequestDto(),
                BuildContext()));
    }

    [Fact]
    public async Task Renderer_generates_html_pdf()
    {
        var pdfGeneration = new RecordingPdfGenerationService();
        var renderer = new BoostSimulationHtmlPdfRenderer(pdfGeneration);
        var model = new BoostSimulationDocumentModel(
            new BoostCollecteModel(7, "Campagne <Boost>", 1.5m, 2.25m, "Alice", "Martin"),
            new[]
            {
                new BoostOperationModel(
                    1,
                    "Versement libre",
                    new DateTime(2026, 7, 15),
                    12000m,
                    "Investissement",
                    0m,
                    12000m,
                    126.42m)
            },
            "simulation-boost-7");

        var rendered = await renderer.RenderAsync(model, BuildDefinition(), BuildContext());

        Assert.Equal("application/pdf", rendered.ContentType);
        Assert.Equal("simulation-boost-7.pdf", rendered.FileName);
        Assert.Equal("%PDF-boost\n"u8.Length, rendered.Content.Length);
        Assert.Equal("A4", pdfGeneration.LastPageFormat);
        Assert.Contains("Campagne &lt;Boost&gt;", pdfGeneration.LastHtml);
        Assert.Contains("Versement libre", pdfGeneration.LastHtml);
        Assert.Equal("1", rendered.Metadata["operationsCount"]);
    }

    private static DocumentDefinition BuildDefinition() =>
        new(
            "boost-simulation",
            "Simulation Boost",
            "html-boost-simulation-v1",
            "Simulation_Boost_{subjectId}_{date}.pdf",
            "A4",
            "Portrait",
            null,
            SupportsPreview: true,
            SupportsDownload: true,
            SupportsArchive: false,
            SupportsEmail: false,
            typeof(BoostSimulationDocumentDataProvider),
            typeof(BoostSimulationHtmlPdfRenderer),
            DocumentRenderEngine.HtmlToPdf);

    private static DocumentGenerationContext BuildContext() =>
        new(
            new ClaimsPrincipal(new ClaimsIdentity()),
            UserId: 7,
            UserName: "alice",
            Locale: "fr-FR",
            TimeZone: "Europe/Paris",
            GeneratedAt: DateTimeOffset.UtcNow,
            DeliveryMode: DocumentDeliveryMode.Download,
            CorrelationId: "test",
            DataAsOfDate: null);

    private sealed class RecordingPdfGenerationService : IPdfGenerationService
    {
        public string LastHtml { get; private set; } = string.Empty;
        public string LastPageFormat { get; private set; } = string.Empty;

        public Task<byte[]> GeneratePdfAsync(string html, string pageFormat, CancellationToken cancellationToken = default)
        {
            LastHtml = html;
            LastPageFormat = pageFormat;
            return Task.FromResult("%PDF-boost\n"u8.ToArray());
        }
    }
}
