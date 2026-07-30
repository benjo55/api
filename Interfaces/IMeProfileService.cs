using api.Dtos.Me;

namespace api.Interfaces
{
    public interface IMeProfileService
    {
        Task<MeProfileDto> GetProfileAsync(int userId, CancellationToken cancellationToken = default);
        Task<MeProfileDto> UpdateProfileAsync(int userId, SaveMeProfileDto dto, CancellationToken cancellationToken = default);
        Task<MeDashboardDto> GetDashboardAsync(int userId, CancellationToken cancellationToken = default);
        Task<MePrivateSpaceDto> GetPrivateSpaceAsync(int userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DonationOrganizationOptionDto>> GetDonationOrganizationsAsync(CancellationToken cancellationToken = default);
    }
}
