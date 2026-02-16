using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using api.Dtos.FinancialSupport;

namespace api.Dtos.Compartment
{
    public class CreateCompartmentRequestDto
    {
        [Required]
        public int ContractId { get; set; }

        [Required]
        public string Label { get; set; } = "Compartiment";

        public string? Description { get; set; }
        public string? ManagementMode { get; set; }
        public string? Notes { get; set; }

    }
}
