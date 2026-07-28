using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using api.Configuration;
using api.Dtos.PersonalDashboard;
using api.Interfaces;
using Microsoft.Extensions.Options;

namespace api.Services.PersonalDashboard
{
    public sealed class RssNewsProvider : INewsProvider
    {
        private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex ImageSourceRegex = new("<img[^>]+src=[\"'](?<src>[^\"']+)[\"']", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ExternalFeedsOptions _options;
        private readonly ILogger<RssNewsProvider> _logger;

        public RssNewsProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalFeedsOptions> options,
            ILogger<RssNewsProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<NewsArticleDto>> GetNewsAsync(
            NewsCategory category,
            int limit,
            CancellationToken cancellationToken)
        {
            var feeds = _options.News.Feeds
                .Where(feed => feed.IsEnabled)
                .Where(feed => MatchesCategory(feed.Category, category))
                .OrderByDescending(feed => feed.Priority)
                .ToList();

            if (feeds.Count == 0)
            {
                feeds = _options.News.Feeds
                    .Where(feed => feed.IsEnabled)
                    .Where(feed => MatchesCategory(feed.Category, NewsCategory.Top))
                    .OrderByDescending(feed => feed.Priority)
                    .ToList();
            }

            if (feeds.Count == 0)
            {
                return Array.Empty<NewsArticleDto>();
            }

            var articles = new List<NewsArticleDto>();
            foreach (var feed in feeds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    articles.AddRange(await ReadFeedAsync(feed, category, Math.Max(limit, 12), cancellationToken));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "RSS feed failed: {FeedKey} ({FeedUrl})", feed.Key, feed.Url);
                }
            }

            return articles
                .GroupBy(article => NormalizeDedupeKey(article.ArticleUrl, article.Title), StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(article => !string.IsNullOrWhiteSpace(article.ImageUrl))
                    .ThenByDescending(article => article.PublishedAt)
                    .First())
                .OrderByDescending(article => article.PublishedAt)
                .Take(limit)
                .ToList();
        }

        private async Task<IReadOnlyCollection<NewsArticleDto>> ReadFeedAsync(
            NewsFeedDefinition feed,
            NewsCategory category,
            int limit,
            CancellationToken cancellationToken)
        {
            var safeFeedUrl = ExternalUrlGuard.SafeHttpUrl(feed.Url);
            if (safeFeedUrl is null)
            {
                return Array.Empty<NewsArticleDto>();
            }

            var client = _httpClientFactory.CreateClient("personal-dashboard-news");
            using var response = await client.GetAsync(safeFeedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (IsI24NewsFeed(feed, safeFeedUrl))
            {
                return ReadI24NewsPage(content, feed, category, limit);
            }

            var document = XDocument.Parse(content, LoadOptions.None);
            var nodes = document.Descendants()
                .Where(element => element.Name.LocalName is "item" or "entry")
                .Take(limit)
                .ToList();

            var now = DateTimeOffset.UtcNow;
            return nodes
                .Select(node => ToArticle(node, feed, category, now))
                .Where(article => article is not null)
                .Cast<NewsArticleDto>()
                .ToList();
        }

        private static IReadOnlyCollection<NewsArticleDto> ReadI24NewsPage(
            string html,
            NewsFeedDefinition feed,
            NewsCategory category,
            int limit)
        {
            var state = ExtractBalancedObjectAfter(html, "window.__PRELOADED_STATE__");
            if (state is null)
            {
                return Array.Empty<NewsArticleDto>();
            }

            var newsPages = ExtractBalancedArrayAfter(state, "\"newsPages\":{\"1\":");
            if (newsPages is null)
            {
                return Array.Empty<NewsArticleDto>();
            }

            var now = DateTimeOffset.UtcNow;
            return ExtractTopLevelObjects(newsPages)
                .Select(article => ToI24Article(article, feed, category, now))
                .Where(article => article is not null)
                .Cast<NewsArticleDto>()
                .Take(limit)
                .ToList();
        }

        private static NewsArticleDto? ToI24Article(
            string article,
            NewsFeedDefinition feed,
            NewsCategory category,
            DateTimeOffset now)
        {
            var articleUrl = ExternalUrlGuard.SafeHttpUrl(GetJsStringValue(article, "frontendUrl"));
            if (articleUrl is null)
            {
                return null;
            }

            var title = CleanText(GetJsStringValues(article, "title").LastOrDefault());
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            var summary = TrimToLength(CleanText(GetJsStringValue(article, "excerpt")), 220);
            var imageUrl = ExternalUrlGuard.SafeHttpUrl(GetI24ImageUrl(article));
            var publishedAt = ParseDate(GetJsStringValue(article, "publishedAt"))
                ?? ParseDate(GetJsDateValue(article, "startedAt"))
                ?? now;
            var id = GetJsNumberValue(article, "id") ?? articleUrl;

            return new NewsArticleDto(
                Hash($"{feed.Key}|{id}|{articleUrl}|{title}"),
                title,
                string.IsNullOrWhiteSpace(summary) ? null : summary,
                string.IsNullOrWhiteSpace(feed.Name) ? feed.Key : feed.Name,
                articleUrl,
                imageUrl,
                category.ToString(),
                publishedAt,
                now - publishedAt <= TimeSpan.FromHours(6));
        }

        private static bool IsI24NewsFeed(NewsFeedDefinition feed, string safeFeedUrl) =>
            feed.Key.Contains("i24", StringComparison.OrdinalIgnoreCase)
            || (Uri.TryCreate(safeFeedUrl, UriKind.Absolute, out var uri)
                && uri.Host.Contains("i24news.tv", StringComparison.OrdinalIgnoreCase));

        private static string? ExtractBalancedObjectAfter(string value, string marker)
        {
            var markerIndex = value.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return null;
            }

            var start = value.IndexOf('{', markerIndex);
            return start < 0 ? null : ExtractBalancedBlock(value, start, '{', '}');
        }

        private static string? ExtractBalancedArrayAfter(string value, string marker)
        {
            var markerIndex = value.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return null;
            }

            var start = value.IndexOf('[', markerIndex);
            return start < 0 ? null : ExtractBalancedBlock(value, start, '[', ']');
        }

        private static string? ExtractBalancedBlock(string value, int start, char open, char close)
        {
            var depth = 0;
            var inString = false;
            var escaping = false;

            for (var index = start; index < value.Length; index++)
            {
                var character = value[index];
                if (inString)
                {
                    if (escaping)
                    {
                        escaping = false;
                    }
                    else if (character == '\\')
                    {
                        escaping = true;
                    }
                    else if (character == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                }
                else if (character == open)
                {
                    depth++;
                }
                else if (character == close)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return value[start..(index + 1)];
                    }
                }
            }

            return null;
        }

        private static IEnumerable<string> ExtractTopLevelObjects(string arrayValue)
        {
            var depth = 0;
            var inString = false;
            var escaping = false;
            var start = -1;

            for (var index = 0; index < arrayValue.Length; index++)
            {
                var character = arrayValue[index];
                if (inString)
                {
                    if (escaping)
                    {
                        escaping = false;
                    }
                    else if (character == '\\')
                    {
                        escaping = true;
                    }
                    else if (character == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                }
                else if (character == '{')
                {
                    if (depth == 0)
                    {
                        start = index;
                    }

                    depth++;
                }
                else if (character == '}')
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        yield return arrayValue[start..(index + 1)];
                        start = -1;
                    }
                }
            }
        }

        private static string? GetI24ImageUrl(string article)
        {
            var match = Regex.Match(
                article,
                "\"image\"\\s*:\\s*\\{[^{}]*\"src\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"\\\\])*)\"",
                RegexOptions.Singleline);

            return match.Success ? DecodeJsonStringValue(match.Groups["value"].Value) : null;
        }

        private static string? GetJsStringValue(string value, string propertyName) =>
            GetJsStringValues(value, propertyName).FirstOrDefault();

        private static IEnumerable<string> GetJsStringValues(string value, string propertyName)
        {
            var matches = Regex.Matches(
                value,
                $"\"{Regex.Escape(propertyName)}\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"\\\\])*)\"",
                RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                yield return DecodeJsonStringValue(match.Groups["value"].Value);
            }
        }

        private static string? GetJsDateValue(string value, string propertyName)
        {
            var match = Regex.Match(
                value,
                $"\"{Regex.Escape(propertyName)}\"\\s*:\\s*new Date\\(\"(?<value>[^\"]+)\"\\)",
                RegexOptions.Singleline);

            return match.Success ? match.Groups["value"].Value : null;
        }

        private static string? GetJsNumberValue(string value, string propertyName)
        {
            var match = Regex.Match(
                value,
                $"\"{Regex.Escape(propertyName)}\"\\s*:\\s*(?<value>\\d+)",
                RegexOptions.Singleline);

            return match.Success ? match.Groups["value"].Value : null;
        }

        private static string DecodeJsonStringValue(string value)
        {
            try
            {
                return JsonSerializer.Deserialize<string>($"\"{value}\"") ?? value;
            }
            catch (JsonException)
            {
                return WebUtility.HtmlDecode(value.Replace("\\/", "/", StringComparison.Ordinal));
            }
        }

        private static NewsArticleDto? ToArticle(
            XElement node,
            NewsFeedDefinition feed,
            NewsCategory category,
            DateTimeOffset now)
        {
            var title = CleanText(GetChildValue(node, "title"));
            var articleUrl = ExternalUrlGuard.SafeHttpUrl(GetArticleUrl(node));

            if (string.IsNullOrWhiteSpace(title) || articleUrl is null)
            {
                return null;
            }

            var summary = TrimToLength(CleanText(
                GetChildValue(node, "description")
                ?? GetChildValue(node, "summary")
                ?? GetChildValue(node, "encoded")), 220);
            var publishedAt = ParseDate(
                GetChildValue(node, "pubDate")
                ?? GetChildValue(node, "published")
                ?? GetChildValue(node, "updated")) ?? now;
            var imageUrl = ExternalUrlGuard.SafeHttpUrl(GetImageUrl(node));
            var guid = CleanText(GetChildValue(node, "guid"));

            return new NewsArticleDto(
                Hash($"{feed.Key}|{guid}|{articleUrl}|{title}"),
                title,
                string.IsNullOrWhiteSpace(summary) ? null : summary,
                string.IsNullOrWhiteSpace(feed.Name) ? feed.Key : feed.Name,
                articleUrl,
                imageUrl,
                category.ToString(),
                publishedAt,
                now - publishedAt <= TimeSpan.FromHours(6));
        }

        private static string? GetArticleUrl(XElement node)
        {
            var link = node.Elements().FirstOrDefault(element => element.Name.LocalName == "link");
            var href = link?.Attribute("href")?.Value;
            return !string.IsNullOrWhiteSpace(href) ? href : link?.Value;
        }

        private static string? GetImageUrl(XElement node)
        {
            foreach (var element in node.Descendants())
            {
                var localName = element.Name.LocalName;
                if (localName is "content" or "thumbnail" or "image")
                {
                    var url = element.Attribute("url")?.Value ?? element.Attribute("href")?.Value;
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url;
                    }
                }

                if (localName == "enclosure"
                    && (element.Attribute("type")?.Value.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    var url = element.Attribute("url")?.Value;
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url;
                    }
                }
            }

            var html = GetChildValue(node, "description") ?? GetChildValue(node, "encoded");
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var match = ImageSourceRegex.Match(html);
            return match.Success ? WebUtility.HtmlDecode(match.Groups["src"].Value) : null;
        }

        private static string? GetChildValue(XElement node, string localName) =>
            node.Elements().FirstOrDefault(element => element.Name.LocalName == localName)?.Value;

        private static string CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            var withoutTags = HtmlTagRegex.Replace(value, " ");
            return WhitespaceRegex.Replace(WebUtility.HtmlDecode(withoutTags), " ").Trim();
        }

        private static string TrimToLength(string value, int maxLength)
        {
            if (value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength].TrimEnd() + "...";
        }

        private static DateTimeOffset? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static bool MatchesCategory(string configuredCategory, NewsCategory requestedCategory)
        {
            if (string.IsNullOrWhiteSpace(configuredCategory))
            {
                return false;
            }

            if (Enum.TryParse<NewsCategory>(configuredCategory, true, out var parsed))
            {
                return parsed == requestedCategory;
            }

            return requestedCategory == NewsCategory.Top
                && string.Equals(configuredCategory, "Headline", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDedupeKey(string articleUrl, string title) =>
            !string.IsNullOrWhiteSpace(articleUrl)
                ? articleUrl.Trim()
                : WhitespaceRegex.Replace(title.Trim().ToUpperInvariant(), " ");

        private static string Hash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes)[..24].ToLowerInvariant();
        }
    }
}
