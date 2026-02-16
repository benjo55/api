using api.Models;

namespace api.Interfaces
{
    public interface ISupportHistoricalDataRepository
    {
        Task<List<SupportHistoricalData>> GetBySupportIdAsync(int supportId);
        Task InsertRangeAsync(List<SupportHistoricalData> entries);
        Task DeleteBySupportIdAsync(int supportId);
    }
}
