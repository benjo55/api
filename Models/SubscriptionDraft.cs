using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public enum SubscriptionDraftStatus
    {
        Draft = 0,
        InProgress = 1,
        AwaitingDocuments = 2,
        AwaitingSignature = 3,
        Signed = 4,
        PaymentPending = 5,
        Active = 6,
        Rejected = 7,
        Cancelled = 8,
        Expired = 9
    }

    public enum SubscriptionStepStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        Invalidated = 3,
        NotApplicable = 4
    }

    public class SubscriptionDraft
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public ContractFamily? ProductType { get; set; }

        public int? ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [MaxLength(40)]
        public string CurrentStep { get; set; } = "project";

        public int HighestCompletedStep { get; set; }

        public SubscriptionDraftStatus Status { get; set; } = SubscriptionDraftStatus.Draft;

        public string? ProjectDataJson { get; set; }

        public string? SituationDataJson { get; set; }

        public string? InvestorProfileDataJson { get; set; }

        public string? RecommendationDataJson { get; set; }

        public string? InvestmentDataJson { get; set; }

        public string? ProtectionDataJson { get; set; }

        public string? StepStatusesJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SubmittedAt { get; set; }

        public DateTime? SignedAt { get; set; }

        public int Version { get; set; } = 1;

        public ICollection<SubscriptionDraftAuditEvent> AuditEvents { get; set; } = new List<SubscriptionDraftAuditEvent>();
    }

    public class SubscriptionDraftAuditEvent
    {
        public int Id { get; set; }

        public int SubscriptionDraftId { get; set; }

        [ForeignKey(nameof(SubscriptionDraftId))]
        public SubscriptionDraft? SubscriptionDraft { get; set; }

        public int UserId { get; set; }

        [MaxLength(80)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? StepKey { get; set; }

        public string? PreviousStateJson { get; set; }

        public string? NewStateJson { get; set; }

        [MaxLength(40)]
        public string RulesVersion { get; set; } = "subscription-rules-v1";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
