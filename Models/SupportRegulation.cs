using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class SupportRegulation
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("FinancialSupport")]
        public int FinancialSupportId { get; set; }

        public bool IsSFDRApplicable { get; set; }
        public string? SFDRLevel { get; set; } // Article 6 / 8 / 9
        public string? ESGScoreProvider { get; set; }
        public string? MifidCategory { get; set; }

        public virtual FinancialSupport? FinancialSupport { get; set; }

        //🌱 ESG & SFDR plus détaillé :
        public string? Ecolabel { get; set; } // ISR, Greenfin
        public bool? HasPrincipalAdverseImpacts { get; set; }
        public string? PAIIndicators { get; set; } // JSON, CSV, etc.

        // 📜 Détails juridiques :
        public string? KIIDDocumentUrl { get; set; }
        public DateTime? LastKIIDUpdate { get; set; }
        public string? ProspectusUrl { get; set; }


    }
}
