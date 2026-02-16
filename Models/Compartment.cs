using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class Compartment
    {
        public int Id { get; set; }

        // FK -> Contract
        public int ContractId { get; set; }
        [ForeignKey(nameof(ContractId))]
        public Contract Contract { get; set; } = null!;

        // Métadonnées
        public string Label { get; set; } = "Compartiment 1";
        public string Description { get; set; } = "Description du compartiment";
        public string? ManagementMode { get; set; } // "Libre", "Pilotée - Prudent/Équilibré/Dynamique", "À horizon"
        public string? Notes { get; set; }

        public bool IsDefault { get; set; } = false; // ✅ nouveau champ

        [Column(TypeName = "decimal(18,5)")]
        public decimal CurrentValue { get; set; }

        // Répartition interne (UC/ETF)
        public ICollection<FinancialSupportAllocation> Supports { get; set; } = new List<FinancialSupportAllocation>();

        [NotMapped]
        public decimal TotalInvested { get; set; }

        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; }
    }
}
