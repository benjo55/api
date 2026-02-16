using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class WithdrawalDetail
    {
        [Key]
        public int OperationId { get; set; }   // PK = FK
        public Operation Operation { get; set; } = null!;

        [Column(TypeName = "decimal(20,7)")]
        public decimal GrossAmount { get; set; }
        public bool IsScheduled { get; set; }
        public string? Frequency { get; set; }
    }
}
