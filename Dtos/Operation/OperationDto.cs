using api.Dtos.Contract;
using api.Dtos.FinancialSupport;
using api.Models;

namespace api.Dtos.Operation
{
    public class OperationDto
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public string? ContractNumber { get; set; }
        public ContractDto? Contract { get; set; }

        public OperationType Type { get; set; }
        public OperationStatus Status { get; set; }

        public DateTime OperationDate { get; set; }
        public DateTime? ExecutionDate { get; set; }
        public decimal? Amount { get; set; }
        public string Currency { get; set; } = "EUR";

        // 🔹 Détails polymorphes (discriminant JSON : "kind")
        public OperationDetailsDto? Details { get; set; }

        // 🔹 Rétrocompatibilité — conservés pour AdvanceDetail (pas encore migré)
        public AdvanceDetailDto? AdvanceDetail { get; set; }

        public List<OperationSupportAllocationDto> Allocations { get; set; } = new();
    }

    public class AdvanceDetailDto
    {
        public decimal Amount { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime MaturityDate { get; set; }
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

        public OperationFlow? Flow { get; set; }

    }

    public class OperationListDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public OperationType Type { get; set; }
    }
}
