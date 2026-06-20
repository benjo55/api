using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public class AdvanceTransaction
    {
        public int Id { get; set; }

        public int AdvanceId { get; set; }
        public Advance Advance { get; set; } = null!;

        public int? OperationId { get; set; }
        public Operation? Operation { get; set; }

        public DateTime OperationDate { get; set; } = DateTime.UtcNow;
        public AdvanceTransactionType Type { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}
