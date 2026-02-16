using api.Dtos.Eod;

public interface IEodDataProvider
{
    Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker, DateTime from, DateTime to);
    Task<string?> FindTickerByIsinAsync(string isin);
    Task<EodFundProfile?> GetFundProfileAsync(string ticker);
    Task<(decimal Value, DateTime Date)?> GetLatestPriceAsync(string isin);
    // Ajout pour Twelve Data
}
public class HistoricalPrice
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}