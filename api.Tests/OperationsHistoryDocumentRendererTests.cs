using System.Security.Claims;
using System.Text.Json;
using api.Dtos.Documents;
using api.Dtos.Pdf;
using api.Exceptions;
using api.Interfaces;
using api.Interfaces.Documents;
using api.Services.Documents.Models;
using api.Services.Documents.Providers;
using api.Services.Documents.Renderers;
using Xunit;

namespace api.Tests;

public sealed class OperationsHistoryDocumentRendererTests
{
    [Fact]
    public async Task Provider_builds_operations_history_request_from_subject_and_parameters()
    {
        var provider = new OperationsHistoryDocumentDataProvider();
        using var parameters = JsonDocument.Parse("""
        {
          "fileName": "historique-operations-C123",
          "logoUrl": "https://life.local/logo.png"
        }
        """);

        var model = (OperationsHistoryDocumentModel)await provider.BuildModelAsync(
            BuildDefinition(),
            new GenerateDocumentRequestDto
            {
                SubjectId = "12",
                Parameters = parameters.RootElement.Clone()
            },
            BuildContext());

        Assert.Equal(12, model.Request.ContractId);
        Assert.Equal("historique-operations-C123", model.Request.FileName);
        Assert.Equal("https://life.local/logo.png", model.Request.LogoUrl);
    }

    [Fact]
    public async Task Provider_requires_a_valid_contract_id()
    {
        var provider = new OperationsHistoryDocumentDataProvider();

        await Assert.ThrowsAsync<BusinessException>(() =>
            provider.BuildModelAsync(
                BuildDefinition(),
                new GenerateDocumentRequestDto { SubjectId = "nope" },
                BuildContext()));
    }

    [Fact]
    public async Task Renderer_delegates_to_business_pdf_service()
    {
        var service = new RecordingPdfBusinessDocumentService();
        var renderer = new OperationsHistoryPdfRenderer(service);
        var request = new GenerateOperationsHistoryRequestDto
        {
            ContractId = 99,
            FileName = "historique-operations-99"
        };

        var rendered = await renderer.RenderAsync(
            new OperationsHistoryDocumentModel(request),
            BuildDefinition(),
            BuildContext());

        Assert.Same(request, service.OperationsHistoryRequest);
        Assert.Equal("application/pdf", rendered.ContentType);
        Assert.Equal("historique-operations-99.pdf", rendered.FileName);
        Assert.Equal("%PDF-operations-history\n"u8.Length, rendered.Content.Length);
        Assert.Equal("99", rendered.Metadata["contractId"]);
        Assert.Equal("False", rendered.Metadata["hasLogo"]);
    }

    private static DocumentDefinition BuildDefinition() =>
        new(
            "operations-history",
            "Historique des opérations",
            "pdf-operations-history-v1",
            "Historique_operations_{subjectId}_{date}.pdf",
            "A4",
            "Portrait",
            null,
            SupportsPreview: true,
            SupportsDownload: true,
            SupportsArchive: false,
            SupportsEmail: false,
            typeof(OperationsHistoryDocumentDataProvider),
            typeof(OperationsHistoryPdfRenderer),
            DocumentRenderEngine.QuestPdf);

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

    private sealed class RecordingPdfBusinessDocumentService : IPdfBusinessDocumentService
    {
        public GenerateOperationsHistoryRequestDto? OperationsHistoryRequest { get; private set; }

        public Task<PdfGeneratedFileDto> GenerateContractSheetAsync(
            GenerateContractSheetRequestDto request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PdfGeneratedFileDto> GenerateOperationsHistoryAsync(
            GenerateOperationsHistoryRequestDto request,
            CancellationToken cancellationToken = default)
        {
            OperationsHistoryRequest = request;
            return Task.FromResult(new PdfGeneratedFileDto
            {
                FileName = "historique-operations-99.pdf",
                Content = "%PDF-operations-history\n"u8.ToArray()
            });
        }

        public Task<PdfGeneratedFileDto> GenerateAssetAllocationReportAsync(
            GenerateAssetAllocationReportRequestDto request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PdfGeneratedFileDto> GenerateClientCaseFileAsync(
            GenerateClientCaseFileRequestDto request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
