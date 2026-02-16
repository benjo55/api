using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class FundScenario
    {
        [Key] public int Id { get; set; }
        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }
        public string ScenarioType { get; set; } = "Favorable";
        [Precision(18, 5)] public decimal ProjectedPerformance { get; set; }
        [Precision(18, 5)] public decimal CostImpact { get; set; }
        public string Methodology { get; set; } = string.Empty;
        public virtual FinancialSupport? FinancialSupport { get; set; }
    }
}