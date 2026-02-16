using api.Dtos;
using api.Models;
using api.Dtos.Yahoo;

public static class YahooToModelMapper
{
    public static FinancialSupport MapToFinancialSupport(YahooETFDto dto) => new()
    {
        Code = dto.Ticker,
        Label = dto.Label,
        Currency = dto.Currency,
        LastValuationAmount = dto.LastNav,
        LastValuationDate = dto.LastNavDate,
        CreatedDate = DateTime.UtcNow,
        UpdatedDate = DateTime.UtcNow
    };
    public static IEnumerable<SupportHistoricalData> MapToSupportHistoricalData(YahooETFDto dto, int supportId)
    {
        return dto.Historicals.Select(h => new SupportHistoricalData
        {
            FinancialSupportId = supportId,
            Date = h.Date,
            Nav = h.Nav
        });
    }
}
