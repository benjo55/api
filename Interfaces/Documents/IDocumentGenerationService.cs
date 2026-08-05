using System.Security.Claims;
using api.Dtos.Documents;

namespace api.Interfaces.Documents
{
    public interface IDocumentGenerationService
    {
        Task<GeneratedDocumentResult> GenerateAsync(
            string documentType,
            GenerateDocumentRequestDto request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default);
    }

    public sealed record GeneratedDocumentResult(
        Stream Content,
        string ContentType,
        string FileName,
        long? ContentLength,
        string DocumentType,
        string TemplateVersion,
        DateTimeOffset GeneratedAt,
        string? Hash,
        IReadOnlyDictionary<string, string> Metadata);
}
