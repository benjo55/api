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

public sealed class ClientCaseFileDocumentRendererTests
{
    [Fact]
    public async Task Provider_builds_client_case_file_request_from_subject_and_parameters()
    {
        var provider = new ClientCaseFileDocumentDataProvider();
        using var parameters = JsonDocument.Parse("""
        {
          "fileName": "dossier-client-C123",
          "qrCodeContent": "https://life.local/contracts/12",
          "includeContractSheet": true,
          "includeSituationStatement": false,
          "includeOperationsHistory": true,
          "includeAssetAllocationReport": false,
          "additionalDocuments": [
            { "fileName": "annexe.pdf", "base64Content": "JVBERi0=" }
          ]
        }
        """);

        var model = (ClientCaseFileDocumentModel)await provider.BuildModelAsync(
            BuildDefinition(),
            new GenerateDocumentRequestDto
            {
                SubjectId = "12",
                Parameters = parameters.RootElement.Clone()
            },
            BuildContext());

        Assert.Equal(12, model.Request.ContractId);
        Assert.Equal("dossier-client-C123", model.Request.FileName);
        Assert.Equal("https://life.local/contracts/12", model.Request.QrCodeContent);
        Assert.True(model.Request.IncludeContractSheet);
        Assert.False(model.Request.IncludeSituationStatement);
        Assert.True(model.Request.IncludeOperationsHistory);
        Assert.False(model.Request.IncludeAssetAllocationReport);
        Assert.Single(model.Request.AdditionalDocuments);
    }

    [Fact]
    public async Task Provider_requires_a_valid_contract_id()
    {
        var provider = new ClientCaseFileDocumentDataProvider();

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
        var renderer = new ClientCaseFilePdfMergeRenderer(service);
        var request = new GenerateClientCaseFileRequestDto
        {
            ContractId = 99,
            FileName = "dossier-client-99",
            IncludeContractSheet = true,
            IncludeSituationStatement = true,
            IncludeOperationsHistory = false,
            IncludeAssetAllocationReport = true
        };

        var rendered = await renderer.RenderAsync(
            new ClientCaseFileDocumentModel(request),
            BuildDefinition(),
            BuildContext());

        Assert.Same(request, service.ClientCaseFileRequest);
        Assert.Equal("application/pdf", rendered.ContentType);
        Assert.Equal("dossier-client-99.pdf", rendered.FileName);
        Assert.Equal("%PDF-client-case-file\n"u8.Length, rendered.Content.Length);
        Assert.Equal("99", rendered.Metadata["contractId"]);
        Assert.Equal("False", rendered.Metadata["includeOperationsHistory"]);
    }

    private static DocumentDefinition BuildDefinition() =>
        new(
            "client-case-file",
            "Dossier client",
            "pdf-merge-client-case-file-v1",
            "Dossier_client_{subjectId}_{date}.pdf",
            "A4",
            "Portrait",
            null,
            SupportsPreview: true,
            SupportsDownload: true,
            SupportsArchive: false,
            SupportsEmail: false,
            typeof(ClientCaseFileDocumentDataProvider),
            typeof(ClientCaseFilePdfMergeRenderer),
            DocumentRenderEngine.PdfMerge);

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
        public GenerateClientCaseFileRequestDto? ClientCaseFileRequest { get; private set; }

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
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PdfGeneratedFileDto> GenerateClientCaseFileAsync(
            GenerateClientCaseFileRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ClientCaseFileRequest = request;
            return Task.FromResult(new PdfGeneratedFileDto
            {
                FileName = "dossier-client-99.pdf",
                Content = "%PDF-client-case-file\n"u8.ToArray()
            });
        }
    }
}
