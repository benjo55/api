using api.Data;
using api.Dtos.Cmdb;
using api.Services.Cmdb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace api.Controllers;

[ApiController]
[Route("api/cartography")]
public sealed class CartographyController : ControllerBase
{
    private const string EmployerEntitiesCacheKey = "cartography:employer-entities:v3";
    private static readonly TimeSpan EmployerEntitiesCacheDuration = TimeSpan.FromMinutes(5);
    private const string GeneralDomainCode = "GENERAL";

    private readonly ApplicationDBContext _db;
    private readonly IMemoryCache _cache;

    public CartographyController(ApplicationDBContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    [HttpGet("employer-entities")]
    public async Task<ActionResult<List<CartographyEmployerEntityDto>>> GetEmployerEntities(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(EmployerEntitiesCacheKey, out List<CartographyEmployerEntityDto>? cached)
            && cached is not null)
        {
            return Ok(cached);
        }

        var ownership = await _db.ConfigurationItems.AsNoTracking()
            .Where(x => x.IsCurrent &&
                ((x.ResponsibleEmployer != null && x.ResponsibleEmployer != "") ||
                 (x.EntityPath != null && x.EntityPath != "")))
            .Select(x => new
            {
                x.Id,
                x.ResponsibleEmployer,
                x.EntityPath,
                x.Model,
                x.Category,
                x.Name,
            })
            .ToListAsync(cancellationToken);

        var ownershipByCiId = ownership
            .Select(x => new
            {
                x.Id,
                EmployerEntity = CmdbEmployerResolver.Resolve(
                    x.EntityPath,
                    x.ResponsibleEmployer),
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.EmployerEntity))
            .ToDictionary(x => x.Id, x => x.EmployerEntity!, EqualityComparer<int>.Default);

        var flowEdges = await _db.IntegrationFlows.AsNoTracking()
            .Where(x => x.Status != "Retired")
            .Select(x => new
            {
                x.SourceCiId,
                x.TargetCiId,
            })
            .ToListAsync(cancellationToken);

        var flowCountsByEntity = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var flow in flowEdges)
        {
            AddEndpointContribution(flowCountsByEntity, ownershipByCiId, flow.SourceCiId);
            AddEndpointContribution(flowCountsByEntity, ownershipByCiId, flow.TargetCiId);
        }

        var relationshipEdges = await _db.CmdbRelationships.AsNoTracking()
            .Where(x => x.IsCurrent)
            .Select(x => new
            {
                x.SourceCiId,
                x.TargetCiId,
            })
            .ToListAsync(cancellationToken);

        var relationshipCountsByEntity = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var relationship in relationshipEdges)
        {
            AddEndpointContribution(relationshipCountsByEntity, ownershipByCiId, relationship.SourceCiId);
            AddEndpointContribution(relationshipCountsByEntity, ownershipByCiId, relationship.TargetCiId);
        }

        var entities = ownership
            .Select(x => new
            {
                EmployerEntity = CmdbEmployerResolver.Resolve(
                    x.EntityPath,
                    x.ResponsibleEmployer),
                Type = ClassifyConfigurationItemType(x.Model, x.Category, x.Name),
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.EmployerEntity))
            .GroupBy(x => x.EmployerEntity!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CartographyEmployerEntityDto
            {
                Name = group.Select(x => x.EmployerEntity!)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .First(),
                ConfigurationItemCount = group.Count(),
                FlowCount = flowCountsByEntity.GetValueOrDefault(group.Key),
                CmdbRelationshipCount = relationshipCountsByEntity.GetValueOrDefault(group.Key),
                TypeCounts = CiTypeLabels
                    .Select(type => new CartographyEmployerEntityTypeCountDto
                    {
                        Type = type.Key,
                        Label = type.Value,
                        Count = group.Count(x => x.Type == type.Key),
                    })
                    .Where(x => x.Count > 0)
                    .ToList(),
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        entities.Insert(0, new CartographyEmployerEntityDto
        {
            Name = GeneralDomainCode,
            ConfigurationItemCount = 0,
            FlowCount = 0,
            CmdbRelationshipCount = 0,
            TypeCounts =
            [
                new CartographyEmployerEntityTypeCountDto
                {
                    Type = "documentaryDomain",
                    Label = "Vision générale du SI ESF",
                    Count = 1,
                },
            ],
        });

        _cache.Set(
            EmployerEntitiesCacheKey,
            entities,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = EmployerEntitiesCacheDuration,
                Size = 1,
            });

        return Ok(entities);
    }

    private static void AddEndpointContribution(
        Dictionary<string, decimal> countsByEntity,
        IReadOnlyDictionary<int, string> ownershipByCiId,
        int configurationItemId)
    {
        if (!ownershipByCiId.TryGetValue(configurationItemId, out var entity) ||
            string.IsNullOrWhiteSpace(entity))
        {
            return;
        }

        countsByEntity[entity] = countsByEntity.GetValueOrDefault(entity) + 0.5m;
    }

    private static readonly IReadOnlyDictionary<string, string> CiTypeLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["businessApplication"] = "Application métier",
            ["technicalApplication"] = "Application technique",
            ["server"] = "Serveur",
            ["database"] = "Database",
            ["serviceDomain"] = "Service / domaine",
            ["other"] = "Autres CI",
        };

    private static string NormalizeForClassification(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(chars)
            .Normalize(System.Text.NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    private static string ClassifyConfigurationItemType(
        string? model,
        string? category,
        string? name)
    {
        var normalizedCategory = NormalizeForClassification(category);
        var normalizedModel = NormalizeForClassification(model);
        var normalizedName = NormalizeForClassification(name);
        var searchable = $"{normalizedCategory} {normalizedModel} {normalizedName}";

        if (searchable.Contains("base de donnees") ||
            searchable.Contains("database") ||
            searchable.Contains("sgbd") ||
            searchable.Contains("oracle") ||
            searchable.Contains("mysql") ||
            searchable.Contains("postgres") ||
            searchable.Contains("sql server"))
        {
            return "database";
        }

        if (normalizedCategory.Contains("application metier"))
        {
            return "businessApplication";
        }

        if (normalizedCategory.Contains("application") ||
            normalizedModel == "application" ||
            normalizedModel.Contains("application"))
        {
            return "technicalApplication";
        }

        if (normalizedCategory.Contains("serveur") ||
            normalizedModel.Contains("serveur") ||
            new[] { "vmware", "aix", "aws-ec2", "xenserver", "ovm", "ibmi" }
                .Any(searchable.Contains))
        {
            return "server";
        }

        if (normalizedCategory.Contains("service") ||
            normalizedCategory.Contains("domaine") ||
            normalizedModel == "service" ||
            normalizedModel == "domaine")
        {
            return "serviceDomain";
        }

        return "other";
    }

    [HttpGet("graph")]
    public async Task<ActionResult<CartographyGraphDto>> GetGraph(
        [FromQuery] int rootCiId,
        [FromQuery] int depth = 0,
        [FromQuery] string direction = "Both",
        [FromQuery] bool includeCmdb = true,
        [FromQuery] bool includeFlows = true,
        [FromQuery] int maxNodes = 300,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.ConfigurationItems.AnyAsync(
                x => x.Id == rootCiId && x.IsCurrent,
                cancellationToken))
        {
            return NotFound("Le CI racine n'existe pas ou n'est plus actif.");
        }

        return Ok(await BuildGraph(
            [rootCiId],
            depth,
            direction,
            includeCmdb,
            includeFlows,
            maxNodes,
            cancellationToken));
    }

    [HttpGet("entity-graph")]
    public async Task<ActionResult<CartographyGraphDto>> GetEntityGraph(
        [FromQuery(Name = "employerEntity")] string[] employerEntities,
        [FromQuery] int depth = 0,
        [FromQuery] string direction = "Both",
        [FromQuery] bool includeCmdb = true,
        [FromQuery] bool includeFlows = true,
        [FromQuery] int maxNodes = 2000,
        CancellationToken cancellationToken = default)
    {
        var selectedEntities = employerEntities
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selectedEntities.Count == 0)
        {
            return BadRequest("Au moins une entité employeur est obligatoire.");
        }

        var ownership = await _db.ConfigurationItems.AsNoTracking()
            .Where(x => x.IsCurrent)
            .Select(x => new
            {
                x.Id,
                x.EntityPath,
                x.ResponsibleEmployer,
            })
            .ToListAsync(cancellationToken);

        var rootNodeIds = ownership
            .Where(x =>
            {
                var employerEntity = CmdbEmployerResolver.Resolve(
                    x.EntityPath,
                    x.ResponsibleEmployer);
                return employerEntity is not null &&
                    selectedEntities.Contains(employerEntity);
            })
            .Select(x => x.Id)
            .ToHashSet();

        if (rootNodeIds.Count == 0)
        {
            return NotFound("Aucun CI actif n'est rattaché aux entités employeur sélectionnées.");
        }

        return Ok(await BuildGraph(
            rootNodeIds,
            depth,
            direction,
            includeCmdb,
            includeFlows,
            maxNodes,
            cancellationToken));
    }

    private async Task<CartographyGraphDto> BuildGraph(
        HashSet<int> rootNodeIds,
        int depth,
        string direction,
        bool includeCmdb,
        bool includeFlows,
        int maxNodes,
        CancellationToken cancellationToken)
    {
        depth = Math.Clamp(depth, 0, 5);
        maxNodes = Math.Clamp(maxNodes, 10, 2000);
        direction = direction is "Upstream" or "Downstream" ? direction : "Both";

        var truncated = rootNodeIds.Count > maxNodes;
        var nodeIds = rootNodeIds.OrderBy(x => x).Take(maxNodes).ToHashSet();
        var frontier = nodeIds.ToHashSet();

        for (var level = 0; level < depth && frontier.Count > 0 && !truncated; level++)
        {
            var current = frontier.ToList();
            var next = new HashSet<int>();

            if (includeCmdb)
            {
                var relationships = await _db.CmdbRelationships.AsNoTracking()
                    .Where(x => x.IsCurrent &&
                        ((direction != "Upstream" && current.Contains(x.SourceCiId)) ||
                         (direction != "Downstream" && current.Contains(x.TargetCiId))))
                    .Select(x => new { x.SourceCiId, x.TargetCiId })
                    .ToListAsync(cancellationToken);

                foreach (var edge in relationships)
                {
                    next.Add(edge.SourceCiId);
                    next.Add(edge.TargetCiId);
                }
            }

            if (includeFlows)
            {
                var flows = await _db.IntegrationFlows.AsNoTracking()
                    .Where(x => x.Status != "Retired" &&
                        ((direction != "Upstream" && current.Contains(x.SourceCiId)) ||
                         (direction != "Downstream" && current.Contains(x.TargetCiId))))
                    .Select(x => new { x.SourceCiId, x.TargetCiId })
                    .ToListAsync(cancellationToken);

                foreach (var edge in flows)
                {
                    next.Add(edge.SourceCiId);
                    next.Add(edge.TargetCiId);
                }
            }

            next.ExceptWith(nodeIds);
            foreach (var id in next.OrderBy(x => x))
            {
                if (nodeIds.Count >= maxNodes)
                {
                    truncated = true;
                    break;
                }

                nodeIds.Add(id);
            }

            frontier = next.Where(nodeIds.Contains).ToHashSet();
        }

        var cmdbEdges = includeCmdb
            ? await _db.CmdbRelationships.AsNoTracking()
                .Where(x => x.IsCurrent &&
                    nodeIds.Contains(x.SourceCiId) &&
                    nodeIds.Contains(x.TargetCiId))
                .Select(x => new CartographyEdgeDto
                {
                    Id = $"cmdb:{x.Id}",
                    Source = x.SourceCiId,
                    Target = x.TargetCiId,
                    Kind = "cmdb",
                    Label = x.RelationshipType.Name,
                    IsBlocking = x.IsBlocking,
                })
                .ToListAsync(cancellationToken)
            : [];

        var flowEdges = includeFlows
            ? await _db.IntegrationFlows.AsNoTracking()
                .Where(x => x.Status != "Retired" &&
                    nodeIds.Contains(x.SourceCiId) &&
                    nodeIds.Contains(x.TargetCiId))
                .Select(x => new CartographyEdgeDto
                {
                    Id = $"flow:{x.Id}",
                    Source = x.SourceCiId,
                    Target = x.TargetCiId,
                    Kind = "flow",
                    Label = x.Name,
                    Family = x.ExchangePattern.Family,
                    InteractionMode = x.ExchangePattern.InteractionMode,
                })
                .ToListAsync(cancellationToken)
            : [];

        var nodes = await _db.ConfigurationItems.AsNoTracking()
            .Where(x => nodeIds.Contains(x.Id))
            .Select(x => new CartographyNodeDto
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
                OwnerName = x.OwnerName,
                PlatformType = x.PlatformType,
                PlatformName = x.PlatformName,
                IsPlaceholder = x.IsPlaceholder,
                IsRoot = rootNodeIds.Contains(x.Id),
            })
            .ToListAsync(cancellationToken);

        foreach (var node in nodes)
        {
            node.EmployerEntity = CmdbEmployerResolver.Resolve(
                node.EntityPath,
                node.ResponsibleEmployer);
        }

        return new CartographyGraphDto
        {
            Nodes = nodes,
            Edges = cmdbEdges.Concat(flowEdges).ToList(),
            Truncated = truncated,
            Depth = depth,
        };
    }
}
