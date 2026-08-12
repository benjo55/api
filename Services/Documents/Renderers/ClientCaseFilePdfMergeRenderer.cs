using api.Interfaces;
using api.Interfaces.Documents;
using api.Services.Documents.Models;

namespace api.Services.Documents.Renderers
{
    public sealed class ClientCaseFilePdfMergeRenderer : IDocumentRenderer
    {
        private readonly IPdfBusinessDocumentService _pdfBusinessDocumentService;

        public ClientCaseFilePdfMergeRenderer(IPdfBusinessDocumentService pdfBusinessDocumentService)
        {
            _pdfBusinessDocumentService = pdfBusinessDocumentService;
        }

        public async Task<RenderedDocument> RenderAsync(
            object model,
            DocumentDefinition definition,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            var document = (ClientCaseFileDocumentModel)model;
            var result = await _pdfBusinessDocumentService.GenerateClientCaseFileAsync(
                document.Request,
                cancellationToken);

            var metadata = new Dictionary<string, string>
            {
                ["contractId"] = document.Request.ContractId.ToString(),
                ["includeContractSheet"] = document.Request.IncludeContractSheet.ToString(),
                ["includeSituationStatement"] = document.Request.IncludeSituationStatement.ToString(),
                ["includeOperationsHistory"] = document.Request.IncludeOperationsHistory.ToString(),
                ["includeAssetAllocationReport"] = document.Request.IncludeAssetAllocationReport.ToString(),
                ["additionalDocumentsCount"] = document.Request.AdditionalDocuments.Count.ToString()
            };

            return new RenderedDocument(
                new MemoryStream(result.Content),
                "application/pdf",
                result.FileName,
                metadata);
        }
    }
}
