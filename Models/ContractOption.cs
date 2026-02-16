using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class ContractOption
    {
        public int Id { get; set; }  // PK
        public int ContractId { get; set; }  // FK vers Contract
        [ForeignKey("ContractId")]
        public int ContractOptionTypeId { get; set; }  // FK vers le catalogue

        public bool IsActive { get; set; } = true;  // option activée ?
        public string? Description { get; set; }    // notes personnalisées
        public string? CustomParameters { get; set; } // JSON libre (seuil %, fréquence, etc.)

        // Navigation
        public Contract? Contract { get; set; }
        public ContractOptionType? ContractOptionType { get; set; }
    }
}
