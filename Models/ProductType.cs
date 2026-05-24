using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class ProductType
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Label { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Category { get; set; } = "Insurance";

        public int? DefaultTaxProfileId { get; set; }
        [ForeignKey(nameof(DefaultTaxProfileId))]
        public TaxProfile? DefaultTaxProfile { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public bool IsActive { get; set; } = true;

        public List<Product> Products { get; set; } = [];
    }
}
