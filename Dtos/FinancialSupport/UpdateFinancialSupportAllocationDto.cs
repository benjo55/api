namespace api.Dtos.FinancialSupport
{
    public class UpdateFinancialSupportAllocationDto
    {
        public int Id { get; set; }   // facultatif pour update fin
        public int ContractId { get; set; }
        public int CompartmentId { get; set; }

        public int SupportId { get; set; }
        public decimal AllocationPercentage { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }
}
