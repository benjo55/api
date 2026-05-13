using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;

namespace api.Models
{
    /// <summary>
    /// Profil fiscal paramétrable par famille de contrat d'assurance financière.
    /// Stocké en base, modifiable par les administrateurs.
    /// </summary>
    public class TaxProfile
    {
        [Key]
        public int Id { get; set; }

        public ContractFamily ContractFamily { get; set; }

        /// <summary>Libellé affiché (ex : "Assurance-vie individuelle")</summary>
        [Required, MaxLength(200)]
        public string Label { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // ─── Entrée ─────────────────────────────────────────────────────────
        /// <summary>Les versements sont-ils déductibles du revenu imposable ?</summary>
        public bool EntryDeductible { get; set; } = false;

        /// <summary>Plafond annuel de déductibilité (null = pas de déduction)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? EntryDeductionCap { get; set; }

        // ─── Seuil de durée ─────────────────────────────────────────────────
        /// <summary>Nombre d'années de détention ouvrant droit au régime fiscal favorable</summary>
        public int DurationThresholdYears { get; set; } = 8;

        // ─── Fiscalité des rachats / retraits ────────────────────────────────
        /// <summary>Taux IR (PFU) applicable avant le seuil de durée</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal IrRateBeforeThreshold { get; set; } = 12.8m;

        /// <summary>Taux IR réduit applicable après le seuil de durée (ex : 7,5 % pour l'AV)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal IrRateAfterThreshold { get; set; } = 12.8m;

        /// <summary>
        /// Plafond de versements totaux (tous contrats confondus) pour bénéficier du taux réduit.
        /// Au-delà, le taux standard s'applique. Null = pas de plafond.
        /// Ex : 150 000 € pour l'assurance-vie.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ContributionCapForReducedRate { get; set; }

        /// <summary>Taux IR au-delà du plafond de versements (ex : 12,8 %)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal IrRateAboveContributionCap { get; set; } = 12.8m;

        // ─── Prélèvements sociaux ────────────────────────────────────────────
        [Column(TypeName = "decimal(5,2)")]
        public decimal SocialChargesRate { get; set; } = 17.2m;

        // ─── Abattements sur les gains ──────────────────────────────────────
        /// <summary>Abattement annuel sur les gains (personne seule). Null = pas d'abattement.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? GainAllowanceSingle { get; set; }

        /// <summary>Abattement annuel sur les gains (couple marié/pacsé). Null = pas d'abattement.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? GainAllowanceCouple { get; set; }

        // ─── Exonérations après seuil ────────────────────────────────────────
        /// <summary>IR exonéré après le seuil de durée (ex : PEA après 5 ans)</summary>
        public bool IrExemptAfterThreshold { get; set; } = false;

        /// <summary>Prélèvements sociaux exonérés après le seuil (rare)</summary>
        public bool SocialChargesExemptAfterThreshold { get; set; } = false;

        // ─── Fiscalité décès – Article 990 I CGI ─────────────────────────────
        /// <summary>Applicable aux versements effectués avant 70 ans du souscripteur</summary>
        public bool HasDeathTaxArticle990I { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Death990I_AllowancePerBeneficiary { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Death990I_Rate1 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Death990I_Rate1Threshold { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Death990I_Rate2 { get; set; }

        // ─── Fiscalité décès – Article 757 B CGI ─────────────────────────────
        /// <summary>Applicable aux versements effectués après 70 ans du souscripteur</summary>
        public bool HasDeathTaxArticle757B { get; set; } = false;

        /// <summary>Abattement global unique sur les versements (pas par bénéficiaire)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Death757B_GlobalAllowance { get; set; }

        // ─── Mode de sortie ──────────────────────────────────────────────────
        public ExitMode ExitMode { get; set; } = ExitMode.Both;

        /// <summary>La rente est-elle imposée comme une pension de retraite (barème IR) ?</summary>
        public bool RenteTaxedAsPension { get; set; } = false;

        /// <summary>
        /// Pour les rentes viagères à titre onéreux : fraction imposable selon l'âge de la 1re rente.
        /// Null = non applicable.
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? RentePartImposable { get; set; }

        // ─── Options barème IR ────────────────────────────────────────────────
        /// <summary>Le contribuable peut-il opter pour le barème progressif à la place du PFU ?</summary>
        public bool CanChooseBareme { get; set; } = true;

        // ─── Succession ──────────────────────────────────────────────────────
        public bool HasSuccessionBenefit { get; set; } = false;

        // ─── Verrouillage ────────────────────────────────────────────────────
        /// <summary>Profil système non modifiable</summary>
        public bool Locked { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
