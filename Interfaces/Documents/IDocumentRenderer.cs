namespace api.Interfaces.Documents
{
    public interface IDocumentRenderer
    {
        Task<RenderedDocument> RenderAsync(
            object model,
            DocumentDefinition definition,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default);
    }

    public sealed record RenderedDocument(
        Stream Content,
        string ContentType,
        string? FileName,
        IReadOnlyDictionary<string, string> Metadata);
}
