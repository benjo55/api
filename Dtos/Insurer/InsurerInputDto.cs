using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace api.Dtos.Insurer
{
    public class InsurerInputDto : IValidatableObject
    {
        public string Name { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public int FoundedYear { get; set; }
        public string? HeadQuarters { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? PostalAddress { get; set; }
        public string? WebSite { get; set; }
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

        public List<InsurerAuthorizationDto> Authorizations { get; set; } = [];
        public List<InsurerContactPointDto> ContactPoints { get; set; } = [];
        public List<InsurerSolvencyMetricDto> SolvencyMetrics { get; set; } = [];

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public bool Locked { get; set; } = false;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(TradeName) && string.IsNullOrWhiteSpace(LegalName))
            {
                yield return new ValidationResult("Un nom, un nom commercial ou une dénomination sociale est obligatoire.", new[] { nameof(Name), nameof(TradeName), nameof(LegalName) });
            }

            if (FoundedYear != 0 && (FoundedYear < 1800 || FoundedYear > DateTime.UtcNow.Year))
            {
                yield return new ValidationResult("L'année de fondation doit être raisonnable et non future.", new[] { nameof(FoundedYear) });
            }

            foreach (var result in ValidateRegex(Siren, @"^\d{9}$", "Le SIREN doit contenir exactement 9 chiffres.", nameof(Siren))) yield return result;
            foreach (var result in ValidateRegex(HeadquartersSiret, @"^\d{14}$", "Le SIRET doit contenir exactement 14 chiffres.", nameof(HeadquartersSiret))) yield return result;
            foreach (var result in ValidateRegex(Lei, @"^[A-Z0-9]{20}$", "Le LEI doit contenir exactement 20 caractères alphanumériques en majuscules.", nameof(Lei))) yield return result;
            foreach (var result in ValidateRegex(ParentLei, @"^[A-Z0-9]{20}$", "Le LEI parent doit contenir exactement 20 caractères alphanumériques en majuscules.", nameof(ParentLei))) yield return result;
            foreach (var result in ValidateRegex(UltimateParentLei, @"^[A-Z0-9]{20}$", "Le LEI parent ultime doit contenir exactement 20 caractères alphanumériques en majuscules.", nameof(UltimateParentLei))) yield return result;

            foreach (var result in ValidateCountryCode(IncorporationCountryCode, nameof(IncorporationCountryCode))) yield return result;
            foreach (var result in ValidateCountryCode(HomeCountryCode, nameof(HomeCountryCode))) yield return result;
            foreach (var result in ValidateCountryCode(SupervisoryAuthorityCountryCode, nameof(SupervisoryAuthorityCountryCode))) yield return result;

            foreach (var result in ValidateUrl(WebSite, nameof(WebSite))) yield return result;
            foreach (var result in ValidateUrl(OfficialRegistryUrl, nameof(OfficialRegistryUrl))) yield return result;
            foreach (var result in ValidateUrl(GroupWebsiteUrl, nameof(GroupWebsiteUrl))) yield return result;
            foreach (var result in ValidateUrl(ComplaintsProcedureUrl, nameof(ComplaintsProcedureUrl))) yield return result;
            foreach (var result in ValidateUrl(PrivacyPolicyUrl, nameof(PrivacyPolicyUrl))) yield return result;
            foreach (var result in ValidateUrl(RatingSourceUrl, nameof(RatingSourceUrl))) yield return result;
            foreach (var result in ValidateUrl(SourceUrl, nameof(SourceUrl))) yield return result;

            if (!string.IsNullOrWhiteSpace(Email) && !new EmailAddressAttribute().IsValid(Email))
            {
                yield return new ValidationResult("L'email n'est pas valide.", new[] { nameof(Email) });
            }

            if (AuthorizationDate.HasValue && WithdrawalDate.HasValue && AuthorizationDate > WithdrawalDate)
            {
                yield return new ValidationResult("La date d'agrément doit être antérieure au retrait.", new[] { nameof(AuthorizationDate), nameof(WithdrawalDate) });
            }

            if (OwnershipPercentage is < 0 or > 100)
            {
                yield return new ValidationResult("Le pourcentage de détention doit être compris entre 0 et 100.", new[] { nameof(OwnershipPercentage) });
            }

            foreach (var result in ValidatePrimaryContacts(ContactPoints)) yield return result;
            foreach (var result in ValidateChildren()) yield return result;
        }

        private static IEnumerable<ValidationResult> ValidateRegex(string? value, string pattern, string message, string memberName)
        {
            if (!string.IsNullOrWhiteSpace(value) && !Regex.IsMatch(value, pattern))
            {
                yield return new ValidationResult(message, new[] { memberName });
            }
        }

        private static IEnumerable<ValidationResult> ValidateCountryCode(string? value, string memberName)
        {
            foreach (var result in ValidateRegex(value, "^[A-Z]{2}$", "Le code pays doit être un code ISO à deux lettres majuscules.", memberName))
            {
                yield return result;
            }
        }

        private static IEnumerable<ValidationResult> ValidateUrl(string? value, string memberName)
        {
            if (string.IsNullOrWhiteSpace(value)) yield break;

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                yield return new ValidationResult("L'URL doit être une URL HTTP ou HTTPS valide.", new[] { memberName });
            }
        }

        private static IEnumerable<ValidationResult> ValidatePrimaryContacts(IEnumerable<InsurerContactPointDto> contactPoints)
        {
            var duplicates = contactPoints
                .Where(c => c.IsPrimary && !string.IsNullOrWhiteSpace(c.ContactType))
                .GroupBy(c => c.ContactType)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                yield return new ValidationResult($"Un seul contact primaire est autorisé par type : {string.Join(", ", duplicates)}.", new[] { nameof(ContactPoints) });
            }
        }

        private IEnumerable<ValidationResult> ValidateChildren()
        {
            for (var i = 0; i < Authorizations.Count; i++)
            {
                foreach (var result in ValidateCountryCode(Authorizations[i].AuthorityCountryCode, $"{nameof(Authorizations)}[{i}].{nameof(InsurerAuthorizationDto.AuthorityCountryCode)}")) yield return result;
                foreach (var result in ValidateCountryCode(Authorizations[i].HostCountryCode, $"{nameof(Authorizations)}[{i}].{nameof(InsurerAuthorizationDto.HostCountryCode)}")) yield return result;
                foreach (var result in ValidateUrl(Authorizations[i].SourceUrl, $"{nameof(Authorizations)}[{i}].{nameof(InsurerAuthorizationDto.SourceUrl)}")) yield return result;
            }

            for (var i = 0; i < ContactPoints.Count; i++)
            {
                foreach (var result in ValidateCountryCode(ContactPoints[i].CountryCode, $"{nameof(ContactPoints)}[{i}].{nameof(InsurerContactPointDto.CountryCode)}")) yield return result;
                foreach (var result in ValidateUrl(ContactPoints[i].WebsiteUrl, $"{nameof(ContactPoints)}[{i}].{nameof(InsurerContactPointDto.WebsiteUrl)}")) yield return result;
                foreach (var result in ValidateUrl(ContactPoints[i].SourceUrl, $"{nameof(ContactPoints)}[{i}].{nameof(InsurerContactPointDto.SourceUrl)}")) yield return result;
                if (!string.IsNullOrWhiteSpace(ContactPoints[i].Email) && !new EmailAddressAttribute().IsValid(ContactPoints[i].Email))
                {
                    yield return new ValidationResult("L'email du contact n'est pas valide.", new[] { $"{nameof(ContactPoints)}[{i}].{nameof(InsurerContactPointDto.Email)}" });
                }
            }

            for (var i = 0; i < SolvencyMetrics.Count; i++)
            {
                foreach (var result in ValidateUrl(SolvencyMetrics[i].SfcrDocumentUrl, $"{nameof(SolvencyMetrics)}[{i}].{nameof(InsurerSolvencyMetricDto.SfcrDocumentUrl)}")) yield return result;
                foreach (var result in ValidateUrl(SolvencyMetrics[i].SourceUrl, $"{nameof(SolvencyMetrics)}[{i}].{nameof(InsurerSolvencyMetricDto.SourceUrl)}")) yield return result;
                if (SolvencyMetrics[i].ScrCoverageRatio is < 0)
                {
                    yield return new ValidationResult("Le ratio SCR doit être positif.", new[] { $"{nameof(SolvencyMetrics)}[{i}].{nameof(InsurerSolvencyMetricDto.ScrCoverageRatio)}" });
                }
                if (SolvencyMetrics[i].McrCoverageRatio is < 0)
                {
                    yield return new ValidationResult("Le ratio MCR doit être positif.", new[] { $"{nameof(SolvencyMetrics)}[{i}].{nameof(InsurerSolvencyMetricDto.McrCoverageRatio)}" });
                }
            }
        }
    }

    public class InsurerAuthorizationDto
    {
        public int Id { get; set; }
        public int InsurerId { get; set; }
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

    public class InsurerContactPointDto
    {
        public int Id { get; set; }
        public int InsurerId { get; set; }
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

    public class InsurerSolvencyMetricDto
    {
        public int Id { get; set; }
        public int InsurerId { get; set; }
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
