using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ProductEligibilityRule
    {
        public int Id { get; set; }

        public int ProductVersionId { get; set; }
        [ForeignKey(nameof(ProductVersionId))]
        public ProductVersion ProductVersion { get; set; } = null!;

        public ProductEligibilityRuleType RuleType { get; set; }

        [MaxLength(500)]
        public string? StringValue { get; set; }

        [Precision(20, 7)]
        public decimal? NumericValue { get; set; }

        public bool? BooleanValue { get; set; }

        public bool IsBlocking { get; set; } = true;

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
