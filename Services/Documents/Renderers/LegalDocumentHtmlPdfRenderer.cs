using System.Globalization;
using api.Interfaces;
using api.Interfaces.Documents;
using api.Services.Documents.Models;

namespace api.Services.Documents.Renderers
{
    public sealed class LegalDocumentHtmlPdfRenderer : IDocumentRenderer
    {
        private readonly IDocumentRenderService _renderService;
        private readonly IPdfGenerationService _pdfGenerationService;

        public LegalDocumentHtmlPdfRenderer(
            IDocumentRenderService renderService,
            IPdfGenerationService pdfGenerationService)
        {
            _renderService = renderService;
            _pdfGenerationService = pdfGenerationService;
        }

        public async Task<RenderedDocument> RenderAsync(
            object model,
            DocumentDefinition definition,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            var document = (LegalDocumentRevisionDocumentModel)model;
            var renderModel = document.RenderModel;
            var html = _renderService.RenderCanonicalHtml(renderModel);
            var pageFormat = string.IsNullOrWhiteSpace(renderModel.Layout.PageFormat)
                ? definition.EffectiveRenderOptions.PageSize
                : renderModel.Layout.PageFormat;
            var pdf = await _pdfGenerationService.GeneratePdfAsync(html, pageFormat, cancellationToken);
            var suffix = context.DeliveryMode == api.Dtos.Documents.DocumentDeliveryMode.Preview ? "-preview" : string.Empty;

            return new RenderedDocument(
                new MemoryStream(pdf),
                "application/pdf",
                $"{renderModel.Code}-{renderModel.Version}{suffix}.pdf",
                new Dictionary<string, string>
                {
                    ["legalDocumentRevisionId"] = renderModel.RevisionId.ToString(CultureInfo.InvariantCulture),
                    ["legalDocumentCode"] = renderModel.Code,
                    ["legalDocumentName"] = renderModel.Name,
                    ["legalDocumentVersion"] = renderModel.Version,
                    ["contentHash"] = renderModel.ContentHash,
                    ["revisionStamp"] = document.RevisionStamp ?? string.Empty,
                    ["layoutPageFormat"] = pageFormat,
                    ["layoutTemplateVersion"] = renderModel.Layout.TemplateVersion.ToString(CultureInfo.InvariantCulture)
                });
        }
    }
}
