using System.ComponentModel.DataAnnotations;

namespace api.Configuration
{
    public sealed class DonationCheckoutOptions
    {
        public const string SectionName = "DonationCheckout";

        [Range(0.01, 1000000)]
        public decimal MinAmountEur { get; set; } = 1.00m;

        [Range(0.01, 1000000)]
        public decimal MaxAmountEur { get; set; } = 10000.00m;

        [Range(1, 120)]
        public int StatusPollingMaxSeconds { get; set; } = 120;

        [Range(1, 120)]
        public int ReceiptTokenLifetimeMinutes { get; set; } = 15;
    }
}
