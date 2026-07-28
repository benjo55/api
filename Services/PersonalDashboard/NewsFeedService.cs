using api.Configuration;
using api.Dtos.PersonalDashboard;
using api.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace api.Services.PersonalDashboard
{
    public sealed class NewsFeedService : INewsFeedService
    {
        private readonly INewsProvider _provider;
        private readonly IMemoryCache _cache;
        private readonly ExternalFeedsOptions _options;
        private readonly ILogger<NewsFeedService> _logger;

        public NewsFeedService(
            INewsProvider provider,
            IMemoryCache cache,
            IOptions<ExternalFeedsOptions> options,
            ILogger<NewsFeedService> logger)
        {
            _provider = provider;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<NewsFeedDto> GetNewsFeedAsync(
            NewsCategory category,
            int limit,
            CancellationToken cancellationToken)
        {
            var safeLimit = Math.Clamp(
                limit <= 0 ? _options.News.DefaultLimit : limit,
                1,
                Math.Max(1, _options.News.MaxLimit));
            var cacheKey = $"personal-dashboard:news:{category}:{safeLimit}";

            if (_cache.TryGetValue(cacheKey, out NewsFeedDto? cached) && cached is not null)
            {
                return cached;
            }

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));

                var articles = await _provider.GetNewsAsync(category, safeLimit, timeout.Token);
                var feed = new NewsFeedDto(
                    category,
                    _options.News.Provider,
                    IsDemoProvider(_options.News.Provider),
                    false,
                    DateTimeOffset.UtcNow,
                    articles
                        .Where(IsSafeArticle)
                        .Take(safeLimit)
                        .ToList());

                _cache.Set(cacheKey, feed, TimeSpan.FromMinutes(Math.Max(1, _options.News.CacheDurationMinutes)));
                _cache.Set($"{cacheKey}:last-good", feed, TimeSpan.FromHours(2));
                return feed;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "News provider failed for Category={Category}", category);
                if (_cache.TryGetValue($"{cacheKey}:last-good", out NewsFeedDto? lastGood) && lastGood is not null)
                {
                    return lastGood with { IsStale = true };
                }

                return new NewsFeedDto(
                    category,
                    _options.News.Provider,
                    IsDemoProvider(_options.News.Provider),
                    true,
                    DateTimeOffset.UtcNow,
                    Array.Empty<NewsArticleDto>());
            }
        }

        private static bool IsSafeArticle(NewsArticleDto article) =>
            ExternalUrlGuard.SafeHttpUrl(article.ArticleUrl) is not null
            && (article.ImageUrl is null || ExternalUrlGuard.SafeHttpUrl(article.ImageUrl) is not null);

        private static bool IsDemoProvider(string provider) =>
            string.Equals(provider, "Demo", StringComparison.OrdinalIgnoreCase);
    }
}
