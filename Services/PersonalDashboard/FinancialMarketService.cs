using api.Configuration;
using api.Dtos.PersonalDashboard;
using api.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace api.Services.PersonalDashboard
{
    public sealed class FinancialMarketService : IFinancialMarketService
    {
        private readonly IFinancialMarketProvider _provider;
        private readonly IMemoryCache _cache;
        private readonly ExternalFeedsOptions _options;
        private readonly ILogger<FinancialMarketService> _logger;

        public FinancialMarketService(
            IFinancialMarketProvider provider,
            IMemoryCache cache,
            IOptions<ExternalFeedsOptions> options,
            ILogger<FinancialMarketService> logger)
        {
            _provider = provider;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<FinancialMarketFeedDto> GetMarketFeedAsync(CancellationToken cancellationToken)
        {
            const string cacheKey = "personal-dashboard:financial-markets";
            if (_cache.TryGetValue(cacheKey, out FinancialMarketFeedDto? cached) && cached is not null)
            {
                return cached;
            }

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));

                var quotes = await _provider.GetQuotesAsync(_options.FinancialMarkets.Symbols, timeout.Token);
                var feed = new FinancialMarketFeedDto(
                    _options.FinancialMarkets.Provider,
                    IsDemoProvider(_options.FinancialMarkets.Provider),
                    false,
                    DateTimeOffset.UtcNow,
                    quotes
                        .Select(NormalizeQuote)
                        .ToList());

                _cache.Set(cacheKey, feed, TimeSpan.FromMinutes(Math.Max(1, _options.FinancialMarkets.CacheDurationMinutes)));
                _cache.Set($"{cacheKey}:last-good", feed, TimeSpan.FromHours(2));
                return feed;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Financial market provider failed.");
                if (_cache.TryGetValue($"{cacheKey}:last-good", out FinancialMarketFeedDto? lastGood) && lastGood is not null)
                {
                    return lastGood with { IsStale = true };
                }

                return new FinancialMarketFeedDto(
                    _options.FinancialMarkets.Provider,
                    IsDemoProvider(_options.FinancialMarkets.Provider),
                    true,
                    DateTimeOffset.UtcNow,
                    Array.Empty<FinancialQuoteDto>());
            }
        }

        private static FinancialQuoteDto NormalizeQuote(FinancialQuoteDto quote) =>
            quote with
            {
                ExternalUrl = ExternalUrlGuard.SafeHttpUrl(quote.ExternalUrl)
            };

        private static bool IsDemoProvider(string provider) =>
            string.Equals(provider, "Demo", StringComparison.OrdinalIgnoreCase);
    }
}
