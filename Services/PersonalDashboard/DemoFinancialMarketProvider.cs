using api.Dtos.PersonalDashboard;
using api.Interfaces;

namespace api.Services.PersonalDashboard
{
    public sealed class DemoFinancialMarketProvider : IFinancialMarketProvider
    {
        private static readonly IReadOnlyDictionary<string, (string Name, decimal Value, decimal Change, decimal Percent, string? Currency, string Url)> Catalog =
            new Dictionary<string, (string, decimal, decimal, decimal, string?, string)>(StringComparer.OrdinalIgnoreCase)
            {
                ["CAC40"] = ("CAC 40", 8145.32m, 60.12m, 0.74m, "pts", "https://www.euronext.com/fr/products/indices/FR0003500008-XPAR"),
                ["EUROSTOXX50"] = ("Euro Stoxx 50", 5210.14m, -6.25m, -0.12m, "pts", "https://www.stoxx.com/index-details?symbol=SX5E"),
                ["DAX"] = ("DAX", 24218.91m, 33.14m, 0.14m, "pts", "https://www.deutsche-boerse.com/dbg-en/"),
                ["FTSE100"] = ("FTSE 100", 9315.44m, -18.88m, -0.20m, "pts", "https://www.londonstockexchange.com/indices/ftse-100"),
                ["SP500"] = ("S&P 500", 6384.20m, 19.74m, 0.31m, "pts", "https://www.spglobal.com/spdji/en/indices/equity/sp-500/"),
                ["NASDAQ"] = ("Nasdaq Composite", 20895.66m, 99.64m, 0.48m, "pts", "https://www.nasdaq.com/market-activity/index/comp"),
                ["DOWJONES"] = ("Dow Jones", 42350.10m, 0m, 0m, "pts", "https://www.spglobal.com/spdji/en/indices/equity/dow-jones-industrial-average/"),
                ["NIKKEI225"] = ("Nikkei 225", 39812.04m, -115.27m, -0.29m, "JPY", "https://indexes.nikkei.co.jp/en/nkave"),
                ["EURUSD"] = ("EUR/USD", 1.0842m, -0.0009m, -0.08m, null, "https://www.ecb.europa.eu/stats/policy_and_exchange_rates/euro_reference_exchange_rates/html/index.en.html"),
                ["GOLD"] = ("Or", 2374.50m, 12.30m, 0.52m, "USD", "https://www.lbma.org.uk/prices-and-data/precious-metal-prices"),
                ["BRENT"] = ("Pétrole Brent", 84.72m, -0.41m, -0.48m, "USD", "https://www.ice.com/products/219/Brent-Crude-Futures"),
                ["BTC"] = ("Bitcoin", 101250m, 1250m, 1.25m, "EUR", "https://www.coindesk.com/price/bitcoin"),
                ["ETH"] = ("Ethereum", 3560m, 18m, 0.51m, "EUR", "https://www.coindesk.com/price/ethereum")
            };

        public Task<IReadOnlyCollection<FinancialQuoteDto>> GetQuotesAsync(
            IEnumerable<string> symbols,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var quotes = symbols
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Select(symbol =>
                {
                    if (!Catalog.TryGetValue(symbol.Trim(), out var item))
                    {
                        return new FinancialQuoteDto(
                            symbol.Trim(),
                            symbol.Trim(),
                            null,
                            null,
                            null,
                            null,
                            null,
                            true,
                            null,
                            "Instrument indisponible chez le fournisseur de démonstration.");
                    }

                    return new FinancialQuoteDto(
                        symbol.Trim().ToUpperInvariant(),
                        item.Name,
                        item.Value,
                        item.Change,
                        item.Percent,
                        item.Currency,
                        now.AddMinutes(-5),
                        true,
                        ExternalUrlGuard.SafeHttpUrl(item.Url),
                        null);
                })
                .ToList();

            return Task.FromResult<IReadOnlyCollection<FinancialQuoteDto>>(quotes);
        }
    }
}
