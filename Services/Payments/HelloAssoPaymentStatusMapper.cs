using api.Models.Enum;

namespace api.Services.Payments
{
    public static class HelloAssoPaymentStatusMapper
    {
        public static PaymentStatus Map(string? providerState)
        {
            if (string.IsNullOrWhiteSpace(providerState))
            {
                return PaymentStatus.Unknown;
            }

            return providerState.Trim().ToLowerInvariant() switch
            {
                "authorized" => PaymentStatus.Authorized,
                "pending" => PaymentStatus.Pending,
                "refused" => PaymentStatus.Refused,
                "refunded" => PaymentStatus.Refunded,
                "contested" => PaymentStatus.Contested,
                _ => PaymentStatus.Unknown,
            };
        }
    }
}
