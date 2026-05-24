using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public class ContractTaxState
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }
        public ContractFamily ContractFamily { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal NetPremiums { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal CurrentValue { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal TotalGainStock { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal SocialChargesAlreadyPaid { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal SocialChargesRemainingDue { get; set; }

        public DateTime? LastValuationDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public Contract? Contract { get; set; }
        public ICollection<PremiumLot> PremiumLots { get; set; } = new List<PremiumLot>();
        public ICollection<GainLot> GainLots { get; set; } = new List<GainLot>();
        public ICollection<PsHistory> PsHistoryItems { get; set; } = new List<PsHistory>();
    }
}
