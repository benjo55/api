using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class BeneficiaryClausePerson
    {
        // 🔹 Référence à la Clause Bénéficiaire
        [Required]
        [ForeignKey("BeneficiaryClause")]
        public int ClauseId { get; set; }
        public BeneficiaryClause? BeneficiaryClause { get; set; } = null!;

        // 🔹 Référence à la Personne bénéficiaire
        [Required]
        [ForeignKey("Person")]
        public int PersonId { get; set; }
        public Person? Person { get; set; } = null!;

        // 🔹 Définition de la relation avec le souscripteur
        [Required]
        public string RelationWithClause { get; set; } = string.Empty;

        // 🔹 Pourcentage attribué dans la clause (avec précision)
        [Required]
        [Column(TypeName = "decimal(5,2)")] // ✅ Fixe la précision SQL Server
        public decimal Percentage { get; set; }

        // 🔹 Date de création de l'entité
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        // 🔹 Date de dernière modification de l'entité
        [Required]
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}
