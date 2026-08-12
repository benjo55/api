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

public sealed class AssetAllocationReportDocumentRendererTests
{
    [Fact]
    public async Task Provider_builds_asset_allocation_request_from_subject_and_parameters()
    {
        var provider = new AssetAllocationReportDocumentDataProvider();
        using var parameters = JsonDocument.Parse("""
        {
          "fileName": "allocation-actifs-C123",
          "logoUrl": "https://life.local/logo.png"
        }
        """);

        var model = (AssetAllocationReportDocumentModel)await provider.BuildModelAsync(
            BuildDefinition(),
            new GenerateDocumentRequestDto
            {
                SubjectId = "12",
                Parameters = parameters.RootElement.Clone()
            },
            BuildContext());

        Assert.Equal(12, model.Request.ContractId);
        Assert.Equal("allocation-actifs-C123", model.Request.FileName);
        Assert.Equal("https://life.local/logo.png", model.Request.LogoUrl);
    }

    [Fact]
    public async Task Provider_requires_a_valid_contract_id()
    {
        var provider = new AssetAllocationReportDocumentDataProvider();

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
        var renderer = new AssetAllocationReportPdfRenderer(service);
        var request = new GenerateAssetAllocationReportRequestDto
        {
            ContractId = 99,
            FileName = "allocation-actifs-99"
        };

        var rendered = await renderer.RenderAsync(
            new AssetAllocationReportDocumentModel(request),
            BuildDefinition(),
            BuildContext());

        Assert.Same(request, service.AssetAllocationReportRequest);
        Assert.Equal("application/pdf", rendered.ContentType);
        Assert.Equal("allocation-actifs-99.pdf", rendered.FileName);
        Assert.Equal("%PDF-asset-allocation\n"u8.Length, rendered.Content.Length);
        Assert.Equal("99", rendered.Metadata["contractId"]);
        Assert.Equal("False", rendered.Metadata["hasLogo"]);
    }

    private static DocumentDefinition BuildDefinition() =>
        new(
            "asset-allocation-report",
            "Rapport d'allocation d'actifs",
            "pdf-asset-allocation-report-v1",
            "Allocation_actifs_{subjectId}_{date}.pdf",
            "A4",
            "Portrait",
            null,
            SupportsPreview: true,
            SupportsDownload: true,
            SupportsArchive: false,
            SupportsEmail: false,
            typeof(AssetAllocationReportDocumentDataProvider),
            typeof(AssetAllocationReportPdfRenderer),
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
        public GenerateAssetAllocationReportRequestDto? AssetAllocationReportRequest { get; private set; }

        public Task<PdfGeneratedFileDto> GenerateContractSheetAsync(
            GenerateContractSheetRequestDto request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PdfGeneratedFileDto> GenerateOperationsHistoryAsync(
            GenerateOperationsHistoryRequestDto request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PdfGeneratedFileDto> GenerateAssetAllocationReportAsync(
            GenerateAssetAllocationReportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            AssetAllocationReportRequest = request;
            return Task.FromResult(new PdfGeneratedFileDto
            {
                FileName = "allocation-actifs-99.pdf",
                Content = "%PDF-asset-allocation\n"u8.ToArray()
            });
        }

        public Task<PdfGeneratedFileDto> GenerateClientCaseFileAsync(
            GenerateClientCaseFileRequestDto request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
