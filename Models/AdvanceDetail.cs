using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class AdvanceDetail
    {
        [Key]
        public int OperationId { get; set; }
        public Operation Operation { get; set; } = null!;

        [Column(TypeName = "decimal(20,7)")]
        public decimal Amount { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal InterestRate { get; set; }
        public DateTime MaturityDate { get; set; }
    }
}
