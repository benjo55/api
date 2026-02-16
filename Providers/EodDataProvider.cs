using api.Dtos.Eod;
using System.Text.Json;
using api.Helpers;
using System.Text.Json.Serialization;
using System.Net;

// Extension methods for JsonElement
public static class JsonElementExtensions
{
    public static string? GetPropertyOrNull(this JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
            return prop.ToString();
        return null;
    }

    public static DateTime? ParseDateOrNull(this JsonElement element, string propertyName)
    {
        var str = element.GetPropertyOrNull(propertyName);
        if (DateTime.TryParse(str, out var dt))
            return dt;
        return null;
    }
}
public class EodDataProvider : IEodDataProvider
{
    private class EodHistoricalPrice
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("open")]
        public decimal Open { get; set; }

        [JsonPropertyName("high")]
        public decimal High { get; set; }

        [JsonPropertyName("low")]
        public decimal Low { get; set; }

        [JsonPropertyName("close")]
        public decimal Close { get; set; }

        [JsonPropertyName("volume")]
        public long Volume { get; set; }
    }

    private class EodSearchResult
    {
        public string Code { get; set; } = "";
        public string Exchange { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Currency { get; set; } = "";
    }
    private readonly string? _apiKey;
    private readonly HttpClient _httpClient;

    public EodDataProvider(IConfiguration config, HttpClient httpClient)
    {
        _apiKey = config["Eod:ApiKey"];
        _httpClient = httpClient;
    }

    public async Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker, DateTime from, DateTime to)
    {
        var url = $"https://eodhistoricaldata.com/api/eod/{ticker}" +
                  $"?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&api_token={_apiKey}&fmt=json";

        Console.WriteLine($"[EOD] 📡 URL historique : {url}");

        try
        {
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.PaymentRequired)
            {
                Console.WriteLine("[EOD] 🚫 Quota EOD dépassé (402 Payment Required). Aucun historique récupéré.");
                return new();
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[EOD] 📄 JSON brut : {json[..Math.Min(300, json.Length)]}...");

            var parsed = JsonSerializer.Deserialize<List<EodHistoricalPrice>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null || parsed.Count == 0)
            {
                Console.WriteLine("[EOD] ❌ Aucune donnée historique.");
                return new();
            }

            var valid = parsed
                .Where(p => p.Date > DateTime.MinValue)
                .Select(p => new HistoricalPrice
                {
                    Date = p.Date,
                    Open = p.Open,
                    High = p.High,
                    Low = p.Low,
                    Close = p.Close,
                    Volume = p.Volume
                })
                .ToList();

            Console.WriteLine($"[EOD] ✅ {valid.Count} points valides.");
            return valid;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[EOD] ❌ Erreur HTTP : {ex.StatusCode} - {ex.Message}");
            return new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EOD] ❌ Erreur inattendue : {ex.Message}");
            return new();
        }
    }

    public async Task<string?> FindTickerByIsinAsync(string isin)
    {
        var url = $"https://eodhistoricaldata.com/api/search/{isin}?api_token={_apiKey}&fmt=json";
        Console.WriteLine($"[EOD] 🔎 Recherche EOD par ISIN : {url}");

        var results = await _httpClient.GetFromJsonAsync<List<EodSearchResult>>(url);

        if (results == null || results.Count == 0)
        {
            Console.WriteLine($"[EOD] ❌ Aucun résultat pour l'ISIN : {isin}");
            return null;
        }

        Console.WriteLine($"[EOD] ✅ Résultats obtenus pour {isin} :");
        foreach (var r in results)
            Console.WriteLine($"→ Code: {r.Code}, Type: {r.Type}, Exchange: {r.Exchange}, Name: {r.Name}");

        // 1. Priorité : tickers ETF/FUND avec exchange connu
        var preferredExchanges = new[] { "PA", "XETRA", "F", "SW", "EUFUND" };
        var filtered = results
            .Where(r => (r.Type == "ETF" || r.Type == "FUND") && !string.IsNullOrWhiteSpace(r.Code))
            .OrderByDescending(r => preferredExchanges.Contains(r.Exchange ?? "")).ToList();

        foreach (var result in filtered)
        {
            var ticker = result.Exchange != null && !result.Code.Contains(".")
                ? $"{result.Code}.{result.Exchange}"
                : result.Code;

            Console.WriteLine($"[EOD] 🔄 Test de {ticker}...");

            if (await IsTickerSupportedAsync(ticker))
            {
                Console.WriteLine($"[EOD] ✅ Ticker valide : {ticker}");
                return ticker;
            }
            else
            {
                Console.WriteLine($"[EOD] ❌ Ticker rejeté (HEAD NOK) : {ticker}");
            }
        }

        // 2. Fallback : premier code ETF/FUND (même si non HEAD OK)
        var fallback = filtered.FirstOrDefault()?.Code;
        if (fallback != null)
            Console.WriteLine($"[EOD] ⚠️ Aucun ticker HEAD OK. Fallback vers : {fallback}");

        return fallback;
    }

    private async Task<bool> IsTickerSupportedAsync(string ticker)
    {
        var url = $"https://eodhistoricaldata.com/api/eod/{ticker}?api_token={_apiKey}&from=2024-01-01&to=2024-01-02";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await _httpClient.SendAsync(request);
            Console.WriteLine($"[EOD] 🔁 HEAD {ticker} → {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EOD] ⚠️ Erreur HEAD sur {ticker} : {ex.Message}");
            return false;
        }
    }

    public async Task<EodFundProfile?> GetFundProfileAsync(string ticker)
    {
        var fundamentalsUrl =
            $"https://eodhistoricaldata.com/api/fundamentals/{Uri.EscapeDataString(ticker)}?api_token={_apiKey}&fmt=json";

        Console.WriteLine($"[EOD] 📊 Récupération du profil EOD pour : {ticker}");

        try
        {
            // --- 1️⃣ Appel principal /fundamentals ---
            var response = await _httpClient.GetAsync(fundamentalsUrl);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                // --- 2️⃣ Fallback automatique vers /eod ---
                Console.WriteLine($"[EOD] ⚠️ /fundamentals interdit pour {ticker}. Fallback vers /eod/…");

                var eodUrl =
                    $"https://eodhistoricaldata.com/api/eod/{Uri.EscapeDataString(ticker)}?api_token={_apiKey}&fmt=json";

                var fbResponse = await _httpClient.GetAsync(eodUrl);
                if (!fbResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[EOD] ❌ Échec fallback /eod pour {ticker} → {fbResponse.StatusCode}");
                    return null;
                }

                var fbJson = await fbResponse.Content.ReadAsStringAsync();

                // 🔥 ICI : réutilisation de TA classe interne EodHistoricalPrice
                var fbData = JsonSerializer.Deserialize<List<EodHistoricalPrice>>(fbJson);

                if (fbData?.Any() == true)
                {
                    var last = fbData.First();

                    return new EodFundProfile
                    {
                        Code = ticker,
                        LastValuationAmount = last.Close,
                        LastValuationDate = last.Date
                    };
                }

                return null;
            }

            // --- 3️⃣ Appel /fundamentals OK ---
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (json.ValueKind != JsonValueKind.Object || !json.TryGetProperty("General", out var general))
                return null;

            json.TryGetProperty("Fees", out var fees);
            json.TryGetProperty("Policy", out var policy);
            json.TryGetProperty("ESG", out var esg);
            json.TryGetProperty("PEA", out var pea);
            json.TryGetProperty("Risk", out var risk);

            // --- 4️⃣ Construction du profil complet ---
            return new EodFundProfile
            {
                Code = general.GetPropertyOrNull("Code"),
                ISIN = general.GetPropertyOrNull("ISIN"),
                Currency = general.GetPropertyOrNull("CurrencyCode"),
                SupportType = general.GetPropertyOrNull("Type"),
                MarketingName = general.GetPropertyOrNull("ShortName"),
                LegalName = general.GetPropertyOrNull("Name"),
                BloombergCode = general.GetPropertyOrNull("Code"),
                MorningstarCode = general.GetPropertyOrNull("Code"),
                CUSIP = general.GetPropertyOrNull("CUSIP"),
                SEDOL = general.GetPropertyOrNull("SEDOL"),
                AssetManager = general.GetPropertyOrNull("Company"),
                FundDomicile = general.GetPropertyOrNull("Country"),
                InceptionDate = general.ParseDateOrNull("InceptionDate"),
                ClosureDate = general.ParseDateOrNull("ClosureDate"),

                AMFCode = general.GetPropertyOrNull("AMFCode"),
                DepositaryBank = general.GetPropertyOrNull("DepositaryBank"),
                Custodian = general.GetPropertyOrNull("CustodianBank"),

                AssetClass = general.GetPropertyOrNull("AssetClass"),
                SubAssetClass = general.GetPropertyOrNull("SubAssetClass"),
                GeographicFocus = general.GetPropertyOrNull("GeographicFocus"),
                SectorFocus = general.GetPropertyOrNull("SectorFocus"),

                CapitalizationPolicy = policy.GetPropertyOrNull("CapitalizationPolicy"),
                InvestmentStrategy = general.GetPropertyOrNull("InvestmentStrategy"),
                LegalForm = general.GetPropertyOrNull("LegalForm"),
                ManagementStyle = general.GetPropertyOrNull("ManagementStyle"),
                UCITSCategory = general.GetPropertyOrNull("UCITSCategory"),

                MinimumSubscription = fees.GetDecimalOrNull("MinimumSubscription"),
                MinimumHolding = fees.GetDecimalOrNull("MinimumHolding"),
                ManagementFee = fees.GetDecimalOrNull("ManagementFee"),
                PerformanceFee = fees.GetDecimalOrNull("PerformanceFee"),
                TurnoverRate = fees.GetDecimalOrNull("TurnoverRate"),
                AUM = general.GetDecimalOrNull("TotalAssets"),

                IsCapitalGuaranteed = policy.GetBoolOrNull("CapitalGuaranteed"),
                IsCurrencyHedged = policy.GetBoolOrNull("CurrencyHedged"),
                Benchmark = general.GetPropertyOrNull("Benchmark"),

                HasESGLabel = esg.GetBoolOrNull("HasESGLabel"),
                ESGLabel = esg.GetPropertyOrNull("Label"),
                SFDRClassification = esg.GetPropertyOrNull("SFDR"),
                ESGScore = esg.GetDecimalOrNull("Score"),
                ESGScoreProvider = esg.GetPropertyOrNull("Provider"),

                MifidTargetMarket = risk.GetPropertyOrNull("TargetMarket"),
                MifidRiskTolerance = risk.GetPropertyOrNull("RiskTolerance"),
                MifidClientType = risk.GetPropertyOrNull("ClientType"),

                LastValuationAmount = general.GetDecimalOrNull("LastNAV"),
                LastValuationDate = general.ParseDateOrNull("LastNAVDate"),
                WeeklyVolatility = risk.GetDecimalOrNull("WeeklyVolatility"),
                MaxDrawdown1Y = risk.GetDecimalOrNull("MaxDrawdown1Y"),

                Distributor = general.GetPropertyOrNull("Distributor"),
                IsAvailableOnline = general.GetBoolOrNull("IsAvailableOnline"),
                IsAdvisedSale = general.GetBoolOrNull("IsAdvisedSale"),
                IsEligiblePEA = pea.GetBoolOrNull("Eligible"),

                CountryOfDistribution = general.GetPropertyOrNull("DistributionCountry"),
                PrimaryListingMarket = general.GetPropertyOrNull("PrimaryListing"),
                IsFundOfFunds = general.GetBoolOrNull("IsFundOfFunds")
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EOD] ❌ Erreur GetFundProfileAsync : {ex.Message}");
            return null;
        }
    }

    public async Task<(decimal Value, DateTime Date)?> GetLatestPriceAsync(string isin)
    {
        try
        {
            // 1️⃣ Trouve le ticker
            var ticker = await FindTickerByIsinAsync(isin);
            if (string.IsNullOrWhiteSpace(ticker))
            {
                Console.WriteLine($"[EOD] ❌ Aucun ticker trouvé pour ISIN {isin}");
                return null;
            }

            // 2️⃣ Récupère le profil EOD (contenant la dernière VL)
            var profile = await GetFundProfileAsync(ticker);
            if (profile == null)
            {
                Console.WriteLine($"[EOD] ⚠️ Aucun profil pour {ticker}");
                return null;
            }

            if (profile.LastValuationAmount == null || profile.LastValuationAmount <= 0)
            {
                Console.WriteLine($"[EOD] ⚠️ Pas de VL valide pour {ticker}");
                return null;
            }

            return (profile.LastValuationAmount.Value, profile.LastValuationDate ?? DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EOD] ❌ Erreur GetLatestPriceAsync : {ex.Message}");
            return null;
        }
    }
}