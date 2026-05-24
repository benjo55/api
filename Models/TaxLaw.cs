using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    public class TaxLaw
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Label { get; set; } = string.Empty;

        [MaxLength(100)]
        public string CountryCode { get; set; } = "FR";

        public DateTime EffectiveDateStart { get; set; }
        public DateTime? EffectiveDateEnd { get; set; }

        [MaxLength(200)]
        public string? LawReference { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
