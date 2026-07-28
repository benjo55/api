using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public class ProductDocument
    {
        public int Id { get; set; }

        public int ProductVersionId { get; set; }
        [ForeignKey(nameof(ProductVersionId))]
        public ProductVersion ProductVersion { get; set; } = null!;

        public ProductDocumentType DocumentType { get; set; }

        [Required, MaxLength(300)]
        public string DocumentName { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string StorageReference { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Version { get; set; }

        public DateTime? PublicationDate { get; set; }

        public bool IsMandatory { get; set; }
        public bool IsCurrent { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
