namespace api.Dtos.PersonalDashboard
{
    public enum NewsCategory
    {
        Top,
        France,
        World,
        Aviation,
        Economy,
        ScienceTechnology,
        ArtificialIntelligence,
        Society,
        Health,
        Culture,
        Sport
    }

    public sealed record NewsArticleDto(
        string Id,
        string Title,
        string? Summary,
        string SourceName,
        string ArticleUrl,
        string? ImageUrl,
        string Category,
        DateTimeOffset PublishedAt,
        bool IsRecent);

    public sealed record NewsFeedDto(
        NewsCategory Category,
        string Provider,
        bool IsDemo,
        bool IsStale,
        DateTimeOffset UpdatedAt,
        IReadOnlyCollection<NewsArticleDto> Articles);

    public sealed record FinancialQuoteDto(
        string Symbol,
        string Name,
        decimal? LastValue,
        decimal? Change,
        decimal? ChangePercent,
        string? Currency,
        DateTimeOffset? QuoteTime,
        bool IsDelayed,
        string? ExternalUrl,
        string? Error);

    public sealed record FinancialMarketFeedDto(
        string Provider,
        bool IsDemo,
        bool IsStale,
        DateTimeOffset UpdatedAt,
        IReadOnlyCollection<FinancialQuoteDto> Quotes);
}
