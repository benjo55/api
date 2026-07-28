namespace api.Dtos.Me
{
    public sealed record MeDonationPaymentOptionsDto(
        string PublicId,
        string Reference,
        decimal Amount,
        string Currency,
        string Status,
        bool IsPayable,
        bool HelloAssoAvailable,
        bool BankTransferAvailable,
        bool PayPalAvailable,
        bool CardProviderAvailable,
        string? Message);

    public sealed record MeHelloAssoPaymentStartedDto(
        string PublicId,
        string Reference,
        int PaymentAttemptId,
        string PaymentReference,
        string RedirectUrl,
        DateTime? ExpiresAt);

    public sealed record MeBankTransferInstructionsDto(
        string PublicId,
        string Reference,
        int PaymentAttemptId,
        string PaymentReference,
        decimal Amount,
        string Currency,
        string AccountHolder,
        string Iban,
        string Bic,
        string? BankName,
        string CountryCode,
        string? Instructions,
        string Communication);

    public sealed record DeclareBankTransferDto(string? Comment);

    public sealed record MeDonationPaymentStatusDto(
        string PublicId,
        string Reference,
        string DonationStatus,
        string? Provider,
        string? PaymentStatus,
        DateTime? PaymentConfirmedAt,
        bool ReceiptAvailable,
        string? ReceiptNumber,
        string? Message);
}
