using api.Models.Enum;

namespace api.Models
{
    public class TaxReceiptEmailHistory
    {
        public int Id { get; set; }
        public int TaxReceiptId { get; set; }
        public TaxReceipt TaxReceipt { get; set; } = null!;
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public TaxReceiptEmailStatus Status { get; set; } = TaxReceiptEmailStatus.Pending;
        public DateTime? SentAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
