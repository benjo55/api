using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class SupportRiskProfile
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("FinancialSupport")]
        public int FinancialSupportId { get; set; }

        public int SRRI { get; set; } // Echelle 1 à 7
        public string? RiskDescription { get; set; }

        public virtual FinancialSupport? FinancialSupport { get; set; }

        [Precision(18, 5)] public decimal? Volatility3Y { get; set; }
        public string? MorningstarRiskRating { get; set; }
        public string? RiskRegion { get; set; }

        [MaxLength(1000)] public string? RiskNarrative { get; set; }


    }
}
