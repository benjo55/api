using System.ComponentModel.DataAnnotations;
using api.Models.Enum;

namespace api.Dtos.FeePolicy
{
    public class FeePolicyDto
    {
        public int Id { get; set; }
        public int? ProductId { get; set; }
        public int? ContractId { get; set; }
        public int? CompartmentId { get; set; }
        public int? FinancialSupportId { get; set; }
        public string? SupportType { get; set; }

        public FeeCategory Category { get; set; }
        public FeeType FeeType { get; set; }
        public FeeScope Scope { get; set; }
        public FeeAmountMode AmountMode { get; set; }
        public FeeApplyOn ApplyOn { get; set; }

        public decimal Rate { get; set; }
        public decimal FixedAmount { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public int Priority { get; set; }
        public bool IsOverride { get; set; }
        public bool IsEnabled { get; set; }

        public ManagementFeeFrequency? Frequency { get; set; }
        public ManagementFeeRateBase? RateBase { get; set; }
        public ManagementFeeProrataMethod? ProrataMethod { get; set; }
        public ManagementFeePostingMode? PostingMode { get; set; }

        public DateTime EffectiveDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class UpsertFeePolicyDto
    {
        public int? ProductId { get; set; }
        public int? ContractId { get; set; }
        public int? CompartmentId { get; set; }
        public int? FinancialSupportId { get; set; }

        [MaxLength(50)]
        public string? SupportType { get; set; }

        [Required]
        public FeeCategory Category { get; set; }

        [Required]
        public FeeType FeeType { get; set; }

        [Required]
        public FeeScope Scope { get; set; }

        [Required]
        public FeeAmountMode AmountMode { get; set; }

        [Required]
        public FeeApplyOn ApplyOn { get; set; }

        [Range(0, 1000000)]
        public decimal Rate { get; set; }

        [Range(0, 1000000000)]
        public decimal FixedAmount { get; set; }

        [Range(0, 1000000000)]
        public decimal? MinAmount { get; set; }

        [Range(0, 1000000000)]
        public decimal? MaxAmount { get; set; }

        public int Priority { get; set; } = 100;
        public bool IsOverride { get; set; }
        public bool IsEnabled { get; set; } = true;

        public ManagementFeeFrequency? Frequency { get; set; }
        public ManagementFeeRateBase? RateBase { get; set; }
        public ManagementFeeProrataMethod? ProrataMethod { get; set; }
        public ManagementFeePostingMode? PostingMode { get; set; }

        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime? EndDate { get; set; }
    }
}
