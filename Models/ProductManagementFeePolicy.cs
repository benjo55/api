using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ProductManagementFeePolicy
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Precision(18, 5)]
        public decimal AnnualRate { get; set; }

        public ManagementFeeFrequency Frequency { get; set; } = ManagementFeeFrequency.Monthly;

        public ManagementFeeProrataMethod ProrataMethod { get; set; } = ManagementFeeProrataMethod.Periodic;

        public ManagementFeePostingMode PostingMode { get; set; } = ManagementFeePostingMode.UnitCancellation;

        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;

        public DateTime? EndDate { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}