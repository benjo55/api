using api.Interfaces;
using api.Interfaces.Documents;
using api.Services.Documents.Models;

namespace api.Services.Documents.Renderers
{
    public sealed class AssetAllocationReportPdfRenderer : IDocumentRenderer
    {
        private readonly IPdfBusinessDocumentService _pdfBusinessDocumentService;

        public AssetAllocationReportPdfRenderer(IPdfBusinessDocumentService pdfBusinessDocumentService)
        {
            _pdfBusinessDocumentService = pdfBusinessDocumentService;
        }

        public async Task<RenderedDocument> RenderAsync(
            object model,
            DocumentDefinition definition,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            var document = (AssetAllocationReportDocumentModel)model;
            var result = await _pdfBusinessDocumentService.GenerateAssetAllocationReportAsync(
                document.Request,
                cancellationToken);

            var metadata = new Dictionary<string, string>
            {
                ["contractId"] = document.Request.ContractId.ToString(),
                ["hasLogo"] = (!string.IsNullOrWhiteSpace(document.Request.LogoBase64) ||
                               !string.IsNullOrWhiteSpace(document.Request.LogoUrl)).ToString()
            };

            return new RenderedDocument(
                new MemoryStream(result.Content),
                "application/pdf",
                result.FileName,
                metadata);
        }
    }
}
