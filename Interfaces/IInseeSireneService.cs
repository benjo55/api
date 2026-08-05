using api.Dtos.Insurer;

namespace api.Interfaces
{
    public interface IInseeSireneService
    {
        Task<IReadOnlyCollection<InsurerSireneSearchDto>> SearchInsurersAsync(
            string search,
            int limit,
            CancellationToken cancellationToken);
    }
}
