using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ProductOperationFeePolicy
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        public OperationFeeType FeeType { get; set; } = OperationFeeType.Entry;
        public FeeAmountMode Mode { get; set; } = FeeAmountMode.Percentage;

        [Precision(18, 5)]
        public decimal Rate { get; set; } // percent if Mode==Percentage

        [Precision(18, 5)]
        public decimal FixedAmount { get; set; } // currency amount if Mode==Fixed

        public FeeApplyOn ApplyOn { get; set; } = FeeApplyOn.Target;

        public bool IsEnabled { get; set; } = true;

        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime? EndDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}
