using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ProductPaymentRule
    {
        public int Id { get; set; }

        public int ProductVersionId { get; set; }
        [ForeignKey(nameof(ProductVersionId))]
        public ProductVersion ProductVersion { get; set; } = null!;

        public ProductPaymentType PaymentType { get; set; }
        public ProductPaymentFrequency? Frequency { get; set; }

        [Precision(20, 7)]
        public decimal? MinimumAmount { get; set; }

        [Precision(20, 7)]
        public decimal? MaximumAmount { get; set; }

        public bool IsAllowed { get; set; } = true;
        public bool RequiresManualApproval { get; set; }

        public int? ProcessingDelayInBusinessDays { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
