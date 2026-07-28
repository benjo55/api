using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ProductFinancialSupport
    {
        public int Id { get; set; }

        public int ProductVersionId { get; set; }
        [ForeignKey(nameof(ProductVersionId))]
        public ProductVersion ProductVersion { get; set; } = null!;

        public int FinancialSupportId { get; set; }
        [ForeignKey(nameof(FinancialSupportId))]
        public FinancialSupport FinancialSupport { get; set; } = null!;

        public bool IsAvailableForSubscription { get; set; } = true;
        public bool IsAvailableForArbitration { get; set; } = true;
        public bool IsDefaultSupport { get; set; }

        [Precision(9, 6)]
        public decimal? MinimumAllocationPercentage { get; set; }

        [Precision(9, 6)]
        public decimal? MaximumAllocationPercentage { get; set; }

        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableTo { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
