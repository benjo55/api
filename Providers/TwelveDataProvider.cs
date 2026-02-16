using System.Text.Json;

public class TwelveDataProvider : ITwelveDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public TwelveDataProvider(IConfiguration config, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = config["TwelveData:ApiKey"]!;
    }

    public async Task<string?> FindSymbolByIsinAsync(string isin)
    {
        var url = $"https://api.twelvedata.com/symbol_search?symbol={isin}&apikey={_apiKey}";
        var json = await _httpClient.GetFromJsonAsync<JsonElement>(url);

        if (json.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Array &&
            data.GetArrayLength() > 0)
        {
            var symbol = data[0].GetPropertyOrNull("symbol");
            var exchange = data[0].GetPropertyOrNull("exchange");
            return !string.IsNullOrEmpty(exchange) ? $"{symbol}:{exchange}" : symbol;
        }

        return null;
    }

    public async Task<decimal?> GetLastPriceAsync(string symbol)
    {
        var url = $"https://api.twelvedata.com/price?symbol={symbol}&apikey={_apiKey}";
        var json = await _httpClient.GetFromJsonAsync<JsonElement>(url);
        return json.TryGetProperty("price", out var p) ? decimal.Parse(p.ToString()!) : null;
    }

    public async Task<List<HistoricalPrice>> GetHistoricalDataAsync(string symbol, string interval = "1day", int outputsize = 30)
    {
        var url = $"https://api.twelvedata.com/time_series?symbol={symbol}&interval={interval}&outputsize={outputsize}&apikey={_apiKey}&format=JSON";
        var json = await _httpClient.GetFromJsonAsync<JsonElement>(url);

        var list = new List<HistoricalPrice>();

        if (!json.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var item in values.EnumerateArray())
        {
            if (!DateTime.TryParse(item.GetProperty("datetime").ToString(), out var date)) continue;

            list.Add(new HistoricalPrice
            {
                Date = date,
                Open = decimal.Parse(item.GetProperty("open").ToString()),
                High = decimal.Parse(item.GetProperty("high").ToString()),
                Low = decimal.Parse(item.GetProperty("low").ToString()),
                Close = decimal.Parse(item.GetProperty("close").ToString()),
                Volume = item.TryGetProperty("volume", out var v) && long.TryParse(v.ToString(), out var vol) ? vol : 0
            });
        }

        return list;
    }
}
