using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public class Advance
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public Contract Contract { get; set; } = null!;

        [Required, MaxLength(40)]
        public string AdvanceNumber { get; set; } = string.Empty;

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovalDate { get; set; }
        public DateTime? DisbursementDate { get; set; }
        public DateTime? MaturityDate { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal RequestedAmount { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal? ApprovedAmount { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal OutstandingCapital { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal InterestRate { get; set; }

        public int DurationMonths { get; set; } = 36;

        [MaxLength(500)]
        public string? Reason { get; set; }

        public AdvanceStatus Status { get; set; } = AdvanceStatus.Requested;
        public bool Locked { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<AdvanceTransaction> Transactions { get; set; } = new List<AdvanceTransaction>();

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}
