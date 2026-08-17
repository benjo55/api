using api.Data;
using api.Dtos.Cmdb;
using api.Dtos.Generic;
using api.Helpers;
using api.Models.Cmdb;
using api.Security;
using api.Services.Cmdb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/configurationItems")]
[Authorize(Policy = AuthorizationPolicies.UrbanisationAccess)]
public sealed class ConfigurationItemsController : ControllerBase
{
    private readonly ApplicationDBContext _db;
    private readonly ICmdbImportService _importService;

    public ConfigurationItemsController(ApplicationDBContext db, ICmdbImportService importService)
    {
        _db = db;
        _importService = importService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ConfigurationItemListDto>>> GetAll(
        [FromQuery] QueryObject query,
        [FromQuery] string? model,
        [FromQuery] string? category,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var items = _db.ConfigurationItems.AsNoTracking().Where(x => x.IsCurrent);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(x =>
                x.Name.Contains(search) ||
                x.ExternalCiNumber.Contains(search) ||
                (x.Label != null && x.Label.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(model)) items = items.Where(x => x.Model == model);
        if (!string.IsNullOrWhiteSpace(category)) items = items.Where(x => x.Category == category);
        if (!string.IsNullOrWhiteSpace(status)) items = items.Where(x => x.Status == status);

        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var pageNumber = Math.Max(query.PageNumber, 1);
        var totalCount = await items.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = await items
            .OrderBy(x => x.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ConfigurationItemListDto
            {
                Id = x.Id,
                ExternalCiNumber = x.ExternalCiNumber,
                Name = x.Name,
                Label = x.Label,
                Model = x.Model,
                Category = x.Category,
                Status = x.Status,
                ApplicationDomain = x.ApplicationDomain,
                EntityPath = x.EntityPath,
                ResponsibleEmployer = x.ResponsibleEmployer,
                IsPlaceholder = x.IsPlaceholder,
                IsCurrent = x.IsCurrent,
                Locked = x.Locked,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate,
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<ConfigurationItemListDto>
        {
            Items = page,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            CurrentPage = pageNumber,
        });
    }

    [HttpGet("typeahead")]
    public async Task<ActionResult<List<ConfigurationItemListDto>>> Typeahead(
        [FromQuery] string? search,
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var items = _db.ConfigurationItems.AsNoTracking().Where(x => x.IsCurrent);
        var searchValue = string.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchValue = search.Trim();
            items = items.Where(x =>
                x.Name.Contains(searchValue) ||
                x.ExternalCiNumber.Contains(searchValue) ||
                (x.Label != null && x.Label.Contains(searchValue)));
        }

        return Ok(await items
            .OrderBy(x =>
                x.Category == "Application Métier"
                    ? 0
                    : x.Model == "Application" ||
                      (x.Category != null && x.Category.StartsWith("Application"))
                        ? 1
                        : 2)
            .ThenBy(x =>
                searchValue != "" && x.Name == searchValue
                    ? 0
                    : searchValue != "" && x.Name.StartsWith(searchValue)
                        ? 1
                        : searchValue != "" && x.Name.Contains(searchValue)
                            ? 2
                            : searchValue != "" && x.ExternalCiNumber.StartsWith(searchValue)
                                ? 3
                                : 4)
            .ThenBy(x => x.Name)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(x => new ConfigurationItemListDto
            {
                Id = x.Id,
                ExternalCiNumber = x.ExternalCiNumber,
                Name = x.Name,
                Label = x.Label,
                Model = x.Model,
                Category = x.Category,
                Status = x.Status,
                ApplicationDomain = x.ApplicationDomain,
                EntityPath = x.EntityPath,
                ResponsibleEmployer = x.ResponsibleEmployer,
                IsPlaceholder = x.IsPlaceholder,
                IsCurrent = x.IsCurrent,
                Locked = x.Locked,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate,
            })
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ConfigurationItemDetailDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var item = await ProjectDetails(_db.ConfigurationItems.AsNoTracking().Where(x => x.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ConfigurationItemDetailDto>> UpdateEnrichment(
        int id,
        ConfigurationItemEnrichmentWriteDto dto,
        CancellationToken cancellationToken)
    {
        var item = await _db.ConfigurationItems
            .Include(x => x.ApplicationProfile)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();
        if (!item.IsCurrent) return Conflict("Ce CI n'est plus actif dans le dernier import CMDB.");

        var validationError = Validate(dto.ApplicationProfile);
        if (validationError is not null) return BadRequest(validationError);

        if (item.ApplicationProfile is null)
        {
            item.ApplicationProfile = new ConfigurationItemApplicationProfile
            {
                ConfigurationItemId = item.Id,
                CreatedDate = DateTime.UtcNow,
            };
        }

        Apply(dto.ApplicationProfile, item.ApplicationProfile);
        item.ApplicationProfile.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(await ProjectDetails(
                _db.ConfigurationItems.AsNoTracking().Where(x => x.Id == id))
            .SingleAsync(cancellationToken));
    }

    [HttpPost("import")]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<CmdbImportResultDto>> Import(
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            return BadRequest("Sélectionnez les quatre exports CMDB cœur.");
        }
        return Ok(await _importService.ImportAsync(files, cancellationToken));
    }

    private static string? Validate(ConfigurationItemApplicationProfileWriteDto profile)
    {
        if (!IsAllowed(profile.ApplicationNature,
                "InternalDevelopment", "IntegratedPackage", "SaaS", "ItForIt"))
            return "La nature de l'application est invalide.";
        if (!IsAllowed(profile.ApplicationCriticality,
                "Low", "Medium", "High", "Critical"))
            return "La criticité applicative est invalide.";
        if (!IsAllowed(profile.HostingMode, "OnPremise", "Cloud", "Hybrid"))
            return "Le mode d'hébergement est invalide.";
        if (!IsAllowed(profile.CloudServiceModel, "SaaS", "IaaS", "PaaS"))
            return "Le modèle de service cloud est invalide.";
        if (!IsAllowed(profile.AuthenticationMode, "IAM", "Standalone", "Hybrid"))
            return "Le mode d'authentification est invalide.";
        if (!IsAllowed(profile.LastRestorationTestResult,
                "Success", "MinorIssues", "MajorIssues"))
            return "Le résultat du test de restauration est invalide.";
        if (!IsAllowed(profile.PersonalDataPseudonymization,
                "Yes", "No", "NotApplicable"))
            return "La valeur de pseudonymisation est invalide.";

        if (!IsPercentage(profile.LastAccessRemediationPercentage) ||
            !IsPercentage(profile.PreviousAccessRemediationPercentage))
            return "Les pourcentages de remédiation doivent être compris entre 0 et 100.";

        var counts = new int?[]
        {
            profile.InternalTechnicalAdminCount,
            profile.ExternalTechnicalAdminCount,
            profile.OpenRecommendationsLow,
            profile.OpenRecommendationsMedium,
            profile.OpenRecommendationsHigh,
            profile.OverdueRecommendationsLow,
            profile.OverdueRecommendationsMedium,
            profile.OverdueRecommendationsHigh,
            profile.PendingTestActionsCount,
        };
        if (counts.Any(x => x < 0)) return "Les nombres saisis ne peuvent pas être négatifs.";

        if (profile.LastAccessRecertificationDate.HasValue &&
            profile.PreviousAccessRecertificationDate >
            profile.LastAccessRecertificationDate)
            return "L'avant-dernière recertification ne peut pas être postérieure à la dernière.";
        if (profile.LastPentestDate.HasValue &&
            profile.PreviousPentestDate > profile.LastPentestDate)
            return "L'avant-dernier pentest ne peut pas être postérieur au dernier.";
        if (profile.LastFailoverTestDate.HasValue &&
            profile.PreviousFailoverTestDate > profile.LastFailoverTestDate)
            return "L'avant-dernier test de bascule ne peut pas être postérieur au dernier.";

        return null;
    }

    private static bool IsAllowed(string? value, params string[] values) =>
        string.IsNullOrWhiteSpace(value) ||
        values.Contains(value, StringComparer.OrdinalIgnoreCase);

    private static bool IsPercentage(decimal? value) =>
        !value.HasValue || value is >= 0 and <= 100;

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Apply(
        ConfigurationItemApplicationProfileWriteDto source,
        ConfigurationItemApplicationProfile target)
    {
        target.ShortDescription = TrimOrNull(source.ShortDescription);
        target.DetailedDescription = TrimOrNull(source.DetailedDescription);
        target.MainFunctionalProcesses = TrimOrNull(source.MainFunctionalProcesses);
        target.GeneralTechnicalFramework = TrimOrNull(source.GeneralTechnicalFramework);
        target.OverallArchitecture = TrimOrNull(source.OverallArchitecture);
        target.ApplicationCriticality = TrimOrNull(source.ApplicationCriticality);
        target.ApplicationNature = TrimOrNull(source.ApplicationNature);
        target.InternetExposed = source.InternetExposed;
        target.LegalOwnerEntity = TrimOrNull(source.LegalOwnerEntity);
        target.OtherStakeholders = TrimOrNull(source.OtherStakeholders);
        target.SourceCodeAvailable = source.SourceCodeAvailable;
        target.HostingMode = TrimOrNull(source.HostingMode);
        target.HostingProvider = TrimOrNull(source.HostingProvider);
        target.CloudServiceModel = TrimOrNull(source.CloudServiceModel);
        target.HostingNetworkZone = TrimOrNull(source.HostingNetworkZone);
        target.AuthenticationMode = TrimOrNull(source.AuthenticationMode);
        target.IamSolution = TrimOrNull(source.IamSolution);
        target.StandalonePasswordRules = TrimOrNull(source.StandalonePasswordRules);
        target.MfaEnabled = source.MfaEnabled;
        target.InternalTechnicalAdminCount = source.InternalTechnicalAdminCount;
        target.ExternalTechnicalAdminCount = source.ExternalTechnicalAdminCount;
        target.LastAccessRecertificationDate = source.LastAccessRecertificationDate;
        target.LastAccessRemediationPercentage = source.LastAccessRemediationPercentage;
        target.PreviousAccessRecertificationDate = source.PreviousAccessRecertificationDate;
        target.PreviousAccessRemediationPercentage = source.PreviousAccessRemediationPercentage;
        target.CodeScanEnabled = source.CodeScanEnabled;
        target.LastPentestDate = source.LastPentestDate;
        target.PreviousPentestDate = source.PreviousPentestDate;
        target.LastRedTeamDate = source.LastRedTeamDate;
        target.LastBugBountyDate = source.LastBugBountyDate;
        target.OpenRecommendationsLow = source.OpenRecommendationsLow;
        target.OpenRecommendationsMedium = source.OpenRecommendationsMedium;
        target.OpenRecommendationsHigh = source.OpenRecommendationsHigh;
        target.OverdueRecommendationsLow = source.OverdueRecommendationsLow;
        target.OverdueRecommendationsMedium = source.OverdueRecommendationsMedium;
        target.OverdueRecommendationsHigh = source.OverdueRecommendationsHigh;
        target.SecurityComments = TrimOrNull(source.SecurityComments);
        target.RestorationTestedWithinYear = source.RestorationTestedWithinYear;
        target.LastRestorationTestResult = TrimOrNull(source.LastRestorationTestResult);
        target.FailoverTestPerformed = source.FailoverTestPerformed;
        target.LastFailoverTestDate = source.LastFailoverTestDate;
        target.PreviousFailoverTestDate = source.PreviousFailoverTestDate;
        target.PendingTestActionsCount = source.PendingTestActionsCount;
        target.ProcessesPersonalData = source.ProcessesPersonalData;
        target.NonProductionPersonalData = source.NonProductionPersonalData;
        target.NonProductionBusinessData = source.NonProductionBusinessData;
        target.PersonalDataPseudonymization = TrimOrNull(source.PersonalDataPseudonymization);
    }

    private static IQueryable<ConfigurationItemDetailDto> ProjectDetails(
        IQueryable<ConfigurationItem> source) =>
        source.Select(x => new ConfigurationItemDetailDto
        {
            Id = x.Id,
            ExternalCiNumber = x.ExternalCiNumber,
            Name = x.Name,
            Label = x.Label,
            Model = x.Model,
            Category = x.Category,
            Status = x.Status,
            ApplicationDomain = x.ApplicationDomain,
            EntityPath = x.EntityPath,
            ResponsibleEmployer = x.ResponsibleEmployer,
            IsPlaceholder = x.IsPlaceholder,
            IsCurrent = x.IsCurrent,
            Locked = x.Locked,
            CreatedDate = x.CreatedDate,
            UpdatedDate = x.UpdatedDate,
            ApplicationCode = x.ApplicationCode,
            Version = x.Version,
            DatabaseCode = x.DatabaseCode,
            PlatformType = x.PlatformType,
            PlatformName = x.PlatformName,
            BudgetCode = x.BudgetCode,
            OwnerName = x.OwnerName,
            Rto = x.Rto,
            Rpo = x.Rpo,
            IncomingRelationshipCount = x.IncomingRelationships.Count(y => y.IsCurrent),
            OutgoingRelationshipCount = x.OutgoingRelationships.Count(y => y.IsCurrent),
            Attributes = x.AttributeValues.OrderBy(y => y.AttributeDefinition.DisplayName)
                .Select(y => new CiAttributeDto
                {
                    Code = y.AttributeDefinition.Code,
                    Name = y.AttributeDefinition.DisplayName,
                    Value = y.RawValue,
                }).ToList(),
            SupportAssignments = x.SupportAssignments.OrderBy(y => y.RoleName)
                .Select(y => new CiSupportAssignmentDto
                {
                    GroupName = y.GroupName,
                    RoleName = y.RoleName,
                    ManagerName = y.ManagerName,
                }).ToList(),
            ApplicationProfile = x.ApplicationProfile == null
                ? new ConfigurationItemApplicationProfileDto()
                : new ConfigurationItemApplicationProfileDto
                {
                    ShortDescription = x.ApplicationProfile.ShortDescription,
                    DetailedDescription = x.ApplicationProfile.DetailedDescription,
                    MainFunctionalProcesses = x.ApplicationProfile.MainFunctionalProcesses,
                    GeneralTechnicalFramework = x.ApplicationProfile.GeneralTechnicalFramework,
                    OverallArchitecture = x.ApplicationProfile.OverallArchitecture,
                    ApplicationCriticality = x.ApplicationProfile.ApplicationCriticality,
                    ApplicationNature = x.ApplicationProfile.ApplicationNature,
                    InternetExposed = x.ApplicationProfile.InternetExposed,
                    LegalOwnerEntity = x.ApplicationProfile.LegalOwnerEntity,
                    OtherStakeholders = x.ApplicationProfile.OtherStakeholders,
                    SourceCodeAvailable = x.ApplicationProfile.SourceCodeAvailable,
                    HostingMode = x.ApplicationProfile.HostingMode,
                    HostingProvider = x.ApplicationProfile.HostingProvider,
                    CloudServiceModel = x.ApplicationProfile.CloudServiceModel,
                    HostingNetworkZone = x.ApplicationProfile.HostingNetworkZone,
                    AuthenticationMode = x.ApplicationProfile.AuthenticationMode,
                    IamSolution = x.ApplicationProfile.IamSolution,
                    StandalonePasswordRules = x.ApplicationProfile.StandalonePasswordRules,
                    MfaEnabled = x.ApplicationProfile.MfaEnabled,
                    InternalTechnicalAdminCount = x.ApplicationProfile.InternalTechnicalAdminCount,
                    ExternalTechnicalAdminCount = x.ApplicationProfile.ExternalTechnicalAdminCount,
                    LastAccessRecertificationDate = x.ApplicationProfile.LastAccessRecertificationDate,
                    LastAccessRemediationPercentage = x.ApplicationProfile.LastAccessRemediationPercentage,
                    PreviousAccessRecertificationDate = x.ApplicationProfile.PreviousAccessRecertificationDate,
                    PreviousAccessRemediationPercentage = x.ApplicationProfile.PreviousAccessRemediationPercentage,
                    CodeScanEnabled = x.ApplicationProfile.CodeScanEnabled,
                    LastPentestDate = x.ApplicationProfile.LastPentestDate,
                    PreviousPentestDate = x.ApplicationProfile.PreviousPentestDate,
                    LastRedTeamDate = x.ApplicationProfile.LastRedTeamDate,
                    LastBugBountyDate = x.ApplicationProfile.LastBugBountyDate,
                    OpenRecommendationsLow = x.ApplicationProfile.OpenRecommendationsLow,
                    OpenRecommendationsMedium = x.ApplicationProfile.OpenRecommendationsMedium,
                    OpenRecommendationsHigh = x.ApplicationProfile.OpenRecommendationsHigh,
                    OverdueRecommendationsLow = x.ApplicationProfile.OverdueRecommendationsLow,
                    OverdueRecommendationsMedium = x.ApplicationProfile.OverdueRecommendationsMedium,
                    OverdueRecommendationsHigh = x.ApplicationProfile.OverdueRecommendationsHigh,
                    SecurityComments = x.ApplicationProfile.SecurityComments,
                    RestorationTestedWithinYear = x.ApplicationProfile.RestorationTestedWithinYear,
                    LastRestorationTestResult = x.ApplicationProfile.LastRestorationTestResult,
                    FailoverTestPerformed = x.ApplicationProfile.FailoverTestPerformed,
                    LastFailoverTestDate = x.ApplicationProfile.LastFailoverTestDate,
                    PreviousFailoverTestDate = x.ApplicationProfile.PreviousFailoverTestDate,
                    PendingTestActionsCount = x.ApplicationProfile.PendingTestActionsCount,
                    ProcessesPersonalData = x.ApplicationProfile.ProcessesPersonalData,
                    NonProductionPersonalData = x.ApplicationProfile.NonProductionPersonalData,
                    NonProductionBusinessData = x.ApplicationProfile.NonProductionBusinessData,
                    PersonalDataPseudonymization = x.ApplicationProfile.PersonalDataPseudonymization,
                    CreatedDate = x.ApplicationProfile.CreatedDate,
                    UpdatedDate = x.ApplicationProfile.UpdatedDate,
                },
        });
}
