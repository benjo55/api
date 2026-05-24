using api.Models.Enum;

namespace api.Dtos.TaxProfile
{
    public enum FiscalEventType
    {
        PartialWithdrawal = 0,
        FullWithdrawal = 1,
        Arbitrage = 2,
        Advance = 3,
        ProgrammedWithdrawal = 4,
        AnnuityConversion = 5,
        Death = 6,
    }

    public enum TaxResidency
    {
        France = 0,
        Eee = 1,
        NonEee = 2,
    }

    public enum PerCompartmentType
    {
        VoluntaryDeducted = 0,
        VoluntaryNonDeducted = 1,
        EmployeeSavings = 2,
        Mandatory = 3,
    }

    public enum BeneficiaryRelation
    {
        Unknown = 0,
        Spouse = 1,
        PacsPartner = 2,
        Child = 3,
        Parent = 4,
        Other = 5,
        LegalEntity = 6,
    }

    public class TaxProfileDto
    {
        public int Id { get; set; }
        public ContractFamily ContractFamily { get; set; }
        public string ContractFamilyLabel { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Entrée
        public bool EntryDeductible { get; set; }
        public decimal? EntryDeductionCap { get; set; }

        // Seuil durée
        public int DurationThresholdYears { get; set; }

        // IR rachats
        public decimal IrRateBeforeThreshold { get; set; }
        public decimal IrRateAfterThreshold { get; set; }
        public decimal? ContributionCapForReducedRate { get; set; }
        public decimal IrRateAboveContributionCap { get; set; }

        // PS
        public decimal SocialChargesRate { get; set; }

        // Abattements
        public decimal? GainAllowanceSingle { get; set; }
        public decimal? GainAllowanceCouple { get; set; }

        // Exonérations
        public bool IrExemptAfterThreshold { get; set; }
        public bool SocialChargesExemptAfterThreshold { get; set; }

        // Décès 990 I
        public bool HasDeathTaxArticle990I { get; set; }
        public decimal? Death990I_AllowancePerBeneficiary { get; set; }
        public decimal? Death990I_Rate1 { get; set; }
        public decimal? Death990I_Rate1Threshold { get; set; }
        public decimal? Death990I_Rate2 { get; set; }

        // Décès 757 B
        public bool HasDeathTaxArticle757B { get; set; }
        public decimal? Death757B_GlobalAllowance { get; set; }

        // Sortie
        public ExitMode ExitMode { get; set; }
        public bool RenteTaxedAsPension { get; set; }
        public decimal? RentePartImposable { get; set; }

        // Options
        public bool CanChooseBareme { get; set; }
        public bool HasSuccessionBenefit { get; set; }
        public bool Locked { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class CreateTaxProfileDto
    {
        public ContractFamily ContractFamily { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool EntryDeductible { get; set; } = false;
        public decimal? EntryDeductionCap { get; set; }
        public int DurationThresholdYears { get; set; } = 8;
        public decimal IrRateBeforeThreshold { get; set; } = 12.8m;
        public decimal IrRateAfterThreshold { get; set; } = 12.8m;
        public decimal? ContributionCapForReducedRate { get; set; }
        public decimal IrRateAboveContributionCap { get; set; } = 12.8m;
        public decimal SocialChargesRate { get; set; } = 17.2m;
        public decimal? GainAllowanceSingle { get; set; }
        public decimal? GainAllowanceCouple { get; set; }
        public bool IrExemptAfterThreshold { get; set; } = false;
        public bool SocialChargesExemptAfterThreshold { get; set; } = false;
        public bool HasDeathTaxArticle990I { get; set; } = false;
        public decimal? Death990I_AllowancePerBeneficiary { get; set; }
        public decimal? Death990I_Rate1 { get; set; }
        public decimal? Death990I_Rate1Threshold { get; set; }
        public decimal? Death990I_Rate2 { get; set; }
        public bool HasDeathTaxArticle757B { get; set; } = false;
        public decimal? Death757B_GlobalAllowance { get; set; }
        public ExitMode ExitMode { get; set; } = ExitMode.Both;
        public bool RenteTaxedAsPension { get; set; } = false;
        public decimal? RentePartImposable { get; set; }
        public bool CanChooseBareme { get; set; } = true;
        public bool HasSuccessionBenefit { get; set; } = false;
    }

    public class UpdateTaxProfileDto : CreateTaxProfileDto { }

    // ─── Simulation ──────────────────────────────────────────────────────────

    public class TaxSimulationRequest
    {
        /// <summary>Id du profil fiscal à utiliser</summary>
        public int TaxProfileId { get; set; }

        /// <summary>Contrat ciblé pour charger l'état fiscal historisé en base</summary>
        public int? ContractId { get; set; }

        /// <summary>Date de calcul/rejeu fiscal</summary>
        public DateTime? CalculationDate { get; set; }

        /// <summary>Durée de détention du contrat en années</summary>
        public int ContractDurationYears { get; set; }

        /// <summary>Montant brut du rachat / capital</summary>
        public decimal GrossWithdrawal { get; set; }

        /// <summary>Valeur contrat au jour du calcul (si absente, valeur issue de l'état historisé)</summary>
        public decimal? ContractValue { get; set; }

        /// <summary>Primes nettes cumulées (si absentes, valeur issue de l'état historisé)</summary>
        public decimal? NetPremiums { get; set; }

        /// <summary>Gains inclus dans le rachat (plusvalue)</summary>
        public decimal GainAmount { get; set; }

        /// <summary>Total des versements effectués sur l'ensemble des contrats du contribuable (pour le seuil 150k€)</summary>
        public decimal TotalContributionsAllContracts { get; set; }

        /// <summary>Le souscripteur est-il en couple marié ou pacsé ?</summary>
        public bool IsCouple { get; set; } = false;

        /// <summary>Mode de sortie retenu pour la simulation</summary>
        public ExitMode ExitMode { get; set; } = ExitMode.Capital;

        /// <summary>Type d'événement fiscal simulé</summary>
        public FiscalEventType EventType { get; set; } = FiscalEventType.PartialWithdrawal;

        /// <summary>Option barème progressif IR au lieu PFU (si autorisé par le profil)</summary>
        public bool ApplyProgressiveScale { get; set; } = false;

        /// <summary>Taux marginal IR utilisé pour la simulation barème progressif</summary>
        public decimal? ProgressiveScaleRate { get; set; }

        /// <summary>Résidence fiscale du souscripteur</summary>
        public TaxResidency Residency { get; set; } = TaxResidency.France;

        /// <summary>Alias métier explicite pour marquer une situation non-résident</summary>
        public bool IsNonResidentFiscal { get; set; } = false;

        /// <summary>Forçage explicite du mode avance (non fiscalisé)</summary>
        public bool IsAdvance { get; set; } = false;

        /// <summary>Exonération des prélèvements sociaux selon convention UE/EEE</summary>
        public bool SocialChargesExemptByTreaty { get; set; } = false;

        /// <summary>PS déjà prélevés en amont (ex : fonds euros) à neutraliser du calcul du retrait</summary>
        public decimal AlreadyPaidSocialCharges { get; set; } = 0m;

        /// <summary>Active explicitement le calcul par strates temporelles</summary>
        public bool UseTemporalLots { get; set; } = false;

        /// <summary>Rente viagère à titre onéreux : âge au premier versement pour la fraction imposable</summary>
        public int? AgeAtFirstAnnuityPayment { get; set; }

        // ─── PER multi-compartiments ────────────────────────────────────────
        public List<PerCompartmentInput> PerCompartments { get; set; } = [];

        // ─── Pour les décès ──────────────────────────────────────────────────
        /// <summary>Versements effectués avant 70 ans du souscripteur</summary>
        public decimal ContributionsBefore70 { get; set; }

        /// <summary>Versements effectués après 70 ans</summary>
        public decimal ContributionsAfter70 { get; set; }

        /// <summary>Nombre de bénéficiaires désignés</summary>
        public int BeneficiaryCount { get; set; } = 1;

        /// <summary>Capital décès total transmis (brut)</summary>
        public decimal DeathCapital { get; set; }

        /// <summary>Ventilation des bénéficiaires pour simulation décès avancée</summary>
        public List<BeneficiaryAllocationInput> Beneficiaries { get; set; } = [];

        /// <summary>Clause bénéficiaire démembrée (usufruit / nue-propriété)</summary>
        public bool IsDismemberedClause { get; set; } = false;

        /// <summary>Quote-part usufruit économique en pourcentage (sinon déterminée par barème d'âge)</summary>
        public decimal? UsufructSharePercent { get; set; }

        /// <summary>Âge de l'usufruitier au décès pour barème fiscal simplifié</summary>
        public int? UsufructuaryAgeAtDeath { get; set; }

        /// <summary>Lots de primes injectés côté requête (simulation hors base)</summary>
        public List<PremiumLotInput> PremiumLots { get; set; } = [];

        /// <summary>Lots de gains injectés côté requête (simulation hors base)</summary>
        public List<GainLotInput> GainLots { get; set; } = [];

        /// <summary>Historique de PS déjà payés injecté côté requête (simulation hors base)</summary>
        public List<PsPaidInput> PsPaidHistory { get; set; } = [];
    }

    public class PremiumLotInput
    {
        public DateTime PaymentDate { get; set; }
        public decimal GrossPremium { get; set; }
        public decimal NetPremium { get; set; }
        public decimal RemainingNetPremium { get; set; }
        public int? TaxGenerationId { get; set; }
        public decimal? TaxedGainShareRate { get; set; }
    }

    public class GainLotInput
    {
        public DateTime GainDate { get; set; }
        public decimal GainAmount { get; set; }
        public decimal RemainingGainAmount { get; set; }
        public decimal SocialChargesAlreadyPaid { get; set; }
        public decimal ApplicableSocialRate { get; set; }
        public int? TaxGenerationId { get; set; }
    }

    public class PsPaidInput
    {
        public DateTime LevyDate { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal AppliedRate { get; set; }
    }

    public class PerCompartmentInput
    {
        public PerCompartmentType Type { get; set; }
        public decimal CapitalAmount { get; set; }
        public decimal GainAmount { get; set; }
    }

    public class BeneficiaryAllocationInput
    {
        public string? Name { get; set; }
        public BeneficiaryRelation Relation { get; set; } = BeneficiaryRelation.Unknown;
        public decimal SharePercent { get; set; }
        public bool IsExempt { get; set; } = false;
        public bool IsUsufructuary { get; set; } = false;
    }

    public class TaxSimulationResult
    {
        public int TaxProfileId { get; set; }
        public string ProfileLabel { get; set; } = string.Empty;
        public ContractFamily ContractFamily { get; set; }
        public bool IsAfterDurationThreshold { get; set; }
        public int? AppliedTaxRuleVersionId { get; set; }
        public int? TaxComputationId { get; set; }
        public FiscalEventType EventType { get; set; }

        // ─── Rachat / Capital ────────────────────────────────────────────────
        public WithdrawalTaxDetail? WithdrawalTax { get; set; }

        // ─── Rente ───────────────────────────────────────────────────────────
        public RenteTaxDetail? RenteTax { get; set; }

        // ─── Décès ───────────────────────────────────────────────────────────
        public DeathTaxDetail? DeathTax { get; set; }

        // ─── Résumé ──────────────────────────────────────────────────────────
        public string[] Notes { get; set; } = [];
        public string[] Warnings { get; set; } = [];
    }

    public class TaxComputationDto
    {
        public int Id { get; set; }
        public int TaxProfileId { get; set; }
        public int? TaxRuleVersionId { get; set; }
        public FiscalEventType EventType { get; set; }
        public decimal GrossWithdrawal { get; set; }
        public decimal GainAmount { get; set; }
        public decimal? TotalTax { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class WithdrawalTaxDetail
    {
        public decimal GrossGain { get; set; }
        public decimal GainAllowanceApplied { get; set; }
        public decimal NetTaxableGain { get; set; }

        public decimal IrRate { get; set; }
        public decimal IrAmount { get; set; }

        public decimal SocialChargesRate { get; set; }
        public decimal SocialChargesAmount { get; set; }

        public decimal TotalTax { get; set; }
        public decimal NetWithdrawal { get; set; }

        /// <summary>Taux effectif global sur le montant brut retiré</summary>
        public decimal EffectiveTaxRate { get; set; }

        public string[] Breakdown { get; set; } = [];
        public PerCompartmentTaxDetail[] PerCompartmentTaxes { get; set; } = [];
        public TaxGenerationBreakdown[] TaxGenerationBreakdowns { get; set; } = [];
    }

    public class TaxGenerationBreakdown
    {
        public int? TaxGenerationId { get; set; }
        public string GenerationCode { get; set; } = string.Empty;
        public decimal AllocatedTaxableGain { get; set; }
        public decimal IrRate { get; set; }
        public decimal IrAmount { get; set; }
        public decimal SocialRate { get; set; }
        public decimal SocialAmount { get; set; }
        public decimal SocialAlreadyPaid { get; set; }
        public decimal SocialRemainingDue { get; set; }
        public string[] Notes { get; set; } = [];
    }

    public class PerCompartmentTaxDetail
    {
        public PerCompartmentType Type { get; set; }
        public decimal CapitalTaxable { get; set; }
        public decimal GainTaxable { get; set; }
        public decimal IrAmount { get; set; }
        public decimal SocialChargesAmount { get; set; }
        public decimal TotalTax { get; set; }
        public string[] Notes { get; set; } = [];
    }

    public class RenteTaxDetail
    {
        public decimal? PartImposable { get; set; }
        public bool TaxedAsPension { get; set; }
        public string[] Notes { get; set; } = [];
    }

    public class DeathTaxDetail
    {
        // Art. 990 I
        public decimal? Article990I_TaxableBase { get; set; }
        public decimal? Article990I_AllowanceTotal { get; set; }
        public decimal? Article990I_TaxAmount { get; set; }

        // Art. 757 B
        public decimal? Article757B_TaxableContributions { get; set; }
        public decimal? Article757B_AllowanceApplied { get; set; }
        public decimal? Article757B_TaxableAfterAllowance { get; set; }

        public BeneficiaryTaxAllocationDetail[] BeneficiaryTaxAllocations { get; set; } = [];
        public decimal? AppliedUsufructSharePercent { get; set; }

        public string[] Notes { get; set; } = [];
    }

    public class BeneficiaryTaxAllocationDetail
    {
        public string BeneficiaryLabel { get; set; } = string.Empty;
        public BeneficiaryRelation Relation { get; set; }
        public bool IsExempt { get; set; }
        public decimal AllocationPercent { get; set; }
        public decimal BaseBefore70 { get; set; }
        public decimal BaseAfter70 { get; set; }
        public decimal Allowance990I { get; set; }
        public decimal Taxable990I { get; set; }
        public decimal Tax990I { get; set; }
        public decimal Allowance757BShare { get; set; }
        public decimal Taxable757B { get; set; }
        public string[] Notes { get; set; } = [];
    }
}
