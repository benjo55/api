using api.Dtos.Subscription;

namespace api.Interfaces
{
    public interface ISubscriptionMfaService
    {
        Task<SubscriptionMfaChallengeDto> CreateChallengeAsync(int userId, int draftId, string? ipAddress, CancellationToken cancellationToken);
        Task<SubscriptionTotpSetupDto> CreateTotpSetupAsync(int userId, int draftId, CancellationToken cancellationToken);
        Task<SubscriptionMfaVerificationDto> VerifyAsync(int userId, int draftId, string code, CancellationToken cancellationToken);
        Task<bool> HasRecentVerificationAsync(int userId, int draftId, CancellationToken cancellationToken);
    }

    public interface ISubscriptionPaymentPreparationService
    {
        Task<SubscriptionPaymentPreparationDto> PrepareAsync(int userId, int draftId, CancellationToken cancellationToken);
        Task<bool> IsPreparedAsync(int userId, int draftId, CancellationToken cancellationToken);
    }

    public interface ISubscriptionSignatureService
    {
        Task<SubscriptionSignatureEnvelopeDto> PrepareEnvelopeAsync(int userId, int draftId, string? userName, CancellationToken cancellationToken);
        Task<bool> IsEnvelopePreparedAsync(int userId, int draftId, CancellationToken cancellationToken);
    }
}
