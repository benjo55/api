using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class SupportValuation
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("FinancialSupport")]
        public int FinancialSupportId { get; set; }

        public DateTime ValuationDate { get; set; }
        [Precision(18, 5)] public decimal Nav { get; set; }

        [MaxLength(50)]
        public string Source { get; set; } = string.Empty;

        [MaxLength(3)]
        public string ValuationCurrency { get; set; } = "EUR";

        public virtual FinancialSupport? FinancialSupport { get; set; }

        public bool IsEstimated { get; set; } = false;
        public DateTime? SourceTimestamp { get; set; }
        public string? SourceReferenceId { get; set; }

        [Precision(18, 5)] public decimal? PerformanceYTD { get; set; }
        [Precision(18, 5)] public decimal? Performance1Y { get; set; }
        [Precision(18, 5)] public decimal? Volatility1Y { get; set; }


    }
}
