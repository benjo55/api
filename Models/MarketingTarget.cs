using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class MarketingTarget
    {
        [Key] public int Id { get; set; }
        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }
        [MaxLength(5)] public string CountryCode { get; set; } = "FR";
        [MaxLength(20)] public string ChannelType { get; set; } = "Bank";
        [MaxLength(50)] public string Segment { get; set; } = "Mass Affluent";
        public bool IsDistributed { get; set; }
        public bool IsHighlighted { get; set; }
        public string LocalName { get; set; } = string.Empty;
        public virtual FinancialSupport? FinancialSupport { get; set; }
    }
}