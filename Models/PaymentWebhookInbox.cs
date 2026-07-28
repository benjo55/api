using api.Models.Enum;

namespace api.Models
{
    public class PaymentWebhookInbox
    {
        public int Id { get; set; }
        public PaymentProvider Provider { get; set; } = PaymentProvider.HelloAsso;
        public string PayloadHash { get; set; } = string.Empty;
        public string? EventType { get; set; }
        public string? ExternalObjectId { get; set; }
        public string RawPayload { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public WebhookProcessingStatus ProcessingStatus { get; set; } = WebhookProcessingStatus.Pending;
        public int AttemptCount { get; set; }
        public string? LastError { get; set; }
    }
}
