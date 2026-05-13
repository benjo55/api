using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class ContractManagementFeeAccrual
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }
        [ForeignKey(nameof(ContractId))]
        public Contract Contract { get; set; } = null!;

        public int SupportId { get; set; }
        [ForeignKey(nameof(SupportId))]
        public FinancialSupport Support { get; set; } = null!;

        public int CompartmentId { get; set; }
        [ForeignKey(nameof(CompartmentId))]
        public Compartment Compartment { get; set; } = null!;

        [Column(TypeName = "decimal(20,7)")]
        public decimal AccruedAmount { get; set; }

        public DateTime? LastAccruedDate { get; set; }
        public DateTime? LastPostedDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}