using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public class PsHistory
    {
        [Key]
        public int Id { get; set; }

        public int ContractTaxStateId { get; set; }
        public int? GainLotId { get; set; }

        public DateTime LevyDate { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal TaxableBase { get; set; }

        [Column(TypeName = "decimal(6,3)")]
        public decimal AppliedRate { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal PaidAmount { get; set; }

        public SupportNature SupportNature { get; set; } = SupportNature.Unknown;

        [MaxLength(200)]
        public string? Source { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ContractTaxState? ContractTaxState { get; set; }
        public GainLot? GainLot { get; set; }
    }
}
