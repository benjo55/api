using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public class AdvanceDetail
    {
        [Key]
        public int OperationId { get; set; }
        public Operation Operation { get; set; } = null!;

        public int? AdvanceId { get; set; }
        public Advance? Advance { get; set; }

        public AdvanceTransactionType? TransactionType { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        // Legacy fields kept for compatibility with existing operation rows.
        [Column(TypeName = "decimal(20,7)")]
        public decimal Amount { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal InterestRate { get; set; }
        public DateTime MaturityDate { get; set; }
    }
}
