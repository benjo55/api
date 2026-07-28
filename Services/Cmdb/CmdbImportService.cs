using System.Globalization;
using System.Text;
using api.Data;
using api.Dtos.Cmdb;
using api.Models.Cmdb;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace api.Services.Cmdb;

public interface ICmdbImportService
{
    Task<CmdbImportResultDto> ImportAsync(IReadOnlyCollection<IFormFile> files, CancellationToken cancellationToken);
    Task<CmdbImportResultDto> ImportDirectoryAsync(string directoryPath, CancellationToken cancellationToken);
}

public sealed class CmdbImportService : ICmdbImportService
{
    private readonly ApplicationDBContext _db;
    private readonly ILogger<CmdbImportService> _logger;

    public CmdbImportService(ApplicationDBContext db, ILogger<CmdbImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CmdbImportResultDto> ImportDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Le répertoire CMDB n'existe pas : {directoryPath}");
        }

        var markers = new[]
        {
            "ARAP Complet",
            "ARAP + Role",
            "CMDB Caracteristiques",
            "CMDB Liens Impactant",
        };
        var allCsvFiles = Directory.GetFiles(directoryPath, "*.CSV", System.IO.SearchOption.TopDirectoryOnly);
        var streams = new List<FileStream>();
        try
        {
            var formFiles = new List<IFormFile>();
            foreach (var marker in markers)
            {
                var path = allCsvFiles.SingleOrDefault(x =>
                    Path.GetFileName(x).Contains(marker, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Le fichier « {marker} » est obligatoire.");
                var stream = File.OpenRead(path);
                streams.Add(stream);
                formFiles.Add(new FormFile(stream, 0, stream.Length, "files", Path.GetFileName(path)));
            }

            return await ImportAsync(formFiles, cancellationToken);
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    public async Task<CmdbImportResultDto> ImportAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var arapFile = FindFile(files, "ARAP Complet");
        var rolesFile = FindFile(files, "ARAP + Role");
        var characteristicsFile = FindFile(files, "CMDB Caracteristiques");
        var relationshipsFile = FindFile(files, "CMDB Liens Impactant");

        var arapRows = ReadCsv(arapFile);
        var roleRows = ReadCsv(rolesFile);
        var characteristicRows = ReadCsv(characteristicsFile);
        var relationshipRows = ReadCsv(relationshipsFile);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var importRun = new CmdbImportRun();
        _db.CmdbImportRuns.Add(importRun);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var ciByNumber = await _db.ConfigurationItems
                .ToDictionaryAsync(x => x.ExternalCiNumber, StringComparer.OrdinalIgnoreCase, cancellationToken);

            foreach (var ci in ciByNumber.Values)
            {
                ci.IsCurrent = false;
            }

            var inserted = 0;
            var updated = 0;

            ConfigurationItem UpsertCi(
                string number,
                string name,
                string model,
                string? status,
                bool authoritative)
            {
                number = number.Trim();
                if (!ciByNumber.TryGetValue(number, out var ci))
                {
                    ci = new ConfigurationItem
                    {
                        ExternalCiNumber = number,
                        Name = string.IsNullOrWhiteSpace(name) ? number : name.Trim(),
                        Model = string.IsNullOrWhiteSpace(model) ? "Inconnu" : model.Trim(),
                        Status = NullIfEmpty(status),
                        IsPlaceholder = !authoritative,
                        IsCurrent = true,
                    };
                    ciByNumber.Add(number, ci);
                    _db.ConfigurationItems.Add(ci);
                    inserted++;
                }
                else
                {
                    updated++;
                    ci.IsCurrent = true;
                    if (authoritative || string.IsNullOrWhiteSpace(ci.Name))
                    {
                        ci.Name = string.IsNullOrWhiteSpace(name) ? ci.Name : name.Trim();
                    }
                    if (authoritative || string.IsNullOrWhiteSpace(ci.Model))
                    {
                        ci.Model = string.IsNullOrWhiteSpace(model) ? ci.Model : model.Trim();
                    }
                    if ((authoritative || string.IsNullOrWhiteSpace(ci.Status)) && !string.IsNullOrWhiteSpace(status))
                    {
                        ci.Status = status.Trim();
                    }
                    ci.IsPlaceholder &= !authoritative;
                    ci.UpdatedDate = DateTime.UtcNow;
                }
                return ci;
            }

            foreach (var row in arapRows)
            {
                var number = Get(row, "N° de CI");
                if (string.IsNullOrWhiteSpace(number))
                {
                    importRun.RejectedCount++;
                    continue;
                }

                var ci = UpsertCi(number, Get(row, "Nom"), Get(row, "Modèle"), Get(row, "Statut du CI"), true);
                ci.Label = NullIfEmpty(Get(row, "Libellé application"));
                ci.Category = NullIfEmpty(Get(row, "Catégorie"));
                ci.ApplicationCode = NullIfEmpty(Get(row, "Code Application"));
                ci.Version = NullIfEmpty(Get(row, "Version"));
                ci.DatabaseCode = NullIfEmpty(Get(row, "Code Base de données"));
                ci.EntityPath = NullIfEmpty(Get(row, "Entité (complète)"));
                ci.ResponsibleEmployer = ResolveResponsibleEmployer(row);
                ci.ApplicationDomain = NullIfEmpty(Get(row, "Domaine Applicatif"));
                ci.PlatformType = NullIfEmpty(Get(row, "Type plateforme"));
                ci.PlatformName = NullIfEmpty(Get(row, "Plateforme SI"));
                ci.BudgetCode = NullIfEmpty(Get(row, "Code budgétaire"));
                ci.OwnerName = NullIfEmpty(Get(row, "Responsable"));
                ci.Rto = NullIfEmpty(Get(row, "RTO"));
                ci.Rpo = NullIfEmpty(Get(row, "RPO"));
                ci.SourceUpdatedAt = ParseFrenchDate(Get(row, "Date maj"));
            }

            foreach (var row in characteristicRows)
            {
                var number = Get(row, "N° de CI");
                if (!string.IsNullOrWhiteSpace(number))
                {
                    UpsertCi(number, Get(row, "Nom"), Get(row, "Modèle"), Get(row, "Statut du CI"), false);
                }
            }

            foreach (var row in relationshipRows)
            {
                var sourceNumber = Get(row, "CI Impactant : N° de CI");
                var targetNumber = Get(row, "CI : N° de CI");
                if (!string.IsNullOrWhiteSpace(sourceNumber))
                {
                    UpsertCi(
                        sourceNumber,
                        Get(row, "CI Impactant : Nom"),
                        Get(row, "CI Impactant : Modèle"),
                        Get(row, "CI Impactant : Statut du CI"),
                        false);
                }
                if (!string.IsNullOrWhiteSpace(targetNumber))
                {
                    UpsertCi(
                        targetNumber,
                        Get(row, "CI : Nom"),
                        Get(row, "CI : Modèle"),
                        null,
                        false);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            await _db.CiAttributeValues.ExecuteDeleteAsync(cancellationToken);
            var definitions = await _db.CiAttributeDefinitions
                .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
            foreach (var row in characteristicRows)
            {
                var number = Get(row, "N° de CI");
                var displayName = Get(row, "Caractéristique");
                if (string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(displayName) ||
                    !ciByNumber.TryGetValue(number, out var ci))
                {
                    importRun.RejectedCount++;
                    continue;
                }

                var code = NormalizeCode(displayName);
                if (!definitions.TryGetValue(code, out var definition))
                {
                    definition = new CiAttributeDefinition
                    {
                        Code = code,
                        DisplayName = displayName,
                        DataType = InferDataType(Get(row, "Valeur")),
                    };
                    definitions.Add(code, definition);
                    _db.CiAttributeDefinitions.Add(definition);
                }
            }
            await _db.SaveChangesAsync(cancellationToken);

            var attributeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var attributeValues = new List<CiAttributeValue>(characteristicRows.Count);
            foreach (var row in characteristicRows)
            {
                var number = Get(row, "N° de CI");
                var displayName = Get(row, "Caractéristique");
                if (!ciByNumber.TryGetValue(number, out var ci) || string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                var definition = definitions[NormalizeCode(displayName)];
                var key = $"{ci.Id}|{definition.Id}";
                if (!attributeKeys.Add(key))
                {
                    importRun.RejectedCount++;
                    continue;
                }

                var raw = NullIfEmpty(Get(row, "Valeur"));
                attributeValues.Add(new CiAttributeValue
                {
                    ConfigurationItemId = ci.Id,
                    AttributeDefinitionId = definition.Id,
                    RawValue = raw,
                    StringValue = raw,
                    NumberValue = ParseDecimal(raw),
                    BooleanValue = ParseBoolean(raw),
                    DateTimeValue = ParseFrenchDate(raw),
                });
            }
            _db.CiAttributeValues.AddRange(attributeValues);

            await _db.CiSupportAssignments.ExecuteDeleteAsync(cancellationToken);
            var supportKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var supportAssignments = new List<CiSupportAssignment>(roleRows.Count);
            foreach (var row in roleRows)
            {
                var number = Get(row, "N° de CI");
                var group = Get(row, "GROUP_CMDB");
                var role = Get(row, "ROLE_FR");
                if (!ciByNumber.TryGetValue(number, out var ci) ||
                    string.IsNullOrWhiteSpace(group) || string.IsNullOrWhiteSpace(role))
                {
                    continue;
                }

                var key = $"{ci.Id}|{group}|{role}";
                if (!supportKeys.Add(key))
                {
                    continue;
                }

                supportAssignments.Add(new CiSupportAssignment
                {
                    ConfigurationItemId = ci.Id,
                    GroupName = group,
                    RoleName = role,
                    ManagerName = NullIfEmpty(Get(row, "Manager du Groupe CMDB")),
                    ManagerEntity = NullIfEmpty(Get(row, "Entité Manager")),
                    ManagerTeam = NullIfEmpty(Get(row, "Equipe Manager")),
                });
            }
            _db.CiSupportAssignments.AddRange(supportAssignments);

            await _db.CmdbRelationships
                .Where(x => x.SourceSystem == "EasyVista")
                .ExecuteDeleteAsync(cancellationToken);
            var relationshipTypes = await _db.CmdbRelationshipTypes
                .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
            foreach (var typeName in relationshipRows
                         .Select(x => NullIfEmpty(Get(x, "Type de relation")) ?? "Non renseigné")
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var code = NormalizeCode(typeName);
                if (!relationshipTypes.ContainsKey(code))
                {
                    var type = new CmdbRelationshipType { Code = code, Name = typeName };
                    relationshipTypes.Add(code, type);
                    _db.CmdbRelationshipTypes.Add(type);
                }
            }
            await _db.SaveChangesAsync(cancellationToken);

            var relationshipKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var relationships = new List<CmdbRelationship>(relationshipRows.Count);
            foreach (var row in relationshipRows)
            {
                var sourceNumber = Get(row, "CI Impactant : N° de CI");
                var targetNumber = Get(row, "CI : N° de CI");
                var typeName = NullIfEmpty(Get(row, "Type de relation")) ?? "Non renseigné";
                if (!ciByNumber.TryGetValue(sourceNumber, out var source) ||
                    !ciByNumber.TryGetValue(targetNumber, out var target))
                {
                    importRun.RejectedCount++;
                    continue;
                }

                var type = relationshipTypes[NormalizeCode(typeName)];
                var key = $"{source.Id}|{target.Id}|{type.Id}";
                if (!relationshipKeys.Add(key))
                {
                    importRun.RejectedCount++;
                    continue;
                }

                relationships.Add(new CmdbRelationship
                {
                    SourceCiId = source.Id,
                    TargetCiId = target.Id,
                    RelationshipTypeId = type.Id,
                    IsBlocking = Get(row, "Bloquant") == "1",
                });
            }
            _db.CmdbRelationships.AddRange(relationships);

            importRun.InsertedCount = inserted;
            importRun.UpdatedCount = updated;
            importRun.AttributeCount = attributeValues.Count;
            importRun.SupportAssignmentCount = supportAssignments.Count;
            importRun.RelationshipCount = relationships.Count;
            importRun.Status = "Succeeded";
            importRun.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Import CMDB {ImportRunId}: {CiCount} CI, {RelationshipCount} relations, {AttributeCount} attributs.",
                importRun.Id, ciByNumber.Count, relationships.Count, attributeValues.Count);

            return new CmdbImportResultDto
            {
                ImportRunId = importRun.Id,
                ConfigurationItemCount = ciByNumber.Count,
                RelationshipCount = relationships.Count,
                AttributeCount = attributeValues.Count,
                SupportAssignmentCount = supportAssignments.Count,
                RejectedCount = importRun.RejectedCount,
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Échec de l'import CMDB.");
            throw;
        }
    }

    private static IFormFile FindFile(IEnumerable<IFormFile> files, string marker) =>
        files.FirstOrDefault(x => x.FileName.Contains(marker, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Le fichier « {marker} » est obligatoire.");

    public static List<Dictionary<string, string>> ReadCsv(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var parser = new TextFieldParser(stream, Encoding.UTF8, true, false)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false,
        };
        parser.SetDelimiters(";");

        var headers = parser.ReadFields()
            ?? throw new InvalidDataException($"Le fichier {file.FileName} ne contient pas d'en-tête.");
        var rows = new List<Dictionary<string, string>>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                row[headers[i].Trim()] = i < fields.Length ? fields[i].Trim() : string.Empty;
            }
            rows.Add(row);
        }
        return rows;
    }

    private static string Get(IReadOnlyDictionary<string, string> row, string column) =>
        row.TryGetValue(column, out var value) ? value.Trim() : string.Empty;

    public static string? ResolveResponsibleEmployer(
        IReadOnlyDictionary<string, string> row) =>
        NullIfEmpty(Get(row, "Employeur Responsable CMDB")) ??
        NullIfEmpty(Get(row, "Responsable CMDB Employeur"));

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? ParseFrenchDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        var formats = new[] { "dd/MM/yyyy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy H:mm" };
        return DateTime.TryParseExact(value.Trim(), formats, CultureInfo.GetCultureInfo("fr-FR"),
            DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("fr-FR"), out var parsed)
            ? parsed
            : null;

    private static bool? ParseBoolean(string? value) => value?.Trim() switch
    {
        "1" => true,
        "0" => false,
        _ => null,
    };

    private static string InferDataType(string? value)
    {
        if (ParseBoolean(value).HasValue) return "Boolean";
        if (ParseDecimal(value).HasValue) return "Number";
        if (ParseFrenchDate(value).HasValue) return "DateTime";
        return "String";
    }

    private static string NormalizeCode(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var previousUnderscore = false;
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
                previousUnderscore = false;
            }
            else if (!previousUnderscore)
            {
                builder.Append('_');
                previousUnderscore = true;
            }
        }
        return builder.ToString().Trim('_');
    }
}
