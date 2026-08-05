using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class Insurer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public int FoundedYear { get; set; }
        public string? HeadQuarters { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? WebSite { get; set; }
        public string? PostalAddress { get; set; }
        public string? IsActive { get; set; }

        public string? LegalName { get; set; }
        public string? TradeName { get; set; }
        public string? Acronym { get; set; }
        public string? FormerNamesJson { get; set; }
        public string? InternalCode { get; set; }
        public string? LegalForm { get; set; }
        public string? InsurerType { get; set; }
        public string? IncorporationCountryCode { get; set; }
        public DateTime? IncorporationDate { get; set; }
        public string? Siren { get; set; }
        public string? HeadquartersSiret { get; set; }
        public string? RcsCity { get; set; }
        public string? RcsNumber { get; set; }
        public string? VatNumber { get; set; }
        public string? Lei { get; set; }
        public string? ApeNafCode { get; set; }
        public string? OfficialRegistryUrl { get; set; }

        public string? HomeCountryCode { get; set; }
        public string? SupervisoryAuthorityName { get; set; }
        public string? SupervisoryAuthorityCountryCode { get; set; }
        public string? SupervisoryRegisterName { get; set; }
        public string? SupervisoryRegisterId { get; set; }
        public string? EiopaRegisterId { get; set; }
        public string? ExerciseRegime { get; set; }
        public string? RegulatoryStatus { get; set; }
        public DateTime? AuthorizationDate { get; set; }
        public DateTime? SuspensionDate { get; set; }
        public DateTime? WithdrawalDate { get; set; }
        public DateTime? ActivityEndDate { get; set; }
        public bool? IsSubjectToSolvencyII { get; set; }
        public bool? IsLifeInsurer { get; set; }
        public bool? IsNonLifeInsurer { get; set; }
        public bool? IsReinsurer { get; set; }
        public string? RegulatoryNotes { get; set; }

        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public string? History { get; set; }
        public string? Mission { get; set; }
        public string? MainActivitiesJson { get; set; }
        public string? CustomerSegmentsJson { get; set; }
        public string? DistributionChannelsJson { get; set; }
        public string? GeographicCoverage { get; set; }
        public string? ProductSpecialtiesJson { get; set; }
        public string? KeyFacts { get; set; }
        public int? EmployeeCount { get; set; }
        public int? CustomerCount { get; set; }
        public decimal? AssetsUnderManagement { get; set; }
        public int? FinancialDataYear { get; set; }

        public string? GroupName { get; set; }
        public string? ParentLegalEntityName { get; set; }
        public string? ParentLei { get; set; }
        public string? UltimateParentName { get; set; }
        public string? UltimateParentLei { get; set; }
        public decimal? OwnershipPercentage { get; set; }
        public bool? IsGroupHead { get; set; }
        public string? GroupWebsiteUrl { get; set; }

        public string? ComplaintsProcedureUrl { get; set; }
        public string? PrivacyPolicyUrl { get; set; }
        public string? RatingAgency { get; set; }
        public string? Rating { get; set; }
        public string? RatingOutlook { get; set; }
        public DateTime? RatingDate { get; set; }
        public string? RatingSourceUrl { get; set; }

        public string? DataSourceType { get; set; }
        public string? SourceName { get; set; }
        public string? SourceUrl { get; set; }
        public string? SourceReference { get; set; }
        public DateTime? RetrievedAt { get; set; }
        public DateTime? LastVerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? VerificationStatus { get; set; }
        public string? DataQualityNotes { get; set; }

        public ICollection<InsurerAuthorization> Authorizations { get; set; } = new List<InsurerAuthorization>();
        public ICollection<InsurerContactPoint> ContactPoints { get; set; } = new List<InsurerContactPoint>();
        public ICollection<InsurerSolvencyMetric> SolvencyMetrics { get; set; } = new List<InsurerSolvencyMetric>();

        [NotMapped]
        public int ProductCount { get; set; }
        [NotMapped]
        public int ContractCount { get; set; }
        [NotMapped]
        public int BrandCount { get; set; }
        [NotMapped]
        public int DocumentCount { get; set; }
        [NotMapped]
        public int PersonCount { get; set; }
        [NotMapped]
        public int AuthorizationCount { get; set; }
        [NotMapped]
        public int ExerciseCountryCount { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public bool Locked { get; set; } = false; // Default to false, can be set to true when updating an insurer
    }

    public class InsurerAuthorization
    {
        public int Id { get; set; }
        public int InsurerId { get; set; }
        public Insurer? Insurer { get; set; }
        public string? AuthorityName { get; set; }
        public string? AuthorityCountryCode { get; set; }
        public string? RegisterName { get; set; }
        public string? RegisterReference { get; set; }
        public string? AuthorizationType { get; set; }
        public string? InsuranceBranchCode { get; set; }
        public string? InsuranceBranchLabel { get; set; }
        public string? BusinessCategory { get; set; }
        public string? HostCountryCode { get; set; }
        public string? ExerciseRegime { get; set; }
        public string? Status { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? SourceUrl { get; set; }
        public DateTime? LastVerifiedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class InsurerContactPoint
    {
        public int Id { get; set; }
        public int InsurerId { get; set; }
        public Insurer? Insurer { get; set; }
        public string? ContactType { get; set; }
        public string? Label { get; set; }
        public string? DepartmentName { get; set; }
        public string? ContactName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? CountryCode { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? OpeningHours { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? SourceUrl { get; set; }
        public DateTime? LastVerifiedAt { get; set; }
    }

    public class InsurerSolvencyMetric
    {
        public int Id { get; set; }
        public int InsurerId { get; set; }
        public Insurer? Insurer { get; set; }
        public int ReportingYear { get; set; }
        public DateTime? ReportingDate { get; set; }
        public DateTime? SfcrPublicationDate { get; set; }
        public string? SfcrDocumentUrl { get; set; }
        public decimal? EligibleOwnFunds { get; set; }
        public decimal? SolvencyCapitalRequirement { get; set; }
        public decimal? ScrCoverageRatio { get; set; }
        public decimal? MinimumCapitalRequirement { get; set; }
        public decimal? McrCoverageRatio { get; set; }
        public string? Currency { get; set; }
        public bool IsGroupReport { get; set; }
        public string? SourceUrl { get; set; }
        public DateTime? LastVerifiedAt { get; set; }
        public string? Notes { get; set; }
    }
}
