using System;
using System.Collections.Generic;
using api.Dtos.FinancialSupport;

namespace api.Dtos.Compartment
{
    public class CompartmentDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }

        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ManagementMode { get; set; }
        public string? Notes { get; set; }
        public bool IsDefault { get; set; } = false;

        // 🔹 Valeur actuelle du compartiment
        public decimal CurrentValue { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public List<FinancialSupportAllocationDto> Supports { get; set; } = new();
    }
}
