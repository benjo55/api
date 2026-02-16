using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class MultilingualDocument
    {
        [Key] public int Id { get; set; }
        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }
        public string DocumentType { get; set; } = "KIID";
        public string Language { get; set; } = "fr";
        public string Url { get; set; } = string.Empty;
        public DateTime PublicationDate { get; set; }
        public string Version { get; set; } = "2024.1";
        public virtual FinancialSupport? FinancialSupport { get; set; }
    }
}