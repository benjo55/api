using api.Dtos.Eod;

public interface ITwelveDataProvider
{
    Task<string?> FindSymbolByIsinAsync(string isin);
    Task<decimal?> GetLastPriceAsync(string symbol);
    Task<List<HistoricalPrice>> GetHistoricalDataAsync(string symbol, string interval = "1day", int outputsize = 30);
}
