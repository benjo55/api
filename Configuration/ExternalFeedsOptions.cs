namespace api.Configuration
{
    public sealed class ExternalFeedsOptions
    {
        public NewsFeedOptions News { get; init; } = new();

        public FinancialMarketsOptions FinancialMarkets { get; init; } = new();
    }

    public sealed class NewsFeedOptions
    {
        public string Provider { get; init; } = "Demo";

        public int CacheDurationMinutes { get; init; } = 15;

        public int DefaultLimit { get; init; } = 6;

        public int MaxLimit { get; init; } = 12;

        public NewsFeedDefinition[] Feeds { get; init; } = [];
    }

    public sealed class NewsFeedDefinition
    {
        public string Key { get; init; } = "";

        public string Name { get; init; } = "";

        public string Category { get; init; } = "";

        public string Url { get; init; } = "";

        public string Language { get; init; } = "fr";

        public int Priority { get; init; } = 0;

        public bool IsEnabled { get; init; } = true;
    }

    public sealed class FinancialMarketsOptions
    {
        public string Provider { get; init; } = "Demo";

        public int CacheDurationMinutes { get; init; } = 5;

        public string[] Symbols { get; init; } =
        [
            "CAC40",
            "EUROSTOXX50",
            "DAX",
            "FTSE100",
            "SP500",
            "NASDAQ",
            "DOWJONES",
            "NIKKEI225",
            "EURUSD",
            "GOLD",
            "BRENT",
            "BTC",
            "ETH"
        ];
    }
}
