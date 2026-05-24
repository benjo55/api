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
        public DateTime? StartDate { get; set; }
        public OperationScheduleStatus? ScheduleStatus { get; set; }
        public string? ScheduleGroupId { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public DateTime? StoppedAt { get; set; }
        public string? SourceOfFunds { get; set; }
        [Column(TypeName = "decimal(20,7)")]
        public decimal Amount { get; set; }
    }
}
