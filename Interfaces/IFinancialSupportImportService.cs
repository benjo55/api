using api.Dtos.FinancialSupport;

namespace api.Interfaces
{
    public interface IFinancialSupportImportService
    {
        Task ImportFromYahooAsync(string ticker);
        Task<List<HistoricalPrice>> ImportFromEodAsync(string ticker);
        Task<object?> SearchSupportByIsinFromEodAsync(string isin);
        Task<(CreateFinancialSupportRequestDto?, string?)> ImportFromYahooThenEodAsync(string isin);
        Task<(CreateFinancialSupportRequestDto?, string?)> ImportFromTwelveAsync(string isin);
        Task<(CreateFinancialSupportRequestDto?, string?, string)> ImportFromAnyProviderAsync(string isin);
        Task ImportFromEodByIsinAsync(int supportId, string isin);
    }
}
