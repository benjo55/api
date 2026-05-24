using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public class GainLot
    {
        [Key]
        public int Id { get; set; }

        public int ContractTaxStateId { get; set; }
        public int? TaxGenerationId { get; set; }

        public DateTime GainDate { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal GainAmount { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal RemainingGainAmount { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal SocialChargesAlreadyPaid { get; set; }

        [Column(TypeName = "decimal(6,3)")]
        public decimal ApplicableSocialRate { get; set; }

        public SupportNature SupportNature { get; set; } = SupportNature.Unknown;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ContractTaxState? ContractTaxState { get; set; }
        public TaxGeneration? TaxGeneration { get; set; }
    }
}
