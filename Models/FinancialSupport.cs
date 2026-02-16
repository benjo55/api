using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class FinancialSupport
    {
        [Key]
        public int Id { get; set; }

        // Identification
        [Required, MaxLength(20)] public string Code { get; set; } = string.Empty;
        [MaxLength(200)] public string Label { get; set; } = string.Empty;
        [MaxLength(12)] public string ISIN { get; set; } = string.Empty;
        [MaxLength(30)] public string SupportType { get; set; } = string.Empty;
        [MaxLength(3)] public string Currency { get; set; } = "EUR";
        [MaxLength(20)] public string Status { get; set; } = "Active";
        [MaxLength(100)] public string? MarketingName { get; set; }
        [MaxLength(100)] public string? LegalName { get; set; }
        [MaxLength(20)] public string? AMFCode { get; set; }
        [MaxLength(20)] public string? BloombergCode { get; set; }
        [MaxLength(20)] public string? MorningstarCode { get; set; }
        [MaxLength(12)] public string? CUSIP { get; set; }
        [MaxLength(12)] public string? SEDOL { get; set; }
        [MaxLength(50)] public string? AssetManager { get; set; }
        [MaxLength(100)] public string? DepositaryBank { get; set; }
        [MaxLength(100)] public string? Custodian { get; set; }
        public DateTime? InceptionDate { get; set; }
        public DateTime? ClosureDate { get; set; }
        public bool IsClosed { get; set; }

        // Classification
        [MaxLength(50)] public string? AssetClass { get; set; }
        [MaxLength(50)] public string? SubAssetClass { get; set; }
        [MaxLength(50)] public string? GeographicFocus { get; set; }
        [MaxLength(50)] public string? SectorFocus { get; set; }
        [MaxLength(30)] public string? CapitalizationPolicy { get; set; }
        [MaxLength(50)] public string? InvestmentStrategy { get; set; }
        [MaxLength(50)] public string? LegalForm { get; set; }
        [MaxLength(50)] public string? ManagementStyle { get; set; }
        [MaxLength(50)] public string? UCITSCategory { get; set; }

        // Caractéristiques financières
        [Precision(18, 5)] public decimal? MinimumSubscription { get; set; }
        [Precision(18, 5)] public decimal? MinimumHolding { get; set; }
        [Precision(18, 5)] public decimal? ManagementFee { get; set; }
        [Precision(18, 5)] public decimal? PerformanceFee { get; set; }
        [Precision(18, 5)] public decimal? TurnoverRate { get; set; }
        [Precision(18, 5)] public decimal? AUM { get; set; }
        public bool? IsCapitalGuaranteed { get; set; }
        public bool? IsCurrencyHedged { get; set; }
        [MaxLength(10)] public string? Benchmark { get; set; }

        // ESG / SFDR / MIFID
        public bool? HasESGLabel { get; set; }
        [MaxLength(50)] public string? ESGLabel { get; set; }
        [MaxLength(50)] public string? SFDRClassification { get; set; }
        [Precision(18, 5)] public decimal? ESGScore { get; set; }
        [MaxLength(100)] public string? ESGScoreProvider { get; set; }
        [MaxLength(50)] public string? MifidTargetMarket { get; set; }
        [MaxLength(50)] public string? MifidRiskTolerance { get; set; }
        [MaxLength(50)] public string? MifidClientType { get; set; }

        // Valorisation simplifiée
        [Precision(18, 5)] public decimal? LastValuationAmount { get; set; }
        public DateTime? LastValuationDate { get; set; }
        [Precision(18, 5)] public decimal? WeeklyVolatility { get; set; }
        [Precision(18, 5)] public decimal? MaxDrawdown1Y { get; set; }

        // Distribution / Réseau
        [MaxLength(100)] public string? Distributor { get; set; }
        public bool? IsAvailableOnline { get; set; }
        public bool? IsAdvisedSale { get; set; }
        public bool? IsEligiblePEA { get; set; }
        [MaxLength(100)] public string? CountryOfDistribution { get; set; }

        // Données techniques
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<SupportValuation> Valuations { get; set; } = new List<SupportValuation>();
        public virtual ICollection<SupportDistribution>? Distributions { get; set; }
        public virtual ICollection<SupportRiskProfile>? RiskProfiles { get; set; }
        public virtual ICollection<SupportRegulation>? Regulations { get; set; }
        public virtual ICollection<SupportDocument>? Documents { get; set; }
        public virtual ICollection<SupportHistoricalData>? HistoricalData { get; set; }
        public virtual ICollection<SupportFeeDetail>? FeeDetails { get; set; }
        public virtual ICollection<SupportLookthroughAsset>? LookthroughAssets { get; set; }

        // External references
        [MaxLength(50)] public string? FundDomicile { get; set; }
        [MaxLength(50)] public string? PrimaryListingMarket { get; set; }
        public bool? IsFundOfFunds { get; set; }
    }
}
