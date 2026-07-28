using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class ProductEnvelope
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public int ProductCategoryId { get; set; }
        [ForeignKey(nameof(ProductCategoryId))]
        public ProductCategory ProductCategory { get; set; } = null!;

        public int LegalNatureId { get; set; }
        [ForeignKey(nameof(LegalNatureId))]
        public LegalNature LegalNature { get; set; } = null!;

        public int? DefaultTaxProfileId { get; set; }
        [ForeignKey(nameof(DefaultTaxProfileId))]
        public TaxProfile? DefaultTaxProfile { get; set; }

        public bool IsIndividual { get; set; } = true;
        public bool IsCollective { get; set; }
        public bool AllowsMultipleHolders { get; set; }
        public bool RequiresInsuredPerson { get; set; }
        public bool SupportsBeneficiaryClause { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public List<Product> Products { get; set; } = [];
    }
}
