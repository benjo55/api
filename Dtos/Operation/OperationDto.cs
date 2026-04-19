using api.Dtos.Compartment;
using api.Dtos.Contract;
using api.Dtos.FinancialSupport;

namespace api.Dtos.Operation
{
    public class OperationDto
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public string? ContractNumber { get; set; }

        public int? CompartmentId { get; set; }   // 🔹 Optionnel
        public ContractDto? Contract { get; set; }
        public CompartmentDto? Compartment { get; set; }

        public OperationType Type { get; set; }
        public OperationStatus Status { get; set; }

        public DateTime OperationDate { get; set; }
        public DateTime? ExecutionDate { get; set; }
        public decimal? Amount { get; set; }
        public string Currency { get; set; } = "EUR";

        // 🔹 Détails optionnels
        public WithdrawalDetailDto? WithdrawalDetail { get; set; }
        public ArbitrageDetailDto? ArbitrageDetail { get; set; }
        public AdvanceDetailDto? AdvanceDetail { get; set; }
        public PaymentDetailDto? PaymentDetail { get; set; }

        public List<OperationSupportAllocationDto> Allocations { get; set; } = new();
    }

    public class WithdrawalDetailDto
    {
        public decimal GrossAmount { get; set; }
        public bool IsScheduled { get; set; }
        public string? Frequency { get; set; }
        public DateTime? StartDate { get; set; }
    }

    public class ArbitrageDetailDto
    {
        public int FromSupportId { get; set; }
        public int ToSupportId { get; set; }
        public decimal Percentage { get; set; }
    }

    public class AdvanceDetailDto
    {
        public decimal Amount { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime MaturityDate { get; set; }
    }

    public class PaymentDetailDto
    {
        public string PaymentMethod { get; set; } = "Virement";
        public string? Frequency { get; set; }
        public string? SourceOfFunds { get; set; }
        public decimal Amount { get; set; }
        public DateTime? StartDate { get; set; }
    }

    public class OperationSupportAllocationDto
    {
        public int SupportId { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Percentage { get; set; }
        public decimal? Shares { get; set; }
        public decimal? NavAtOperation { get; set; }
        public DateTime? NavDateAtOperation { get; set; }

        public FinancialSupportLightDto? Support { get; set; }  // 👈 enrichi

        public int? CompartmentId { get; set; }
    }

    public class OperationListDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public OperationType Type { get; set; }
    }
}
