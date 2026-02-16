
using api.Dtos.FinancialSupport;
using System.Net.Http.Json;
using System.Text.Json;

public class YahooDataProvider
{
    private readonly HttpClient _httpClient;

    public YahooDataProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreateFinancialSupportRequestDto?> GetBasicInfoByIsinAsync(string isin)
    {
        var url = $"https://query2.finance.yahoo.com/v1/finance/search?q={isin}";
        var json = await _httpClient.GetFromJsonAsync<JsonElement>(url);

        if (json.TryGetProperty("quotes", out var quotes) && quotes.ValueKind == JsonValueKind.Array && quotes.GetArrayLength() > 0)
        {
            var quote = quotes[0]; // Premier résultat

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
