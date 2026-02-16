namespace api.Dtos.Yahoo
{
    public class YahooETFDto
    {
        public string Ticker { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal LastNav { get; set; }
        public DateTime LastNavDate { get; set; }
        public List<HistoricalNavDto> Historicals { get; set; } = new();
    }
    public class HistoricalNavDto
    {
        public DateTime Date { get; set; }
        public decimal Nav { get; set; }
    }
}
