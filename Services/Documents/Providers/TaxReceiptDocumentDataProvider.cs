using api.Dtos.Documents;
using api.Exceptions;
using api.Interfaces.Documents;
using api.Services.Documents.Models;

namespace api.Services.Documents.Providers
{
    public sealed class TaxReceiptDocumentDataProvider : IDocumentDataProvider
    {
        public Task<object> BuildModelAsync(
            DocumentDefinition definition,
            GenerateDocumentRequestDto request,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.SubjectId)
                || !int.TryParse(request.SubjectId, out var taxReceiptId)
                || taxReceiptId <= 0)
            {
                throw new BusinessException("TaxReceiptIdRequired");
            }

            return Task.FromResult<object>(new TaxReceiptDocumentModel(taxReceiptId));
        }
    }
}
