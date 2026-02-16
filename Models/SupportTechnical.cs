using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{

    public class SupportTechnical
    {
        [Key] public int Id { get; set; }
        [ForeignKey("FinancialSupport")] public int FinancialSupportId { get; set; }

        public string DataSource { get; set; } = string.Empty;
        public string SyncStatus { get; set; } = "Synchronized";
        public DateTime LastSyncDate { get; set; } = DateTime.UtcNow;

        public virtual FinancialSupport? FinancialSupport { get; set; }
    }
}
