using api.Models.Enum;

namespace api.Dtos.Advance
{
    public class AdvanceDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string AdvanceNumber { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime? DisbursementDate { get; set; }
        public DateTime? MaturityDate { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public decimal OutstandingCapital { get; set; }
        public decimal InterestRate { get; set; }
        public int DurationMonths { get; set; }
        public string? Reason { get; set; }
        public AdvanceStatus Status { get; set; }
        public bool Locked { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public List<AdvanceTransactionDto> Transactions { get; set; } = new();
    }

    public class CreateAdvanceRequestDto
    {
        public int ContractId { get; set; }
        public string? AdvanceNumber { get; set; }
        public DateTime? RequestDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime? DisbursementDate { get; set; }
        public DateTime? MaturityDate { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public decimal? OutstandingCapital { get; set; }
        public decimal InterestRate { get; set; }
        public int DurationMonths { get; set; } = 36;
        public string? Reason { get; set; }
        public AdvanceStatus Status { get; set; } = AdvanceStatus.Requested;
    }

    public class UpdateAdvanceRequestDto
    {
        public DateTime? ApprovalDate { get; set; }
        public DateTime? MaturityDate { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public int? DurationMonths { get; set; }
        public string? Reason { get; set; }
        public AdvanceStatus? Status { get; set; }
        public bool? Locked { get; set; }
    }

    public class AdvanceTransactionDto
    {
        public int Id { get; set; }
        public int AdvanceId { get; set; }
        public int? OperationId { get; set; }
        public DateTime OperationDate { get; set; }
        public AdvanceTransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class CreateAdvanceTransactionRequestDto
    {
        public DateTime? OperationDate { get; set; }
        public AdvanceTransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public string? Comment { get; set; }
    }

    public class AdvanceEligibilityDto
    {
        public int ContractId { get; set; }
        public decimal EligibleEuroFundValue { get; set; }
        public decimal EligibleUnitLinkedValue { get; set; }
        public decimal MaximumAdvanceAmount { get; set; }
        public decimal OutstandingAdvanceCapital { get; set; }
        public decimal AvailableAdvanceAmount { get; set; }
    }
}
