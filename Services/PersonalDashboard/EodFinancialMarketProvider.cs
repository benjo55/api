using api.Dtos.PersonalDashboard;
using api.Interfaces;

namespace api.Services.PersonalDashboard
{
    public sealed class EodFinancialMarketProvider : IFinancialMarketProvider
    {
        private sealed record Instrument(
            string Symbol,
            string Name,
            string EodTicker,
            string? Currency,
            string ExternalUrl);

        private static readonly IReadOnlyDictionary<string, Instrument> Catalog =
            new Dictionary<string, Instrument>(StringComparer.OrdinalIgnoreCase)
            {
                ["CAC40"] = new("CAC40", "CAC 40", "FCHI.INDX", "pts", "https://eodhd.com/financial-summary/FCHI.INDX"),
                ["EUROSTOXX50"] = new("EUROSTOXX50", "Euro Stoxx 50", "STOXX50E.INDX", "pts", "https://eodhd.com/financial-summary/STOXX50E.INDX"),
                ["DAX"] = new("DAX", "DAX", "GDAXI.INDX", "pts", "https://eodhd.com/financial-summary/GDAXI.INDX"),
                ["FTSE100"] = new("FTSE100", "FTSE 100", "FTSE.INDX", "pts", "https://eodhd.com/financial-summary/FTSE.INDX"),
                ["SP500"] = new("SP500", "S&P 500", "GSPC.INDX", "pts", "https://eodhd.com/financial-summary/GSPC.INDX"),
                ["NASDAQ"] = new("NASDAQ", "Nasdaq Composite", "IXIC.INDX", "pts", "https://eodhd.com/financial-summary/IXIC.INDX"),
                ["DOWJONES"] = new("DOWJONES", "Dow Jones", "DJI.INDX", "pts", "https://eodhd.com/financial-summary/DJI.INDX"),
                ["NIKKEI225"] = new("NIKKEI225", "Nikkei 225", "N225.INDX", "JPY", "https://eodhd.com/financial-summary/N225.INDX"),
                ["EURUSD"] = new("EURUSD", "EUR/USD", "EURUSD.FOREX", null, "https://eodhd.com/financial-summary/EURUSD.FOREX"),
                ["GOLD"] = new("GOLD", "Or", "XAUUSD.FOREX", "USD", "https://eodhd.com/financial-summary/XAUUSD.FOREX"),
                ["BRENT"] = new("BRENT", "Pétrole Brent", "BRENTOIL.COMM", "USD", "https://eodhd.com/financial-summary/BRENTOIL.COMM"),
                ["BTC"] = new("BTC", "Bitcoin", "BTC-USD.CC", "USD", "https://eodhd.com/financial-summary/BTC-USD.CC"),
                ["ETH"] = new("ETH", "Ethereum", "ETH-USD.CC", "USD", "https://eodhd.com/financial-summary/ETH-USD.CC")
            };

        private readonly IEodDataProvider _eodDataProvider;
        private readonly ILogger<EodFinancialMarketProvider> _logger;

        public EodFinancialMarketProvider(
            IEodDataProvider eodDataProvider,
            ILogger<EodFinancialMarketProvider> logger)
        {
            _eodDataProvider = eodDataProvider;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<FinancialQuoteDto>> GetQuotesAsync(
            IEnumerable<string> symbols,
            CancellationToken cancellationToken)
        {
            var result = new List<FinancialQuoteDto>();
            foreach (var symbol in symbols.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(await GetQuoteAsync(symbol.Trim(), cancellationToken));
            }

            return result;
        }

        private async Task<FinancialQuoteDto> GetQuoteAsync(string requestedSymbol, CancellationToken cancellationToken)
        {
            if (!Catalog.TryGetValue(requestedSymbol, out var instrument))
            {
                return Unavailable(requestedSymbol, requestedSymbol, "Instrument non configuré pour EOD.");
            }

            try
            {
                var to = DateTime.UtcNow.Date;
                var from = to.AddDays(-12);
                var prices = await _eodDataProvider.GetHistoricalDataAsync(instrument.EodTicker, from, to);
                cancellationToken.ThrowIfCancellationRequested();

                var ordered = prices
                    .Where(price => price.Date > DateTime.MinValue && price.Close > 0)
                    .OrderBy(price => price.Date)
                    .ToList();

                if (ordered.Count == 0)
                {
                    return Unavailable(instrument.Symbol, instrument.Name, $"Aucun cours EOD disponible pour {instrument.EodTicker}.");
                }

                var latest = ordered[^1];
                var previous = ordered.Count >= 2 ? ordered[^2] : null;
                decimal? change = previous is null ? null : latest.Close - previous.Close;
                decimal? changePercent = previous is null || previous.Close == 0
                    ? null
                    : change / previous.Close * 100m;

                return new FinancialQuoteDto(
                    instrument.Symbol,
                    instrument.Name,
                    latest.Close,
                    change,
                    changePercent,
                    instrument.Currency,
                    DateTime.SpecifyKind(latest.Date, DateTimeKind.Utc),
                    true,
                    ExternalUrlGuard.SafeHttpUrl(instrument.ExternalUrl),
                    null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "EOD quote failed for Symbol={Symbol}", requestedSymbol);
                return Unavailable(instrument.Symbol, instrument.Name, $"Cours EOD indisponible pour {instrument.EodTicker}.");
            }
        }

        private static FinancialQuoteDto Unavailable(string symbol, string name, string error) =>
            new(
                symbol.Trim().ToUpperInvariant(),
                name,
                null,
                null,
                null,
                null,
                null,
                true,
                null,
                error);
    }
}
