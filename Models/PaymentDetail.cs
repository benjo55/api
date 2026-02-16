using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class PaymentDetail
    {
        [Key]
        public int OperationId { get; set; }
        public Operation Operation { get; set; } = null!;

        public string PaymentMethod { get; set; } = "Virement";
        public string? Frequency { get; set; }
        public string? SourceOfFunds { get; set; }
        [Column(TypeName = "decimal(20,7)")]
        public decimal Amount { get; set; }
    }
}
