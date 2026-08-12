using api.Interfaces;
using api.Interfaces.Documents;
using api.Services.Documents.Models;

namespace api.Services.Documents.Renderers
{
    public sealed class TaxReceiptPdfRenderer : IDocumentRenderer
    {
        private readonly ITaxReceiptService _taxReceiptService;

        public TaxReceiptPdfRenderer(ITaxReceiptService taxReceiptService)
        {
            _taxReceiptService = taxReceiptService;
        }

        public async Task<RenderedDocument> RenderAsync(
            object model,
            DocumentDefinition definition,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            var taxReceipt = (TaxReceiptDocumentModel)model;
            var generation = await _taxReceiptService.GenerateAsync(
                taxReceipt.TaxReceiptId,
                context.UserName,
                cancellationToken);
            var pdf = await _taxReceiptService.GetPdfAsync(
                taxReceipt.TaxReceiptId,
                cancellationToken);

            var metadata = new Dictionary<string, string>
            {
                ["taxReceiptId"] = taxReceipt.TaxReceiptId.ToString(),
                ["taxReceiptNumber"] = generation.Receipt.ReceiptNumber,
                ["cerfaCode"] = generation.Receipt.CerfaCode,
                ["cerfaVersion"] = generation.Receipt.CerfaVersion,
                ["donationId"] = generation.Receipt.DonationId.ToString(),
                ["beneficiaryOrganizationId"] = generation.Receipt.BeneficiaryOrganizationId.ToString(),
                ["status"] = generation.Receipt.Status.ToString()
            };

            return new RenderedDocument(
                new MemoryStream(pdf.Content),
                "application/pdf",
                pdf.FileName,
                metadata);
        }
    }
}
