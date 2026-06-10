using System.ComponentModel.DataAnnotations;
using api.Models.Enum;

namespace api.Dtos.Product
{
    public class ProductOperationFeePolicyDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public OperationFeeType FeeType { get; set; }
        public FeeAmountMode Mode { get; set; }
        public decimal Rate { get; set; }
        public decimal FixedAmount { get; set; }
        public FeeApplyOn ApplyOn { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class CreateProductOperationFeePolicyDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public OperationFeeType FeeType { get; set; }

        [Required]
        public FeeAmountMode Mode { get; set; }

        [Range(0, 1000000)]
        public decimal Rate { get; set; }

        [Range(0, 1000000000)]
        public decimal FixedAmount { get; set; }

        [Required]
        public FeeApplyOn ApplyOn { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;

        public DateTime? EndDate { get; set; }
    }

    public class UpdateProductOperationFeePolicyDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public OperationFeeType FeeType { get; set; }

        [Required]
        public FeeAmountMode Mode { get; set; }

        [Range(0, 1000000)]
        public decimal Rate { get; set; }

        [Range(0, 1000000000)]
        public decimal FixedAmount { get; set; }

        [Required]
        public FeeApplyOn ApplyOn { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;

        public DateTime? EndDate { get; set; }
    }
}
