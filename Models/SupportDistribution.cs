using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class SupportDistribution
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("FinancialSupport")]
        public int FinancialSupportId { get; set; }

        [Precision(18, 5)] public decimal EntryFee { get; set; }
        [Precision(18, 5)] public decimal ExitFee { get; set; }
        [Precision(18, 5)] public decimal OngoingCharges { get; set; }
        public bool HasRetrocession { get; set; }

        public virtual FinancialSupport? FinancialSupport { get; set; }

        public string? DistributionFrequency { get; set; } // Mensuelle, Trimestrielle, etc.
        public DateTime? LastDistributionDate { get; set; }
        [Precision(18, 5)] public decimal? AverageRetrocessionRate { get; set; }
        public bool? IsCleanShare { get; set; } // Part sans rétrocession

        public string? DistributionRegion { get; set; }


    }
}
