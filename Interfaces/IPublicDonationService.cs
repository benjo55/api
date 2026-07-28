using api.Dtos.PublicDonations;

namespace api.Interfaces
{
    public interface IPublicDonationService
    {
        Task<PublicDonationCheckoutResponse> InitializeCheckoutAsync(PublicDonationCheckoutRequest request, CancellationToken cancellationToken);
        Task<PublicDonationStatusResponse?> GetPublicStatusAsync(string publicId, CancellationToken cancellationToken);
        Task<PublicDonationReceiptTokenResponse?> CreateReceiptTokenAsync(string publicId, CancellationToken cancellationToken);
        Task<(byte[] Content, string FileName)?> DownloadReceiptAsync(string publicId, string token, CancellationToken cancellationToken);
        Task ProcessWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, string? remoteIpAddress, CancellationToken cancellationToken);
        Task<int> ProcessPendingWebhooksAsync(CancellationToken cancellationToken);
        Task ForceReconcileAsync(int donationId, CancellationToken cancellationToken);
        Task ResendReceiptAsync(int donationId, CancellationToken cancellationToken);
    }
}
