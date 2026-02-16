using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class ContractValuation
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        [ForeignKey(nameof(ContractId))]
        public Contract Contract { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }  // Valeur totale du contrat (€)

        public DateTime ValuationDate { get; set; } = DateTime.UtcNow; // Date de la valorisation

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
