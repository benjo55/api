using api.Dtos.FinancialSupport;

namespace api.Dtos.Compartment
{
    public class UpdateCompartmentRequestDto
    {
        public int Id { get; set; }   // utile si plus tard tu veux update partiel
        public int ContractId { get; set; }

        public string Label { get; set; } = "Compartiment";
        public string? Description { get; set; }
        public string? ManagementMode { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

    }
}
