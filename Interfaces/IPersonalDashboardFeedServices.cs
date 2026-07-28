using api.Dtos.PersonalDashboard;

namespace api.Interfaces
{
    public interface INewsProvider
    {
        Task<IReadOnlyCollection<NewsArticleDto>> GetNewsAsync(
            NewsCategory category,
            int limit,
            CancellationToken cancellationToken);
    }

    public interface IFinancialMarketProvider
    {
        Task<IReadOnlyCollection<FinancialQuoteDto>> GetQuotesAsync(
            IEnumerable<string> symbols,
            CancellationToken cancellationToken);
    }

    public interface INewsFeedService
    {
        Task<NewsFeedDto> GetNewsFeedAsync(
            NewsCategory category,
            int limit,
            CancellationToken cancellationToken);
    }

    public interface IFinancialMarketService
    {
        Task<FinancialMarketFeedDto> GetMarketFeedAsync(CancellationToken cancellationToken);
    }
}
