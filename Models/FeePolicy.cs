using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class FeePolicy
    {
        public int Id { get; set; }

        public FeeCategory Category { get; set; } = FeeCategory.Operation;
        public FeeType FeeType { get; set; } = FeeType.Entry;
        public FeeScope Scope { get; set; } = FeeScope.Product;

        public int? ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        public int? ContractId { get; set; }
        [ForeignKey(nameof(ContractId))]
        public Contract? Contract { get; set; }

        public int? CompartmentId { get; set; }
        [ForeignKey(nameof(CompartmentId))]
        public Compartment? Compartment { get; set; }

        public int? FinancialSupportId { get; set; }
        [ForeignKey(nameof(FinancialSupportId))]
        public FinancialSupport? FinancialSupport { get; set; }

        [MaxLength(50)]
        public string? SupportType { get; set; }

        public FeeAmountMode AmountMode { get; set; } = FeeAmountMode.Percentage;
        public FeeApplyOn ApplyOn { get; set; } = FeeApplyOn.Target;

        [Precision(18, 5)]
        public decimal Rate { get; set; }

        [Precision(18, 5)]
        public decimal FixedAmount { get; set; }

        [Precision(18, 5)]
        public decimal? MinAmount { get; set; }

        [Precision(18, 5)]
        public decimal? MaxAmount { get; set; }

        public int Priority { get; set; } = 100;
        public bool IsOverride { get; set; }

        public bool IsEnabled { get; set; } = true;
        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime? EndDate { get; set; }

        public ManagementFeeFrequency? Frequency { get; set; }
        public ManagementFeeRateBase? RateBase { get; set; }
        public ManagementFeeProrataMethod? ProrataMethod { get; set; }
        public ManagementFeePostingMode? PostingMode { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}
