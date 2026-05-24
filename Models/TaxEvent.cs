using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public class TaxEvent
    {
        [Key]
        public int Id { get; set; }

        public int ContractTaxStateId { get; set; }
        public int? OperationId { get; set; }
        public int? TaxComputationId { get; set; }

        public TaxEventKind EventKind { get; set; }
        public DateTime EventDate { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal Amount { get; set; }

        [MaxLength(120)]
        public string Source { get; set; } = "TaxEngine";

        public string PayloadJson { get; set; } = "{}";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ContractTaxState? ContractTaxState { get; set; }
        public Operation? Operation { get; set; }
        public TaxComputation? TaxComputation { get; set; }
    }
}
