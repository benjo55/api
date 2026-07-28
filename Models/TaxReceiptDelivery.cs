using api.Models.Enum;

namespace api.Models
{
    public class TaxReceiptDelivery
    {
        public int Id { get; set; }
        public int TaxReceiptId { get; set; }
        public TaxReceipt TaxReceipt { get; set; } = null!;
        public string RecipientEmail { get; set; } = string.Empty;
        public string DeliveryType { get; set; } = string.Empty;
        public DateTime? SentAt { get; set; }
        public TaxReceiptEmailStatus DeliveryStatus { get; set; } = TaxReceiptEmailStatus.Pending;
        public int AttemptCount { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public string? LastError { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}