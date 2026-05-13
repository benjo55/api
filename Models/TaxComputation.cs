using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Dtos.TaxProfile;

namespace api.Models
{
    /// <summary>
    /// Snapshot d'un calcul fiscal (requête + résultat) pour audit/rejeu.
    /// </summary>
    public class TaxComputation
    {
        [Key]
        public int Id { get; set; }

        public int TaxProfileId { get; set; }

        public int? TaxRuleVersionId { get; set; }

        public FiscalEventType EventType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossWithdrawal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GainAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalTax { get; set; }

        public string RequestJson { get; set; } = string.Empty;
        public string ResultJson { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public TaxProfile? TaxProfile { get; set; }
        public TaxRuleVersion? TaxRuleVersion { get; set; }
    }
}
