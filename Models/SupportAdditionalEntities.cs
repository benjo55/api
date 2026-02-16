using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class SupportDocument
    {
        [Key] public int Id { get; set; }

        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }
        public string DocumentType { get; set; } = string.Empty; // KIID, Prospectus, ESG Report, etc.
        public string Url { get; set; } = string.Empty;
        public DateTime? PublicationDate { get; set; }

        public virtual FinancialSupport? FinancialSupport { get; set; }
    }
    public class SupportHistoricalData
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("FinancialSupport")]
        public int FinancialSupportId { get; set; }

        public DateTime Date { get; set; }

        // Pour graphique Candlestick (EOD)
        [Precision(18, 5)] public decimal? Open { get; set; }
        [Precision(18, 5)] public decimal? High { get; set; }
        [Precision(18, 5)] public decimal? Low { get; set; }
        [Precision(18, 5)] public decimal? Close { get; set; }
        public long? Volume { get; set; }

        // Données NAV / PERF complémentaires
        [Precision(18, 5)] public decimal? Nav { get; set; }
        [Precision(18, 5)] public decimal? PerformanceYTD { get; set; }
        [Precision(18, 5)] public decimal? AUM { get; set; }
        [Precision(18, 5)] public decimal? Volatility1Y { get; set; }

        public virtual FinancialSupport? FinancialSupport { get; set; }
    }


    public class SupportFeeDetail
    {
        [Key] public int Id { get; set; }

        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }
        public string FeeType { get; set; } = string.Empty; // Entry, Exit, Management, Performance, etc.
        [Precision(18, 5)] public decimal Rate { get; set; }
        public string? Conditions { get; set; }
        public string? Currency { get; set; } = "EUR";

        public virtual FinancialSupport? FinancialSupport { get; set; }
    }

    public class SupportLookthroughAsset
    {
        [Key] public int Id { get; set; }

        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string ISIN { get; set; } = string.Empty;
        public string AssetClass { get; set; } = string.Empty;
        [Precision(18, 5)] public decimal Weight { get; set; } // In percentage (0-100)

        public virtual FinancialSupport? FinancialSupport { get; set; }
    }
}
