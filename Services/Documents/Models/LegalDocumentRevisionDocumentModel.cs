using api.Dtos.LegalDocuments;

namespace api.Services.Documents.Models
{
    public sealed record LegalDocumentRevisionDocumentModel(
        DocumentRenderModel RenderModel,
        string? RevisionStamp);
}
