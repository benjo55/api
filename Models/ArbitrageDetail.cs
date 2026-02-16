using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class ArbitrageDetail
    {
        [Key]
        public int OperationId { get; set; }
        public Operation Operation { get; set; } = null!;

        public int FromSupportId { get; set; }
        public int ToSupportId { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal Percentage { get; set; }
    }
}
