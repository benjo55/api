using System.ComponentModel.DataAnnotations;
using api.Models.Enum;

namespace api.Models
{
    public class TaxGeneration
    {
        [Key]
        public int Id { get; set; }

        public int? TaxLawId { get; set; }

        [Required, MaxLength(80)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(160)]
        public string Label { get; set; } = string.Empty;

        public ContractFamily ProductType { get; set; }
        public TaxRuleType TaxRuleType { get; set; } = TaxRuleType.IncomeTax;
        public TaxCompartmentType TaxCompartmentType { get; set; } = TaxCompartmentType.General;

        public DateTime EffectiveDateStart { get; set; }
        public DateTime? EffectiveDateEnd { get; set; }

        public bool RequiresPaymentDateSplit { get; set; } = true;
        public bool RequiresHoldingDuration { get; set; } = false;
        public bool UsesHistoricalSocialRate { get; set; } = false;

        [MaxLength(2000)]
        public string? FormulaMetadataJson { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public TaxLaw? TaxLaw { get; set; }
    }
}
