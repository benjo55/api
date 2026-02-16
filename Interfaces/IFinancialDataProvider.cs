// Interfaces/IFinancialDataProvider.cs
namespace api.Interfaces
{
    public interface IFinancialDataProvider
    {
        Task<decimal?> GetLatestNavAsync(string isin);
    }
}
