using System.Text.Json;
using api.Models;
using api.Models.Enum;

namespace api.Dtos.Subscription
{
    public sealed record SaveSubscriptionStepRequestDto(JsonElement Data, string? CurrentStep = null);

    public sealed record RecommendationOverrideRequestDto(string Reason);

    public sealed record SubscriptionValidationResultDto(bool IsValid, string[] Errors, string[] Warnings);

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
