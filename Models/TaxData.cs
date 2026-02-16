using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class TaxData
    {
        [Key] public int Id { get; set; }
        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }

        public bool IsFATCAEligible { get; set; }
        public bool IsCRSReportable { get; set; }

        public string CountryOfTaxation { get; set; } = "FR";
        public string LegalForm { get; set; } = "FCP";

        public virtual FinancialSupport? FinancialSupport { get; set; }
    }

}