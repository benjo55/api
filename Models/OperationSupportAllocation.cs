using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class OperationSupportAllocation
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // 🔗 Lien vers l’opération
        public int OperationId { get; set; }
        public Operation? Operation { get; set; } = null!;

        // 🔗 Lien vers le support financier
        public int SupportId { get; set; }
        public FinancialSupport Support { get; set; } = null!;

        public int? CompartmentId { get; set; }   // nullable pour compatibilité
        public Compartment? Compartment { get; set; }  // navigation optionnelle

        // 🔹 Répartition : soit en montants, soit en %
        [Column(TypeName = "decimal(20,7)")]
        public decimal? Amount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? Percentage { get; set; }

        // ⭐ ESTIMATION AU MOMENT DE LA CRÉATION
        [Column(TypeName = "decimal(20,7)")]
        public decimal? EstimatedNav { get; set; }     // VL connue J (optionnelle)
        [Column(TypeName = "decimal(20,7)")]
        public decimal? EstimatedShares { get; set; }  // Parts estimées J
        [Column(TypeName = "decimal(20,7)")]
        public decimal? Shares { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal? NavAtOperation { get; set; }       // VL utilisée
        public DateTime? NavDateAtOperation { get; set; }  // Date de cette VL

        // =====================================================================
        // 🆕 Champs enrichis uniquement pour la logique métier / front
        // =====================================================================

        [NotMapped]
        public List<int> Compartments { get; set; } = new(); // Liste des compartiments impactés

        [NotMapped]
        public bool IsMultiCompartment { get; set; } // true si support présent dans plusieurs compartiments

    }
}
