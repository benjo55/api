using api.Models.Enum;

namespace api.Dtos.Contract
{
    public class ContractSupportFeeApplicationDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public int FeeOperationId { get; set; }
        public int? SourceOperationId { get; set; }
        public ContractSupportFeeNature FeeNature { get; set; }
        public int CompartmentId { get; set; }
        public string CompartmentLabel { get; set; } = string.Empty;
        public int SupportId { get; set; }
        public string SupportLabel { get; set; } = string.Empty;
        public string SupportIsin { get; set; } = string.Empty;
        public FeeApplyOn ApplyOn { get; set; }
        public decimal BaseAmount { get; set; }
        public FeeAmountMode? FeeMode { get; set; }
        public decimal? AppliedRate { get; set; }
        public ManagementFeeRateBase? RateBase { get; set; }
        public ManagementFeeFrequency? Frequency { get; set; }
        public ManagementFeeProrataMethod? ProrataMethod { get; set; }
        public ManagementFeePostingMode? PostingMode { get; set; }
        public DateTime? AccrualStartDate { get; set; }
        public DateTime? AccrualEndDate { get; set; }
        public int? AccruedDays { get; set; }
        public decimal FeeAmount { get; set; }
        public decimal FeeShares { get; set; }
        public decimal NavUsed { get; set; }
        public DateTime? NavDateUsed { get; set; }
        public string PolicySource { get; set; } = string.Empty;
        public int? PolicyId { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ContractSupportFeeAggregateDto
    {
        public int ContractId { get; set; }
        public int CompartmentId { get; set; }
        public string CompartmentLabel { get; set; } = string.Empty;
        public int SupportId { get; set; }
        public string SupportLabel { get; set; } = string.Empty;
        public string SupportIsin { get; set; } = string.Empty;
        public ContractSupportFeeNature FeeNature { get; set; }
        public decimal TotalFeeAmount { get; set; }
        public decimal TotalFeeShares { get; set; }
    }
}
