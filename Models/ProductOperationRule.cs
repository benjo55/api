using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ProductOperationRule
    {
        public int Id { get; set; }

        public int ProductVersionId { get; set; }
        [ForeignKey(nameof(ProductVersionId))]
        public ProductVersion ProductVersion { get; set; } = null!;

        public ProductOperationType OperationType { get; set; }

        public bool IsAllowed { get; set; } = true;

        [Precision(20, 7)]
        public decimal? MinimumAmount { get; set; }

        [Precision(20, 7)]
        public decimal? MaximumAmount { get; set; }

        [Precision(20, 7)]
        public decimal? MinimumRemainingAmount { get; set; }

        [Precision(9, 6)]
        public decimal? MaximumPercentage { get; set; }

        public int? MinimumHoldingPeriodInMonths { get; set; }
        public int? ProcessingDelayInBusinessDays { get; set; }

        public bool RequiresApproval { get; set; }
        public bool RequiresSupportingDocument { get; set; }

        [MaxLength(2000)]
        public string? Conditions { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
