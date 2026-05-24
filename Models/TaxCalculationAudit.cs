using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class TaxCalculationAudit
    {
        [Key]
        public int Id { get; set; }

        public int? TaxComputationId { get; set; }
        public int? ContractTaxStateId { get; set; }
        public int? TaxGenerationId { get; set; }

        [MaxLength(120)]
        public string StepCode { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Label { get; set; } = string.Empty;

        [Column(TypeName = "decimal(20,7)")]
        public decimal? BaseAmount { get; set; }

        [Column(TypeName = "decimal(6,3)")]
        public decimal? Rate { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal? ComputedAmount { get; set; }

        public string DetailsJson { get; set; } = "{}";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public TaxComputation? TaxComputation { get; set; }
        public ContractTaxState? ContractTaxState { get; set; }
        public TaxGeneration? TaxGeneration { get; set; }
    }
}
