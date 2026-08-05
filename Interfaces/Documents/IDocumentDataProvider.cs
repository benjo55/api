using api.Dtos.Documents;

namespace api.Interfaces.Documents
{
    public interface IDocumentDataProvider
    {
        Task<object> BuildModelAsync(
            DocumentDefinition definition,
            GenerateDocumentRequestDto request,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default);
    }
}
