using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class ClientTypeCompliance
    {
        [Key] public int Id { get; set; }
        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }
        public string ClientType { get; set; } = "Retail"; // or Institutional
        public string MifidCategory { get; set; } = "Non-complex";
        public bool IsEligible { get; set; }
        public string ExclusionReason { get; set; } = string.Empty;
        public virtual FinancialSupport? FinancialSupport { get; set; }
    }
}