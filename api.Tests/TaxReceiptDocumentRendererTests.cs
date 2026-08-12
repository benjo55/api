using System.Security.Claims;
using api.Dtos.Documents;
using api.Dtos.Generic;
using api.Dtos.TaxReceipts;
using api.Exceptions;
using api.Helpers;
using api.Interfaces;
using api.Interfaces.Documents;
using api.Models.Enum;
using api.Services.Documents.Models;
using api.Services.Documents.Providers;
using api.Services.Documents.Renderers;
using Xunit;

namespace api.Tests;

public sealed class TaxReceiptDocumentRendererTests
{
    [Fact]
    public async Task Provider_requires_a_valid_tax_receipt_id()
    {
        var provider = new TaxReceiptDocumentDataProvider();
        var definition = BuildDefinition();
        var context = BuildContext();

        await Assert.ThrowsAsync<BusinessException>(() =>
            provider.BuildModelAsync(
                definition,
                new GenerateDocumentRequestDto { SubjectId = "not-an-id" },
                context));
    }

    [Fact]
    public async Task Renderer_regenerates_and_returns_tax_receipt_pdf()
    {
        var service = new RecordingTaxReceiptService();
        var renderer = new TaxReceiptPdfRenderer(service);
        var definition = BuildDefinition();

        var rendered = await renderer.RenderAsync(
            new TaxReceiptDocumentModel(42),
            definition,
            BuildContext());

        Assert.Equal(42, service.GeneratedTaxReceiptId);
        Assert.Equal(42, service.DownloadedTaxReceiptId);
        Assert.Equal("application/pdf", rendered.ContentType);
        Assert.Equal("recu-fiscal.pdf", rendered.FileName);
        Assert.Equal("%PDF-test\n"u8.Length, rendered.Content.Length);
        Assert.Equal("RF-2026-000042", rendered.Metadata["taxReceiptNumber"]);
        Assert.Equal("2041-RD", rendered.Metadata["cerfaCode"]);
    }

    private static DocumentDefinition BuildDefinition() =>
        new(
            "tax-receipt",
            "Reçu fiscal",
            "pdf-template-tax-receipt-v1",
            "Recu_fiscal_{subjectId}_{date}.pdf",
            "A4",
            "Portrait",
            null,
            SupportsPreview: true,
            SupportsDownload: true,
            SupportsArchive: false,
            SupportsEmail: false,
            typeof(TaxReceiptDocumentDataProvider),
            typeof(TaxReceiptPdfRenderer),
            DocumentRenderEngine.PdfTemplateOverlay);

    private static DocumentGenerationContext BuildContext() =>
        new(
            new ClaimsPrincipal(new ClaimsIdentity()),
            UserId: 7,
            UserName: "alice",
            Locale: "fr-FR",
            TimeZone: "Europe/Paris",
            GeneratedAt: DateTimeOffset.UtcNow,
            DeliveryMode: DocumentDeliveryMode.Preview,
            CorrelationId: "test",
            DataAsOfDate: null);

    private sealed class RecordingTaxReceiptService : ITaxReceiptService
    {
        public int? GeneratedTaxReceiptId { get; private set; }
        public int? DownloadedTaxReceiptId { get; private set; }

        public Task<PagedResult<TaxReceiptDto>> GetAllAsync(QueryObject query, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<TaxReceiptDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<TaxReceiptDto> CreateForDonationAsync(int donationId, CreateTaxReceiptDto dto, string? userName, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<TaxReceiptDto?> ValidateAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<TaxReceiptGenerationResultDto> GenerateAsync(int id, string? userName, CancellationToken cancellationToken = default)
        {
            GeneratedTaxReceiptId = id;
            return Task.FromResult(new TaxReceiptGenerationResultDto(BuildReceipt(id), $"/tax-receipts/{id}/pdf"));
        }

        public Task<(byte[] Content, string FileName)> GetPdfAsync(int id, CancellationToken cancellationToken = default)
        {
            DownloadedTaxReceiptId = id;
            return Task.FromResult(("%PDF-test\n"u8.ToArray(), "recu-fiscal.pdf"));
        }

        public Task<TaxReceiptDto?> CancelAsync(int id, string? reason, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<TaxReceiptDto> ReplaceAsync(int id, string? userName, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<TaxReceiptEmailHistoryDto>> GetEmailHistoryAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        private static TaxReceiptDto BuildReceipt(int id) =>
            new(
                id,
                DonationId: 100,
                BeneficiaryOrganizationId: 200,
                ReceiptNumber: "RF-2026-000042",
                CerfaCode: "2041-RD",
                CerfaVersion: "11580*05",
                Status: TaxReceiptStatus.Generated,
                GeneratedFileName: "recu-fiscal.pdf",
                PdfHash: "hash",
                GeneratedAt: DateTime.UtcNow,
                GeneratedBy: "alice",
                SentAt: null,
                SentToEmail: null,
                LastEmailStatus: null,
                DonorFullName: "Alice Martin",
                DonationAmount: 120m,
                DonationDate: new DateTime(2026, 8, 10));
    }
}
