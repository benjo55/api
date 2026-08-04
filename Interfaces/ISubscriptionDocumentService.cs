using api.Dtos.Subscription;

namespace api.Interfaces
{
    public interface ISubscriptionDocumentService
    {
        Task<SubscriptionDocumentDossierDto> GetDossierAsync(int userId, int draftId, CancellationToken cancellationToken);
        Task<SubscriptionDocumentDossierDto> GenerateDossierAsync(int userId, int draftId, string? userName, CancellationToken cancellationToken);
        Task<SubscriptionDocumentFileDto> GetDocumentFileAsync(int userId, int draftId, int artifactId, CancellationToken cancellationToken);
    }
}
