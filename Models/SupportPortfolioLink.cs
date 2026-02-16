using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class SupportPortfolioLink
    {
        [Key] public int Id { get; set; }
        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }

        public string PortfolioCode { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public string RiskProfile { get; set; } = string.Empty;

        public virtual FinancialSupport? FinancialSupport { get; set; }
    }
}
