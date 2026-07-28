using api.Models.Enum;

namespace api.Models
{
    public class PaymentAttempt
    {
        public int Id { get; set; }
        public int DonationId { get; set; }
        public Donation Donation { get; set; } = null!;
        public PaymentProvider Provider { get; set; } = PaymentProvider.HelloAsso;
        public string InternalReference { get; set; } = string.Empty;
        public string? IdempotencyKey { get; set; }
        public string? ProviderCheckoutIntentId { get; set; }
        public string? ProviderOrderId { get; set; }
        public string? ProviderPaymentId { get; set; }
        public string? ProviderPaymentState { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EUR";
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Created;
        public string? RedirectUrl { get; set; }
        public DateTime? CheckoutUrlExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AuthorizedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? FailureCode { get; set; }
        public string? FailureMessage { get; set; }
        public DateTime? LastReconciledAt { get; set; }
        public DateTime? DonorTransferDeclaredAt { get; set; }
        public string? DonorTransferDeclarationComment { get; set; }
        public int? ConfirmedByUserId { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? AdminNote { get; set; }
        public byte[]? RowVersion { get; set; }
    }
}
