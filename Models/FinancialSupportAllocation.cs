using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class FinancialSupportAllocation
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        [ForeignKey("ContractId")]
        public Contract Contract { get; set; } = null!;

        public int? CompartmentId { get; set; }
        [ForeignKey(nameof(CompartmentId))]
        public Compartment? Compartment { get; set; }

        public int SupportId { get; set; }
        [ForeignKey("SupportId")]
        public FinancialSupport Support { get; set; } = null!;


        public decimal AllocationPercentage { get; set; } // en %

        [Column(TypeName = "decimal(20,7)")]
        public decimal CurrentShares { get; set; }
        [Column(TypeName = "decimal(20,7)")]
        public decimal CurrentAmount { get; set; }
        [Column(TypeName = "decimal(20,7)")]
        public decimal InvestedAmount { get; set; } = 0m;

        // FinancialSupportAllocation.cs
        [NotMapped]
        public decimal? Pru { get; set; }

        [NotMapped]
        public decimal Performance { get; set; }


        // 🔹 Ajout cohérent avec les autres entités
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        // =====================================================================
        // 🆕 Champs enrichis uniquement pour la logique métier / front
        // =====================================================================

        [NotMapped]
        public List<int> Compartments { get; set; } = new(); // ✅ liste des compartiments contenant ce support

        [NotMapped]
        public bool IsMultiCompartment { get; set; } // ✅ true si le support est présent dans plusieurs compartiments

    }
}
