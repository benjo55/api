using api.Configuration;
using api.Dtos.PersonalDashboard;
using api.Interfaces;
using api.Services.PersonalDashboard;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using Xunit;

namespace api.Tests;

public sealed class PersonalDashboardFeedTests
{
    [Fact]
    public async Task Demo_news_provider_returns_limited_safe_articles()
    {
        var provider = new DemoNewsProvider();

        var articles = await provider.GetNewsAsync(NewsCategory.ArtificialIntelligence, 3, CancellationToken.None);

        Assert.Equal(3, articles.Count);
        Assert.All(articles, article =>
        {
            Assert.Equal(NewsCategory.ArtificialIntelligence.ToString(), article.Category);
            Assert.StartsWith("https://", article.ArticleUrl);
            Assert.DoesNotContain("javascript:", article.ArticleUrl, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task News_service_applies_limit_and_cache()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new CountingNewsProvider();
        var service = new NewsFeedService(
            provider,
            cache,
            Options.Create(new ExternalFeedsOptions
            {
                News = new NewsFeedOptions
                {
                    Provider = "Demo",
                    DefaultLimit = 6,
                    MaxLimit = 2,
                    CacheDurationMinutes = 15
                }
            }),
            NullLogger<NewsFeedService>.Instance);

        var first = await service.GetNewsFeedAsync(NewsCategory.Top, 10, CancellationToken.None);
        var second = await service.GetNewsFeedAsync(NewsCategory.Top, 10, CancellationToken.None);

        Assert.Equal(2, first.Articles.Count);
        Assert.Equal(2, second.Articles.Count);
        Assert.Equal(1, provider.CallCount);
        Assert.True(first.IsDemo);
    }

    [Fact]
    public async Task News_service_returns_stale_empty_feed_when_provider_fails_without_cache()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new NewsFeedService(
            new ThrowingNewsProvider(),
            cache,
            Options.Create(new ExternalFeedsOptions()),
            NullLogger<NewsFeedService>.Instance);

        var feed = await service.GetNewsFeedAsync(NewsCategory.France, 6, CancellationToken.None);

        Assert.True(feed.IsStale);
        Assert.Empty(feed.Articles);
    }

    [Fact]
    public async Task Rss_news_provider_extracts_image_and_plain_summary()
    {
        const string rss = """
            <?xml version="1.0" encoding="UTF-8" ?>
            <rss version="2.0" xmlns:media="http://search.yahoo.com/mrss/">
              <channel>
                <item>
                  <guid>article-1</guid>
                  <title>Une actualité fiable</title>
                  <link>https://example.org/news/1</link>
                  <description><![CDATA[<p>Résumé <strong>nettoyé</strong> avec une image.</p>]]></description>
                  <pubDate>Mon, 27 Jul 2026 08:00:00 GMT</pubDate>
                  <media:thumbnail url="https://example.org/image.jpg" />
                </item>
              </channel>
            </rss>
            """;
        var client = new HttpClient(new StubHttpMessageHandler(rss));
        var provider = new RssNewsProvider(
            new StubHttpClientFactory(client),
            Options.Create(new ExternalFeedsOptions
            {
                News = new NewsFeedOptions
                {
                    Provider = "Rss",
                    Feeds =
                    [
                        new NewsFeedDefinition
                        {
                            Key = "test",
                            Name = "Test Source",
                            Category = "Top",
                            Url = "https://example.org/rss.xml",
                            Priority = 100,
                            IsEnabled = true
                        }
                    ]
                }
            }),
            NullLogger<RssNewsProvider>.Instance);

        var articles = await provider.GetNewsAsync(NewsCategory.Top, 6, CancellationToken.None);

        var article = Assert.Single(articles);
        Assert.Equal("Une actualité fiable", article.Title);
        Assert.Equal("Résumé nettoyé avec une image.", article.Summary);
        Assert.Equal("https://example.org/image.jpg", article.ImageUrl);
        Assert.StartsWith("https://example.org/news/1", article.ArticleUrl);
    }

    [Fact]
    public async Task Rss_news_provider_extracts_i24_news_page_articles()
    {
        const string page = """
            <html><body>
            <script>window.__PRELOADED_STATE__ = {"newsFeed":{"isFetching":false,"limit":24,"newsPages":{"1":[{"id":1334091,"title":"Short live title","content":{"excerpt":"A detailed <strong>article</strong> summary","id":745791,"image":{"src":"https:\u002F\u002Fcdn.i24news.tv\u002Fuploads\u002Fimage.jpg","alt":"News image"},"frontendUrl":"https:\u002F\u002Fwww.i24news.tv\u002Fen\u002Fnews\u002Fmiddle-east\u002Farticle-1","title":"Full i24 article title","publishedAt":"2026-07-26T14:18:00+00:00"},"startedAt":new Date("2026-07-26T14:56:03.000Z"),"status":"normal"},{"id":1334031,"title":"Live item without article","content":null,"startedAt":new Date("2026-07-26T13:59:12.000Z"),"status":"normal"}]}}};</script>
            </body></html>
            """;
        var client = new HttpClient(new StubHttpMessageHandler(page));
        var provider = new RssNewsProvider(
            new StubHttpClientFactory(client),
            Options.Create(new ExternalFeedsOptions
            {
                News = new NewsFeedOptions
                {
                    Provider = "Rss",
                    Feeds =
                    [
                        new NewsFeedDefinition
                        {
                            Key = "i24-news",
                            Name = "i24NEWS",
                            Category = "World",
                            Url = "https://www.i24news.tv/en/news",
                            Priority = 90,
                            IsEnabled = true
                        }
                    ]
                }
            }),
            NullLogger<RssNewsProvider>.Instance);

        var articles = await provider.GetNewsAsync(NewsCategory.World, 6, CancellationToken.None);

        var article = Assert.Single(articles);
        Assert.Equal("Full i24 article title", article.Title);
        Assert.Equal("A detailed article summary", article.Summary);
        Assert.Equal("i24NEWS", article.SourceName);
        Assert.Equal("https://cdn.i24news.tv/uploads/image.jpg", article.ImageUrl);
        Assert.Equal("https://www.i24news.tv/en/news/middle-east/article-1", article.ArticleUrl);
        Assert.Equal(NewsCategory.World.ToString(), article.Category);
    }

    [Fact]
    public async Task Financial_market_service_keeps_partial_quote_errors()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new FinancialMarketService(
            new DemoFinancialMarketProvider(),
            cache,
            Options.Create(new ExternalFeedsOptions
            {
                FinancialMarkets = new FinancialMarketsOptions
                {
                    Provider = "Demo",
                    Symbols = ["CAC40", "UNKNOWN"],
                    CacheDurationMinutes = 5
                }
            }),
            NullLogger<FinancialMarketService>.Instance);

        var feed = await service.GetMarketFeedAsync(CancellationToken.None);

        Assert.Equal(2, feed.Quotes.Count);
        Assert.Contains(feed.Quotes, quote => quote.Symbol == "CAC40" && quote.LastValue.HasValue);
        Assert.Contains(feed.Quotes, quote => quote.Symbol == "UNKNOWN" && quote.Error is not null);
        Assert.DoesNotContain(feed.Quotes, quote => quote.ExternalUrl?.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task Eod_financial_market_provider_computes_latest_variation()
    {
        var provider = new EodFinancialMarketProvider(
            new StubEodDataProvider([
                new HistoricalPrice { Date = new DateTime(2026, 07, 24), Close = 100m },
                new HistoricalPrice { Date = new DateTime(2026, 07, 27), Close = 105m }
            ]),
            NullLogger<EodFinancialMarketProvider>.Instance);

        var quotes = await provider.GetQuotesAsync(["CAC40"], CancellationToken.None);

        var quote = Assert.Single(quotes);
        Assert.Equal("CAC40", quote.Symbol);
        Assert.Equal(105m, quote.LastValue);
        Assert.Equal(5m, quote.Change);
        Assert.Equal(5m, quote.ChangePercent);
        Assert.True(quote.IsDelayed);
        Assert.Null(quote.Error);
    }

    private sealed class CountingNewsProvider : INewsProvider
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyCollection<NewsArticleDto>> GetNewsAsync(
            NewsCategory category,
            int limit,
            CancellationToken cancellationToken)
        {
            CallCount++;
            IReadOnlyCollection<NewsArticleDto> articles = Enumerable.Range(1, limit)
                .Select(index => new NewsArticleDto(
                    index.ToString(),
                    $"Article {index}",
                    "Résumé",
                    "Source",
                    $"https://example.org/{index}",
                    null,
                    category.ToString(),
                    DateTimeOffset.UtcNow,
                    true))
                .ToList();

            return Task.FromResult(articles);
        }
    }

    private sealed class ThrowingNewsProvider : INewsProvider
    {
        public Task<IReadOnlyCollection<NewsArticleDto>> GetNewsAsync(
            NewsCategory category,
            int limit,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Provider unavailable");
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StubHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _content;

        public StubHttpMessageHandler(string content)
        {
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_content)
            });
        }
    }

    private sealed class StubEodDataProvider : IEodDataProvider
    {
        private readonly List<HistoricalPrice> _prices;

        public StubEodDataProvider(List<HistoricalPrice> prices)
        {
            _prices = prices;
        }

        public Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker, DateTime from, DateTime to) =>
            Task.FromResult(_prices);

        public Task<string?> FindTickerByIsinAsync(string isin) =>
            Task.FromResult<string?>(null);

        public Task<api.Dtos.Eod.EodFundProfile?> GetFundProfileAsync(string ticker) =>
            Task.FromResult<api.Dtos.Eod.EodFundProfile?>(null);

        public Task<(decimal Value, DateTime Date)?> GetLatestPriceAsync(string isin) =>
            Task.FromResult<(decimal Value, DateTime Date)?>(null);
    }
}
