using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class DistributionChannel
    {
        [Key] public int Id { get; set; }
        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }
        public string Channel { get; set; } = "CGPI";
        [Precision(18, 5)] public decimal MaxEntryFee { get; set; }
        [Precision(18, 5)] public decimal CommissionRate { get; set; }
        public bool HasRetrocession { get; set; }
        public string CommercialName { get; set; } = string.Empty;
        public virtual FinancialSupport? FinancialSupport { get; set; }
    }

}