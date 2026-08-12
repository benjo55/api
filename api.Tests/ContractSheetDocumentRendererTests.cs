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

public sealed class ContractSheetDocumentRendererTests
{
    [Fact]
    public async Task Provider_builds_contract_sheet_request_from_subject_and_parameters()
    {
        var provider = new ContractSheetDocumentDataProvider();
        using var parameters = JsonDocument.Parse("""
        {
          "fileName": "fiche-contrat-C123",
          "logoUrl": "https://life.local/logo.png",
          "qrCodeContent": "https://life.local/contracts/12"
        }
        """);

        var model = (ContractSheetDocumentModel)await provider.BuildModelAsync(
            BuildDefinition(),
            new GenerateDocumentRequestDto
            {
                SubjectId = "12",
                Parameters = parameters.RootElement.Clone()
            },
            BuildContext());

        Assert.Equal(12, model.Request.ContractId);
        Assert.Equal("fiche-contrat-C123", model.Request.FileName);
        Assert.Equal("https://life.local/logo.png", model.Request.LogoUrl);
        Assert.Equal("https://life.local/contracts/12", model.Request.QrCodeContent);
    }

    [Fact]
    public async Task Provider_requires_a_valid_contract_id()
    {
        var provider = new ContractSheetDocumentDataProvider();

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
        var renderer = new ContractSheetPdfRenderer(service);
        var request = new GenerateContractSheetRequestDto
        {
            ContractId = 99,
            FileName = "fiche-contrat-99",
            QrCodeContent = "https://life.local/contracts/99"
        };

        var rendered = await renderer.RenderAsync(
            new ContractSheetDocumentModel(request),
            BuildDefinition(),
            BuildContext());

        Assert.Same(request, service.ContractSheetRequest);
        Assert.Equal("application/pdf", rendered.ContentType);
        Assert.Equal("fiche-contrat-99.pdf", rendered.FileName);
        Assert.Equal("%PDF-contract-sheet\n"u8.Length, rendered.Content.Length);
        Assert.Equal("99", rendered.Metadata["contractId"]);
        Assert.Equal("True", rendered.Metadata["hasQrCode"]);
    }

    private static DocumentDefinition BuildDefinition() =>
        new(
            "contract-sheet",
            "Fiche contrat",
            "pdf-contract-sheet-v1",
            "Fiche_contrat_{subjectId}_{date}.pdf",
            "A4",
            "Portrait",
            null,
            SupportsPreview: true,
            SupportsDownload: true,
            SupportsArchive: false,
            SupportsEmail: false,
            typeof(ContractSheetDocumentDataProvider),
            typeof(ContractSheetPdfRenderer),
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
        public GenerateContractSheetRequestDto? ContractSheetRequest { get; private set; }

        public Task<PdfGeneratedFileDto> GenerateContractSheetAsync(
            GenerateContractSheetRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ContractSheetRequest = request;
            return Task.FromResult(new PdfGeneratedFileDto
            {
                FileName = "fiche-contrat-99.pdf",
                Content = "%PDF-contract-sheet\n"u8.ToArray()
            });
        }

        public Task<PdfGeneratedFileDto> GenerateOperationsHistoryAsync(
            GenerateOperationsHistoryRequestDto request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

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
