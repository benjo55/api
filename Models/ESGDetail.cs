using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ESGDetail
    {
        [Key] public int Id { get; set; }
        public int FinancialSupportId { get; set; }

        public bool IsSFDRApplicable { get; set; }
        public string SFDRArticle { get; set; } = "8";
        public string Ecolabel { get; set; } = "Greenfin";

        [Precision(18, 5)] public decimal CarbonFootprint { get; set; }
        [Precision(18, 5)] public decimal GenderEqualityScore { get; set; }
        [Precision(18, 5)] public decimal WaterUseScore { get; set; }

        public string ESGProvider { get; set; } = "MSCI";
        public string RiskLabel { get; set; } = "Low";

        public virtual FinancialSupport? FinancialSupport { get; set; }
    }
}
