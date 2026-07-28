using api.Dtos.Generic;
using api.Dtos.Me;
using api.Helpers;

namespace api.Interfaces
{
    public interface IMeDonationsService
    {
        Task<PagedResult<MeDonationListItemDto>> GetMyDonationsAsync(int userId, QueryObject query, CancellationToken cancellationToken = default);
        Task<MeDonationDetailDto?> GetMyDonationAsync(int userId, string publicId, CancellationToken cancellationToken = default);
        Task<MeDonationIntentCreatedDto> CreateDonationIntentAsync(int userId, CreateMeDonationIntentDto dto, CancellationToken cancellationToken = default);
        Task<(byte[] Content, string FileName)?> DownloadMyReceiptAsync(int userId, string publicId, CancellationToken cancellationToken = default);
        Task<MeDonationReceiptResendResultDto?> ResendMyReceiptAsync(int userId, string publicId, string? userName, CancellationToken cancellationToken = default);
    }
}
