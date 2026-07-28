using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using api.Data;
using api.Models.Cmdb;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Cmdb;

public sealed record ArchiMateFlowImportResult(
    int DetectedRelationships,
    int DetectedFlows,
    int ImportedFlows,
    int CreatedFlows,
    int UpdatedFlows,
    int ImportedStructuralRelationships,
    int CreatedStructuralRelationships,
    int UpdatedStructuralRelationships,
    int SkippedLegendFlows,
    int DistinctEndpoints,
    int MatchedConfigurationItems,
    int PlaceholderConfigurationItems);

public interface IArchiMateFlowImportService
{
    Task<ArchiMateFlowImportResult> ImportAsync(string filePath, CancellationToken cancellationToken);
}

public sealed class ArchiMateFlowImportService : IArchiMateFlowImportService
{
    private const string ArchiMateNamespace = "http://www.archimatetool.com/archimate";
    private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    private readonly ApplicationDBContext _db;
    private readonly ILogger<ArchiMateFlowImportService> _logger;

    public ArchiMateFlowImportService(
        ApplicationDBContext db,
        ILogger<ArchiMateFlowImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ArchiMateFlowImportResult> ImportAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Le modèle ArchiMate est introuvable.", filePath);
        }

        var document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
        var model = ReadModel(document);
        var elementById = model.Elements.ToDictionary(
            x => x.Id,
            StringComparer.OrdinalIgnoreCase);
        var detectedFlows = model.Relationships
            .Where(x => x.Type == "Flow")
            .ToList();
        var legendFlows = detectedFlows.Where(flow =>
        {
            if (!elementById.TryGetValue(flow.SourceId ?? string.Empty, out var source) ||
                !elementById.TryGetValue(flow.TargetId ?? string.Empty, out var target))
            {
                return false;
            }
            return source.Name == "A" && target.Name == "B" &&
                   Regex.IsMatch(flow.Name, "^F(?:xxx|yyy)", RegexOptions.IgnoreCase);
        }).ToList();
        var dynamicRelationships = model.Relationships
            .Where(x => x.Type is "Flow" or "Triggering")
            .Except(legendFlows)
            .ToList();
        var structuralRelationships = model.Relationships
            .Where(x => x.Type is not ("Flow" or "Triggering"))
            .ToList();
        var importedRelationships = dynamicRelationships
            .Concat(structuralRelationships)
            .ToList();

        var endpointIds = importedRelationships
            .SelectMany(x => new[] { x.SourceId, x.TargetId })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var endpoints = endpointIds
            .Where(elementById.ContainsKey)
            .Select(id => elementById[id])
            .ToList();

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var configurationItems = await _db.ConfigurationItems.ToListAsync(cancellationToken);
        var ciByNumber = configurationItems.ToDictionary(
            x => x.ExternalCiNumber,
            StringComparer.OrdinalIgnoreCase);
        var ciByNormalizedName = configurationItems
            .GroupBy(x => Normalize(x.Name))
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
        var ciByNormalizedLabel = configurationItems
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .GroupBy(x => Normalize(x.Label!))
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

        var endpointGroups = endpoints
            .GroupBy(x => $"{x.Type}|{Normalize(x.Name)}", StringComparer.OrdinalIgnoreCase)
            .ToList();
        var ciByElementId = new Dictionary<string, ConfigurationItem>(StringComparer.OrdinalIgnoreCase);
        var matchedCiCount = 0;
        var placeholderCount = 0;

        foreach (var endpointGroup in endpointGroups)
        {
            var representative = endpointGroup.First();
            var normalizedName = Normalize(representative.Name);
            var candidates = ciByNormalizedName.GetValueOrDefault(normalizedName) ?? [];
            if (candidates.Count == 0)
            {
                candidates = ciByNormalizedLabel.GetValueOrDefault(normalizedName) ?? [];
            }

            ConfigurationItem configurationItem;
            if (candidates.Count == 1)
            {
                configurationItem = candidates[0];
                matchedCiCount++;
            }
            else
            {
                var externalNumber = BuildExternalCiNumber(representative.Type, normalizedName);
                if (!ciByNumber.TryGetValue(externalNumber, out configurationItem!))
                {
                    configurationItem = new ConfigurationItem
                    {
                        ExternalCiNumber = externalNumber,
                        Name = string.IsNullOrWhiteSpace(representative.Name)
                            ? $"Élément ArchiMate {ShortId(representative.Id)}"
                            : representative.Name,
                        Label = $"Élément importé du modèle ArchiMate ({representative.Type})",
                        Model = representative.Type switch
                        {
                            "ApplicationComponent" => "Application",
                            "BusinessActor" => "BusinessActor",
                            "ApplicationFunction" => "ApplicationFunction",
                            _ => $"ArchiMate:{representative.Type}",
                        },
                        Category = representative.Type switch
                        {
                            "ApplicationComponent" => "Application ArchiMate",
                            "BusinessActor" => "Acteur externe ArchiMate",
                            "ApplicationFunction" => "Fonction applicative ArchiMate",
                            _ => "Élément ArchiMate",
                        },
                        Status = "Documentation",
                        IsPlaceholder = true,
                        IsCurrent = true,
                    };
                    _db.ConfigurationItems.Add(configurationItem);
                    ciByNumber.Add(externalNumber, configurationItem);
                    placeholderCount++;
                }
            }

            foreach (var endpoint in endpointGroup)
            {
                ciByElementId[endpoint.Id] = configurationItem;
            }
        }
        await _db.SaveChangesAsync(cancellationToken);

        var patterns = await _db.ExchangePatterns.ToListAsync(cancellationToken);
        ExchangePattern EnsurePattern(
            string code,
            string name,
            string family,
            string interaction,
            string trigger)
        {
            var pattern = patterns.SingleOrDefault(x =>
                string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
            if (pattern is not null)
            {
                return pattern;
            }

            pattern = new ExchangePattern
            {
                Code = code,
                Name = name,
                Family = family,
                InteractionMode = interaction,
                TriggerMode = trigger,
                Description = "Pattern créé pour l'import ArchiMate.",
                IsSystem = true,
                IsActive = true,
            };
            patterns.Add(pattern);
            _db.ExchangePatterns.Add(pattern);
            return pattern;
        }

        EnsurePattern("ARCHIMATE_UNSPECIFIED", "Flux ArchiMate non qualifié", "Unspecified", "Asynchronous", "OnDemand");
        EnsurePattern("ARCHIMATE_TRIGGERING", "Déclenchement ArchiMate", "Event", "Asynchronous", "EventDriven");
        EnsurePattern("MANUAL_EXCHANGE", "Échange manuel", "Manual", "Asynchronous", "OnDemand");
        EnsurePattern("DATABASE_SYNC", "Accès direct base de données", "Database", "Synchronous", "OnDemand");
        await _db.SaveChangesAsync(cancellationToken);

        var patternByCode = patterns.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var existingArchiFlows = await _db.IntegrationFlows
            .Where(x =>
                x.EndpointReference != null &&
                (x.EndpointReference.StartsWith("archimate:") ||
                 x.EndpointReference.StartsWith("archimate-open:")))
            .ToDictionaryAsync(x => x.EndpointReference!, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var usedCodes = await _db.IntegrationFlows
            .ToDictionaryAsync(x => x.Code, x => x.EndpointReference, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var baseCodes = dynamicRelationships
            .Select(x => ExtractBaseCode(x.Name))
            .Where(x => x is not null)
            .Cast<string>()
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

        var createdFlows = 0;
        var updatedFlows = 0;
        foreach (var relationship in dynamicRelationships)
        {
            if (!ciByElementId.TryGetValue(relationship.SourceId ?? string.Empty, out var sourceCi) ||
                !ciByElementId.TryGetValue(relationship.TargetId ?? string.Empty, out var targetCi))
            {
                continue;
            }

            var externalReference = model.IsNativeFormat
                ? $"archimate:{relationship.Id}"
                : $"archimate-open:{ShortId(model.ModelId)}:{relationship.Id}";
            var baseCode = ExtractBaseCode(relationship.Name);
            var proposedCode = baseCode is null
                ? $"ARCHI-{ShortId(relationship.Id)}"
                : baseCodes[baseCode] > 1
                    ? $"{baseCode}-{ShortId(relationship.Id)}"
                    : baseCode;
            proposedCode = EnsureUniqueCode(proposedCode, externalReference, usedCodes);

            var patternCode = relationship.Type == "Triggering"
                ? "ARCHIMATE_TRIGGERING"
                : InferPatternCode(relationship.Name);
            var pattern = patternByCode[patternCode];
            var flowName = string.IsNullOrWhiteSpace(relationship.Name)
                ? relationship.Type == "Triggering"
                    ? $"Déclenchement {sourceCi.Name} → {targetCi.Name}"
                    : $"Flux {sourceCi.Name} → {targetCi.Name}"
                : relationship.Name;
            var description = new StringBuilder()
                .AppendLine($"Import ArchiMate : {Path.GetFileName(filePath)}")
                .AppendLine($"Relation : {relationship.Id}")
                .AppendLine($"Type ArchiMate : {relationship.Type}")
                .AppendLine($"Source ArchiMate : {elementById[relationship.SourceId!].Name}")
                .AppendLine($"Cible ArchiMate : {elementById[relationship.TargetId!].Name}");
            if (!string.IsNullOrWhiteSpace(relationship.Documentation))
            {
                description.AppendLine().Append(relationship.Documentation);
            }

            if (!existingArchiFlows.TryGetValue(externalReference, out var flow))
            {
                flow = new IntegrationFlow
                {
                    EndpointReference = externalReference,
                    CreatedDate = DateTime.UtcNow,
                };
                _db.IntegrationFlows.Add(flow);
                existingArchiFlows.Add(externalReference, flow);
                createdFlows++;
            }
            else
            {
                updatedFlows++;
            }

            flow.Code = proposedCode;
            flow.Name = flowName;
            flow.Description = description.ToString();
            flow.SourceCiId = sourceCi.Id;
            flow.TargetCiId = targetCi.Id;
            flow.ExchangePatternId = pattern.Id;
            flow.TechnologyId = pattern.DefaultTechnologyId;
            flow.FlowGroupCode = baseCode;
            flow.Status = "Active";
            flow.UpdatedDate = DateTime.UtcNow;
            usedCodes[proposedCode] = externalReference;
        }

        var relationshipTypes = await _db.CmdbRelationshipTypes
            .ToListAsync(cancellationToken);
        CmdbRelationshipType EnsureRelationshipType(string archiMateType)
        {
            var code = $"ARCHIMATE_{NormalizeCode(archiMateType)}";
            var relationshipType = relationshipTypes.SingleOrDefault(x =>
                string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
            if (relationshipType is not null)
            {
                return relationshipType;
            }

            relationshipType = new CmdbRelationshipType
            {
                Code = code,
                Name = GetRelationshipTypeName(archiMateType),
                Family = "ArchiMate",
                IsDirectional = archiMateType != "Association",
                IsActive = true,
            };
            relationshipTypes.Add(relationshipType);
            _db.CmdbRelationshipTypes.Add(relationshipType);
            return relationshipType;
        }

        foreach (var relationshipType in structuralRelationships
                     .Select(x => x.Type)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            EnsureRelationshipType(relationshipType);
        }
        await _db.SaveChangesAsync(cancellationToken);

        var sourceSystem = $"ArchiMate:{ShortId(model.ModelId)}";
        var existingStructuralRelationships = await _db.CmdbRelationships
            .Where(x => x.SourceSystem == sourceSystem)
            .ToListAsync(cancellationToken);
        foreach (var relationship in existingStructuralRelationships)
        {
            relationship.IsCurrent = false;
        }
        var existingStructuralByKey = existingStructuralRelationships
            .ToDictionary(
                x => $"{x.SourceCiId}|{x.TargetCiId}|{x.RelationshipTypeId}",
                StringComparer.OrdinalIgnoreCase);
        var createdStructuralRelationships = 0;
        var updatedStructuralRelationships = 0;

        foreach (var relationship in structuralRelationships)
        {
            if (!ciByElementId.TryGetValue(
                    relationship.SourceId ?? string.Empty,
                    out var sourceCi) ||
                !ciByElementId.TryGetValue(
                    relationship.TargetId ?? string.Empty,
                    out var targetCi))
            {
                continue;
            }

            var relationshipType = EnsureRelationshipType(relationship.Type);
            var key =
                $"{sourceCi.Id}|{targetCi.Id}|{relationshipType.Id}";
            if (!existingStructuralByKey.TryGetValue(
                    key,
                    out var cmdbRelationship))
            {
                cmdbRelationship = new CmdbRelationship
                {
                    SourceCiId = sourceCi.Id,
                    TargetCiId = targetCi.Id,
                    RelationshipTypeId = relationshipType.Id,
                    SourceSystem = sourceSystem,
                };
                _db.CmdbRelationships.Add(cmdbRelationship);
                existingStructuralByKey.Add(key, cmdbRelationship);
                createdStructuralRelationships++;
            }
            else
            {
                updatedStructuralRelationships++;
            }

            cmdbRelationship.IsCurrent = true;
            cmdbRelationship.IsBlocking = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Import ArchiMate: {Detected} relations, {Dynamic} dynamiques, {Structural} structurelles, {Matched} CI rapprochés, {Placeholders} placeholders.",
            model.Relationships.Count,
            dynamicRelationships.Count,
            structuralRelationships.Count,
            matchedCiCount,
            placeholderCount);

        return new ArchiMateFlowImportResult(
            model.Relationships.Count,
            detectedFlows.Count,
            dynamicRelationships.Count,
            createdFlows,
            updatedFlows,
            structuralRelationships.Count,
            createdStructuralRelationships,
            updatedStructuralRelationships,
            legendFlows.Count,
            endpointGroups.Count,
            matchedCiCount,
            placeholderCount);
    }

    private static ArchiModel ReadModel(XDocument document)
    {
        var root = document.Root
            ?? throw new InvalidDataException("Le document ArchiMate est vide.");
        var isOpenExchange = root.Name.NamespaceName.StartsWith(
            "http://www.opengroup.org/xsd/archimate/",
            StringComparison.OrdinalIgnoreCase);
        return isOpenExchange
            ? ReadOpenExchangeModel(root)
            : ReadNativeModel(root);
    }

    private static ArchiModel ReadNativeModel(XElement root)
    {
        var xsi = XNamespace.Get(XsiNamespace);
        var nodes = root.Descendants()
            .Where(x => x.Name.LocalName == "element")
            .ToList();
        var elements = new List<ArchiElement>();
        var relationships = new List<ArchiRelationship>();

        foreach (var node in nodes)
        {
            var id = ((string?)node.Attribute("id") ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var type = ((string?)node.Attribute(xsi + "type") ??
                        string.Empty)
                .Replace("archimate:", string.Empty)
                .Trim();
            var name = ((string?)node.Attribute("name") ?? string.Empty)
                .Trim();
            var sourceId = (string?)node.Attribute("source");
            var targetId = (string?)node.Attribute("target");
            var documentation = ChildValue(node, "documentation");

            if (!string.IsNullOrWhiteSpace(sourceId) &&
                !string.IsNullOrWhiteSpace(targetId))
            {
                relationships.Add(new ArchiRelationship(
                    id,
                    name,
                    CanonicalRelationshipType(type),
                    sourceId,
                    targetId,
                    documentation));
            }
            else
            {
                elements.Add(new ArchiElement(id, name, type));
            }
        }

        return new ArchiModel(
            ((string?)root.Attribute("id") ?? "native-model").Trim(),
            true,
            elements,
            relationships);
    }

    private static ArchiModel ReadOpenExchangeModel(XElement root)
    {
        var xsi = XNamespace.Get(XsiNamespace);
        var elements = root.Descendants()
            .Where(x =>
                x.Name.LocalName == "element" &&
                x.Parent?.Name.LocalName == "elements")
            .Select(node => new ArchiElement(
                ((string?)node.Attribute("identifier") ?? string.Empty)
                    .Trim(),
                ChildValue(node, "name") ?? string.Empty,
                ((string?)node.Attribute(xsi + "type") ?? string.Empty)
                    .Trim()))
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToList();
        var relationships = root.Descendants()
            .Where(x =>
                x.Name.LocalName == "relationship" &&
                x.Parent?.Name.LocalName == "relationships")
            .Select(node => new ArchiRelationship(
                ((string?)node.Attribute("identifier") ?? string.Empty)
                    .Trim(),
                ChildValue(node, "name") ?? string.Empty,
                CanonicalRelationshipType(
                    ((string?)node.Attribute(xsi + "type") ??
                     string.Empty).Trim()),
                (string?)node.Attribute("source"),
                (string?)node.Attribute("target"),
                ChildValue(node, "documentation")))
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.Id) &&
                !string.IsNullOrWhiteSpace(x.SourceId) &&
                !string.IsNullOrWhiteSpace(x.TargetId))
            .ToList();

        return new ArchiModel(
            ((string?)root.Attribute("identifier") ??
             "open-exchange-model").Trim(),
            false,
            elements,
            relationships);
    }

    private static string? ChildValue(XElement node, string localName)
    {
        var value = node.Elements()
            .FirstOrDefault(x => x.Name.LocalName == localName)
            ?.Value
            .Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string CanonicalRelationshipType(string type) =>
        type.EndsWith(
            "Relationship",
            StringComparison.OrdinalIgnoreCase)
            ? type[..^"Relationship".Length]
            : type;

    private static string NormalizeCode(string value) =>
        Regex.Replace(value, "[^A-Za-z0-9]+", "_")
            .Trim('_')
            .ToUpperInvariant();

    private static string GetRelationshipTypeName(string type) =>
        type switch
        {
            "Access" => "Accès ArchiMate",
            "Aggregation" => "Agrégation ArchiMate",
            "Assignment" => "Affectation ArchiMate",
            "Association" => "Association ArchiMate",
            "Composition" => "Composition ArchiMate",
            "Realization" => "Réalisation ArchiMate",
            "Serving" => "Service ArchiMate",
            "Specialization" => "Spécialisation ArchiMate",
            _ => $"{type} ArchiMate",
        };

    private static string InferPatternCode(string name)
    {
        if (Regex.IsMatch(name, "non automatis|manuel|excel|xls", RegexOptions.IgnoreCase))
            return "MANUAL_EXCHANGE";
        if (Regex.IsMatch(name, "kafka|événement|event", RegexOptions.IgnoreCase))
            return "KAFKA_EVENT";
        if (Regex.IsMatch(name, "\\bWS\\b|web.?service|\\bAPI\\b|REST|SOAP", RegexOptions.IgnoreCase))
            return "API_SYNC";
        if (Regex.IsMatch(name, "SFTP|fichier|\\bfile\\b|CSV|XML", RegexOptions.IgnoreCase))
            return "SFTP_BATCH";
        if (Regex.IsMatch(name, "accès direct.*DB|directe DB", RegexOptions.IgnoreCase))
            return "DATABASE_SYNC";
        if (Regex.IsMatch(name, "ETL|batch|traitement", RegexOptions.IgnoreCase))
            return "ETL_BATCH";
        return "ARCHIMATE_UNSPECIFIED";
    }

    private static string? ExtractBaseCode(string name)
    {
        var match = Regex.Match(name, "^\\s*(F\\d+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    private static string EnsureUniqueCode(
        string proposedCode,
        string externalReference,
        IReadOnlyDictionary<string, string?> usedCodes)
    {
        if (!usedCodes.TryGetValue(proposedCode, out var existingReference) ||
            string.Equals(existingReference, externalReference, StringComparison.OrdinalIgnoreCase))
        {
            return proposedCode;
        }
        return $"{proposedCode}-{ShortId(externalReference)}";
    }

    private static string BuildExternalCiNumber(string type, string normalizedName)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{type}|{normalizedName}"));
        return $"ARCHI-{Convert.ToHexString(bytes)[..12]}";
    }

    private static string ShortId(string value)
    {
        var normalized = value.Replace("archimate:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("id-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty);
        return normalized.Length <= 8 ? normalized.ToUpperInvariant() : normalized[^8..].ToUpperInvariant();
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }
        return builder.ToString();
    }

    private sealed record ArchiModel(
        string ModelId,
        bool IsNativeFormat,
        List<ArchiElement> Elements,
        List<ArchiRelationship> Relationships);

    private sealed record ArchiElement(
        string Id,
        string Name,
        string Type);

    private sealed record ArchiRelationship(
        string Id,
        string Name,
        string Type,
        string? SourceId,
        string? TargetId,
        string? Documentation);
}
