using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public class PremiumLot
    {
        [Key]
        public int Id { get; set; }

        public int ContractTaxStateId { get; set; }
        public int? TaxGenerationId { get; set; }

        public DateTime PaymentDate { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal GrossPremium { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal NetPremium { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal RemainingNetPremium { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal SocialChargesPaid { get; set; }

        public int? AgeAtPayment { get; set; }
        public TaxCompartmentType TaxCompartmentType { get; set; } = TaxCompartmentType.General;
        public SupportNature SupportNature { get; set; } = SupportNature.Unknown;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ContractTaxState? ContractTaxState { get; set; }
        public TaxGeneration? TaxGeneration { get; set; }
    }
}
