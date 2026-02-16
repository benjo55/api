
namespace api.Dtos.Operation
{
    public class OperationCreateResponseDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;

        // 🔹 Liste synthétique des allocations
        public List<SimpleAllocationDto> Allocations { get; set; } = new();

        // 🔹 Données contractuelles mises à jour
        public decimal CurrentValue { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalWithdrawals { get; set; }
    }

    public class SimpleAllocationDto
    {
        public int SupportId { get; set; }
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
        public decimal Shares { get; set; }
    }
}
