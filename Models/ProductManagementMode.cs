using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    public class ProductManagementMode
    {
        public int Id { get; set; }

        public int ProductVersionId { get; set; }
        [ForeignKey(nameof(ProductVersionId))]
        public ProductVersion ProductVersion { get; set; } = null!;

        public ProductManagementModeType ManagementModeType { get; set; }

        [MaxLength(200)]
        public string? Label { get; set; }

        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableTo { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
