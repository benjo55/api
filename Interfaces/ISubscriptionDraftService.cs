using System.Text.Json;
using api.Dtos.Subscription;

namespace api.Interfaces
{
    public interface ISubscriptionDraftService
    {
        Task<SubscriptionDraftDto?> GetCurrentAsync(int userId, CancellationToken cancellationToken);
        Task<SubscriptionDraftDto> CreateAsync(int userId, CancellationToken cancellationToken);
        Task<SubscriptionDraftDto?> GetByIdAsync(int userId, int draftId, CancellationToken cancellationToken);
        Task<SubscriptionDraftDto> SaveStepAsync(int userId, int draftId, string stepKey, JsonElement data, CancellationToken cancellationToken);
        Task<SubscriptionDraftDto> ComputeInvestorProfileAsync(int userId, int draftId, CancellationToken cancellationToken);
        Task<SubscriptionDraftDto> GenerateRecommendationAsync(int userId, int draftId, CancellationToken cancellationToken);
        Task<SubscriptionDraftDto> AcceptRecommendationAsync(int userId, int draftId, CancellationToken cancellationToken);
        Task<SubscriptionDraftDto> OverrideRecommendationAsync(int userId, int draftId, string reason, CancellationToken cancellationToken);
        Task<SubscriptionDraftDto> SubmitAsync(int userId, int draftId, CancellationToken cancellationToken);
    }
}
