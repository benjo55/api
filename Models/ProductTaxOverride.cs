using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ProductTaxOverride
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Required, MaxLength(80)]
        public string ParameterKey { get; set; } = string.Empty;

        [Precision(10, 4)]
        public decimal? NumericValue { get; set; }

        [MaxLength(2000)]
        public string? JsonValue { get; set; }

        [MaxLength(500)]
        public string? Justification { get; set; }

        public DateTime ValidFrom { get; set; } = DateTime.UtcNow.Date;
        public DateTime? ValidTo { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
