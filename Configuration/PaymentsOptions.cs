using System.ComponentModel.DataAnnotations;

namespace api.Configuration
{
    public sealed class PaymentsOptions
    {
        public const string SectionName = "Payments";

        [Required]
        [Url]
        public string PublicBaseUrl { get; set; } = "http://localhost:5173";

        [Required]
        [RegularExpression("^[A-Z]{3}$")]
        public string DefaultCurrency { get; set; } = "EUR";

        public bool BankTransfersEnabled { get; set; }

        public string BankEncryptionKey { get; set; } = string.Empty;

        public PayPalPaymentOptions PayPal { get; set; } = new();
        public DirectCardPaymentOptions CardProvider { get; set; } = new();
    }

    public sealed class PayPalPaymentOptions
    {
        public bool Enabled { get; set; }
        public string Environment { get; set; } = "Sandbox";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string WebhookId { get; set; } = string.Empty;
    }

    public sealed class DirectCardPaymentOptions
    {
        public bool Enabled { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string PublicKeyAlias { get; set; } = string.Empty;
        public string SecretKeyAlias { get; set; } = string.Empty;
    }
}
