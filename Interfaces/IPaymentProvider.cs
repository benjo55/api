namespace api.Interfaces
{
    public interface IPaymentProvider
    {
        Task<CreateCheckoutResult> CreateCheckoutAsync(
            CreateCheckoutCommand command,
            CancellationToken cancellationToken);

        Task<PaymentReconciliationResult> ReconcilePaymentAsync(
            PaymentReconciliationCommand command,
            CancellationToken cancellationToken);

        Task<WebhookReceptionResult> ReceiveWebhookAsync(
            string rawBody,
            IReadOnlyDictionary<string, string> headers,
            string? remoteIpAddress,
            CancellationToken cancellationToken);
    }

    public sealed record CreateCheckoutCommand(
        string OrganizationSlug,
        int AmountInCents,
        string ItemName,
        string ReturnUrl,
        string BackUrl,
        string ErrorUrl,
        string FirstName,
        string LastName,
        string Email,
        string Address,
        string ZipCode,
        string City,
        string Country,
        IReadOnlyDictionary<string, string> Metadata,
        string? CredentialKey = null);

    public sealed record CreateCheckoutResult(
        bool Success,
        string? CheckoutIntentId,
        string? RedirectUrl,
        string? ErrorCode,
        string? ErrorMessage,
        string? RawTechnicalPayload);

    public sealed record PaymentReconciliationCommand(
        string OrganizationSlug,
        string CheckoutIntentId,
        string? CredentialKey = null);

    public sealed record PaymentReconciliationResult(
        bool Found,
        bool IsAuthorized,
        string? ExternalOrderId,
        string? ExternalPaymentId,
        string? ProviderPaymentState,
        int? AmountInCents,
        string? Currency,
        IReadOnlyDictionary<string, string> Metadata,
        string? ErrorCode,
        string? ErrorMessage,
        string? RawTechnicalPayload);

    public sealed record WebhookReceptionResult(
        bool Accepted,
        string? EventType,
        string? ExternalObjectId,
        string? ErrorCode,
        string? ErrorMessage);
}
