using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ProductFeeRule
    {
        public int Id { get; set; }

        public int ProductVersionId { get; set; }
        [ForeignKey(nameof(ProductVersionId))]
        public ProductVersion ProductVersion { get; set; } = null!;

        public FeeType FeeType { get; set; }
        public ProductFeeCalculationMethod CalculationMethod { get; set; }

        [Precision(18, 5)]
        public decimal? Rate { get; set; }

        [Precision(20, 7)]
        public decimal? FixedAmount { get; set; }

        [Precision(20, 7)]
        public decimal? MinimumAmount { get; set; }

        [Precision(20, 7)]
        public decimal? MaximumAmount { get; set; }

        public int? FreeOperationCount { get; set; }

        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
