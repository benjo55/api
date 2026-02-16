using System.Text.Json.Serialization;

namespace api.Dtos.FinancialSupport
{
    public class UpdateFinancialSupportRequestDto
    {
        // Tous les champs en nullable (pour patch partiel)
        public string? Label { get; set; }
        [JsonPropertyName("isin")]
        public string? ISIN { get; set; }
        public string? SupportType { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
        public string? MarketingName { get; set; }
        public string? LegalName { get; set; }
        public string? AMFCode { get; set; }
        public string? BloombergCode { get; set; }
        public string? MorningstarCode { get; set; }
        public string? CUSIP { get; set; }
        public string? SEDOL { get; set; }
        public string? AssetManager { get; set; }
        public string? DepositaryBank { get; set; }
        public string? Custodian { get; set; }
        public DateTime? InceptionDate { get; set; }
        public DateTime? ClosureDate { get; set; }
        public bool? IsClosed { get; set; }

        // Classification
        public string? AssetClass { get; set; }
        public string? SubAssetClass { get; set; }
        public string? GeographicFocus { get; set; }
        public string? SectorFocus { get; set; }
        public string? CapitalizationPolicy { get; set; }
        public string? InvestmentStrategy { get; set; }
        public string? LegalForm { get; set; }
        public string? ManagementStyle { get; set; }
        public string? UCITSCategory { get; set; }

        // Caractéristiques financières
        public decimal? MinimumSubscription { get; set; }
        public decimal? MinimumHolding { get; set; }
        public decimal? ManagementFee { get; set; }
        public decimal? PerformanceFee { get; set; }
        public decimal? TurnoverRate { get; set; }
        public decimal? AUM { get; set; }
        public bool? IsCapitalGuaranteed { get; set; }
        public bool? IsCurrencyHedged { get; set; }
        public string? Benchmark { get; set; }

        // ESG / SFDR / MIFID
        public bool? HasESGLabel { get; set; }
        public string? ESGLabel { get; set; }
        public string? SFDRClassification { get; set; }
        public decimal? ESGScore { get; set; }
        public string? ESGScoreProvider { get; set; }
        public string? MifidTargetMarket { get; set; }
        public string? MifidRiskTolerance { get; set; }
        public string? MifidClientType { get; set; }

        // Valorisation simplifiée
        public decimal? LastValuationAmount { get; set; }
        public DateTime? LastValuationDate { get; set; }
        public decimal? WeeklyVolatility { get; set; }
        public decimal? MaxDrawdown1Y { get; set; }

        // Distribution / Réseau
        public string? Distributor { get; set; }
        public bool? IsAvailableOnline { get; set; }
        public bool? IsAdvisedSale { get; set; }
        public bool? IsEligiblePEA { get; set; }
        public string? CountryOfDistribution { get; set; }

        // External references
        public string? FundDomicile { get; set; }
        public string? PrimaryListingMarket { get; set; }
        public bool? IsFundOfFunds { get; set; }
    }
}
