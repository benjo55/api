using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ProductGuarantee
    {
        public int Id { get; set; }

        public int ProductVersionId { get; set; }
        [ForeignKey(nameof(ProductVersionId))]
        public ProductVersion ProductVersion { get; set; } = null!;

        public ProductGuaranteeType GuaranteeType { get; set; }

        public bool IsMandatory { get; set; }
        public bool IsOptional { get; set; } = true;

        [Precision(20, 7)]
        public decimal? MinimumCoverageAmount { get; set; }

        [Precision(20, 7)]
        public decimal? MaximumCoverageAmount { get; set; }

        [Precision(18, 5)]
        public decimal? MinimumRate { get; set; }

        [Precision(18, 5)]
        public decimal? MaximumRate { get; set; }

        [MaxLength(2000)]
        public string? CalculationRule { get; set; }

        [MaxLength(2000)]
        public string? EligibilityConditions { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
