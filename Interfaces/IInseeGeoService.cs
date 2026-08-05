using api.Dtos.Insee;

namespace api.Interfaces
{
    public interface IInseeGeoService
    {
        Task<IReadOnlyCollection<InseeCommuneDto>> SearchCommunesAsync(
            string search,
            int limit,
            CancellationToken cancellationToken);
    }
}
