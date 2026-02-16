using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class FundLifeCycle
    {
        [Key] public int Id { get; set; }
        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }
        public DateTime? InceptionDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public DateTime? LastSubscriptionDate { get; set; }
        public bool IsActive { get; set; }
        public virtual FinancialSupport? FinancialSupport { get; set; }
    }
}