using System.Text.Json;
using api.Models;
using api.Models.Enum;

namespace api.Dtos.Subscription
{
    public sealed record SaveSubscriptionStepRequestDto(JsonElement Data, string? CurrentStep = null);

    public sealed record RecommendationOverrideRequestDto(string Reason);

    public sealed record SubscriptionValidationResultDto(bool IsValid, string[] Errors, string[] Warnings);

    public sealed record SubscriptionDocumentDto(
        int? LegalDocumentRevisionId,
        string DocumentCode,
        string DocumentName,
        string Version,
        string Role,
        string RoleLabel,
        string Status,
        int? ArtifactId,
        string? FileName,
        DateTime? GeneratedAt,
        string? DownloadUrl);

    public sealed record SubscriptionDocumentDossierDto(
        int DraftId,
        int? ProductId,
        string? ProductLabel,
        IReadOnlyList<SubscriptionDocumentDto> Documents,
        bool IsComplete,
        IReadOnlyList<string> Warnings);

    public sealed record SubscriptionDocumentFileDto(
        string FileName,
        string ContentType,
        byte[] Content);

    public sealed record SubscriptionMfaChallengeDto(
        int DraftId,
        string Channel,
        string MaskedTarget,
        DateTime ExpiresAt,
        bool DeliverySucceeded,
        string? DebugCode);

    public sealed record SubscriptionTotpSetupDto(
        int DraftId,
        bool AlreadyEnabled,
        string Issuer,
        string AccountName,
        string? Secret,
        string OtpAuthUri,
        string QrCodeDataUri,
        string Message);

    public sealed record SubscriptionMfaVerifyRequestDto(string Code);

    public sealed record SubscriptionMfaVerificationDto(
        int DraftId,
        bool Verified,
        DateTime? VerifiedAt,
        DateTime? ExpiresAt,
        string Message);

    public sealed record SubscriptionPaymentPreparationDto(
        int DraftId,
        string PaymentMode,
        decimal InitialAmount,
        bool ScheduledPaymentEnabled,
        decimal ScheduledAmount,
        string? ScheduledFrequency,
        string? MaskedIban,
        string Status,
        DateTime PreparedAt);

    public sealed record SubscriptionSignatureEnvelopeDto(
        int DraftId,
        string EnvelopeReference,
        string Provider,
        string Status,
        DateTime PreparedAt,
        IReadOnlyList<string> RequiredDocuments);

    public sealed record SubscriptionRecommendationDto(
        string Id,
        int SubscriptionDraftId,
        ContractFamily? ProductType,
        int? ContractId,
        string ManagementMode,
        string RiskLevel,
        string RecommendedHorizon,
        IReadOnlyList<SubscriptionAllocationDto> RecommendedAllocation,
        IReadOnlyList<string> Reasons,
        IReadOnlyList<string> Warnings,
        DateTime GeneratedAt,
        string RulesVersion,
        DateTime? AcceptedAt,
        DateTime? OverriddenAt,
        string? OverrideReason);

    public sealed record SubscriptionAllocationDto(string Label, decimal Percentage, string RiskLevel);

    public sealed record SubscriptionDraftDto(
        int Id,
        int UserId,
        ContractFamily? ProductType,
        int? ProductId,
        string? ProductLabel,
        string CurrentStep,
        int HighestCompletedStep,
        SubscriptionDraftStatus Status,
        JsonElement? ProjectData,
        JsonElement? SituationData,
        JsonElement? InvestorProfileData,
        JsonElement? RecommendationData,
        JsonElement? InvestmentData,
        JsonElement? ProtectionData,
        IReadOnlyDictionary<string, SubscriptionStepStatus> StepStatuses,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? SubmittedAt,
        DateTime? SignedAt,
        int Version);
}
