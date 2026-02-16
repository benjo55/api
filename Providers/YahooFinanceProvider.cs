using YahooFinanceApi;
using api.Dtos;
using api.Dtos.Yahoo;
using api.Dtos.FinancialSupport;
using System.Text.Json;

public class YahooFinanceProvider : IYahooFinanceProvider
{
    private readonly HttpClient _httpClient;
    private readonly IEodDataProvider _eodProvider;

    // Exemple minimal à inclure dans ton provider (ou ailleurs)
    private static readonly Dictionary<string, string> IsinToYahooTicker = new()
    {
        // ETF Monde Amundi
        { "LU1681043599", "CW8.PA" },
        // Lyxor MSCI Emerging Markets UCITS ETF
        { "LU1829221024", "LEM.PA" },
        // Amundi S&P 500 UCITS ETF EUR
        { "LU1681048805", "500.PA" },
        // Lyxor Nasdaq-100 UCITS ETF
        { "FR0010342592", "EQQQ.PA" },
        // Lyxor PEA Monde
        { "FR0011869353", "PEA.PA" },
        // Lyxor ETF CAC 40
        { "FR0007052782", "CAC.PA" },
        // iShares Core MSCI World UCITS ETF
        { "IE00B4L5Y983", "SWDA.L" },
        // iShares Core S&P 500 UCITS ETF
        { "IE00B5BMR087", "CSPX.L" },
        // iShares Core MSCI EM IMI UCITS ETF
        { "IE00BKM4GZ66", "EIMI.L" }
    };

    public YahooFinanceProvider(HttpClient httpClient, IEodDataProvider eodProvider)
    {
        _httpClient = httpClient;
        _eodProvider = eodProvider;

    }

    public async Task<YahooETFDto> GetEtfDataAsync(string codeOrIsin)
    {
        // Cherche si l'utilisateur a fourni un ISIN ou un ticker Yahoo déjà
        var ticker = IsinToYahooTicker.TryGetValue(codeOrIsin, out var yahooTicker)
            ? yahooTicker
            : codeOrIsin;

        var securities = await Yahoo
            .Symbols(ticker)
            .Fields(Field.Symbol, Field.LongName, Field.Currency, Field.RegularMarketPrice, Field.RegularMarketTime)
            .QueryAsync();

        if (!securities.ContainsKey(ticker))
            throw new Exception($"Le ticker '{ticker}' n'existe pas sur Yahoo Finance (issu de {codeOrIsin}).");

        var etf = securities[ticker];

        // 2. Historique sur 1 an (modifie si besoin)
        var historic = await Yahoo.GetHistoricalAsync(ticker, DateTime.Now.AddYears(-1), DateTime.Now, Period.Daily);

        var dto = new YahooETFDto
        {
            Ticker = ticker,
            Label = etf[Field.LongName]?.ToString() ?? ticker,
            Currency = etf[Field.Currency]?.ToString() ?? "EUR",
            LastNav = Convert.ToDecimal(etf[Field.RegularMarketPrice] ?? 0),
            LastNavDate = ((DateTimeOffset?)etf[Field.RegularMarketTime])?.DateTime ?? DateTime.Now,
            Historicals = historic
                .Select(h => new HistoricalNavDto { Date = h.DateTime, Nav = h.Close })
                .ToList()
        };

        return dto;
    }

    public async Task<CreateFinancialSupportRequestDto?> GetBasicInfoByIsinAsync(string isin)
    {
        // 1. Premier essai avec l'ISIN
        var url = $"https://query2.finance.yahoo.com/v1/finance/search?q={isin}";
        var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);

        if (response.TryGetProperty("quotes", out var quotes) &&
            quotes.ValueKind == JsonValueKind.Array &&
            quotes.GetArrayLength() > 0)
        {
            var quote = quotes[0];

            var dto = new CreateFinancialSupportRequestDto
            {
                ISIN = isin,
                MarketingName = quote.GetPropertyOrNull("shortname"),
                LegalName = quote.GetPropertyOrNull("longname"),
                Currency = quote.GetPropertyOrNull("currency"),
                BloombergCode = quote.GetPropertyOrNull("symbol"),
                PrimaryListingMarket = quote.GetPropertyOrNull("exchange"),
                Status = "Actif"
            };

            dto.Code = $"{dto.MarketingName?.Replace(" ", "")?.Substring(0, 10)}-{isin}";
            return dto;
        }

        // 2. Fallback via ticker EOD
        var ticker = await _eodProvider.FindTickerByIsinAsync(isin);
        if (ticker == null) return null;

        var fallbackUrl = $"https://query2.finance.yahoo.com/v1/finance/search?q={ticker}";
        var fallbackResponse = await _httpClient.GetFromJsonAsync<JsonElement>(fallbackUrl);

        if (fallbackResponse.TryGetProperty("quotes", out var fallbackQuotes) &&
            fallbackQuotes.ValueKind == JsonValueKind.Array &&
            fallbackQuotes.GetArrayLength() > 0)
        {
            var quote = fallbackQuotes[0];

            var dto = new CreateFinancialSupportRequestDto
            {
                ISIN = isin,
                MarketingName = quote.GetPropertyOrNull("shortname"),
                LegalName = quote.GetPropertyOrNull("longname"),
                Currency = quote.GetPropertyOrNull("currency"),
                BloombergCode = quote.GetPropertyOrNull("symbol"),
                PrimaryListingMarket = quote.GetPropertyOrNull("exchange"),
                Status = "Actif"
            };

            dto.Code = $"{dto.MarketingName?.Replace(" ", "")?.Substring(0, 10)}-{isin}";
            return dto;
        }

        return null;
    }


}
