using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public class ContractSupportFeeApplication
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        [ForeignKey(nameof(ContractId))]
        public Contract Contract { get; set; } = null!;

        public int FeeOperationId { get; set; }
        [ForeignKey(nameof(FeeOperationId))]
        public Operation FeeOperation { get; set; } = null!;

        public int? SourceOperationId { get; set; }
        [ForeignKey(nameof(SourceOperationId))]
        public Operation? SourceOperation { get; set; }

        public ContractSupportFeeNature FeeNature { get; set; } = ContractSupportFeeNature.OtherFee;

        public int CompartmentId { get; set; }
        [ForeignKey(nameof(CompartmentId))]
        public Compartment Compartment { get; set; } = null!;

        public int SupportId { get; set; }
        [ForeignKey(nameof(SupportId))]
        public FinancialSupport Support { get; set; } = null!;

        public FeeApplyOn ApplyOn { get; set; } = FeeApplyOn.Target;

        [Column(TypeName = "decimal(20,7)")]
        public decimal BaseAmount { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal FeeAmount { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal FeeShares { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal NavUsed { get; set; }

        public DateTime? NavDateUsed { get; set; }
        public string PolicySource { get; set; } = string.Empty;
        public int? PolicyId { get; set; }
        public DateTime EffectiveDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}