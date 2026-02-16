using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ShareClass
    {
        [Key] public int Id { get; set; }
        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }

        [MaxLength(20)] public string Code { get; set; } = string.Empty;
        [MaxLength(12)] public string ISIN { get; set; } = string.Empty;
        [MaxLength(10)] public string Currency { get; set; } = "EUR";
        public DateTime? LaunchDate { get; set; }

        [MaxLength(30)] public string DistributionPolicy { get; set; } = "Accumulation"; // or "Distribution"
        [MaxLength(30)] public string Category { get; set; } = "Retail";

        [Precision(18, 5)] public decimal EntryFee { get; set; }
        [Precision(18, 5)] public decimal ExitFee { get; set; }
        [Precision(18, 5)] public decimal ManagementFee { get; set; }
        [Precision(18, 5)] public decimal OngoingCharges { get; set; }

        [MaxLength(10)] public string CountryOfRegistration { get; set; } = "FR";

        public virtual FinancialSupport? FinancialSupport { get; set; }
    }

}