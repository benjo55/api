using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    /// <summary>
    /// Version de référentiel fiscal applicable à une période.
    /// Permet de conserver l'auditabilité des calculs dans le temps.
    /// </summary>
    public class TaxRuleVersion
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Label { get; set; } = string.Empty;

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
