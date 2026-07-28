using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Data;
using api.Dtos.LegalDocuments;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.LegalDocuments
{
    public sealed class LegalDocumentImportService : ILegalDocumentImportService
    {
        private const int SortStep = 1000;
        private readonly ApplicationDBContext _db;

        public LegalDocumentImportService(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<LegalDocumentImportResult> ImportAsync(
            string filePath,
            string? userName,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Le fichier d'import documentaire est introuvable.", filePath);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new JsonStringEnumConverter());

            await using var stream = File.OpenRead(filePath);
            var source = await JsonSerializer.DeserializeAsync<LegalDocumentImportFile>(
                stream,
                options,
                cancellationToken)
                ?? throw new BusinessException("Le fichier d'import documentaire est vide ou invalide.");

            if (string.IsNullOrWhiteSpace(source.Code) || string.IsNullOrWhiteSpace(source.Name))
            {
                throw new BusinessException("Le code et le nom du document sont obligatoires.");
            }

            var existing = await _db.LegalDocumentDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == source.Code.Trim(), cancellationToken);
            if (existing is not null)
            {
                var revisionId = existing.CurrentDraftRevisionId
                    ?? existing.CurrentPublishedRevisionId
                    ?? await _db.LegalDocumentRevisions
                        .Where(x => x.LegalDocumentDefinitionId == existing.Id)
                        .OrderByDescending(x => x.MajorVersion)
                        .ThenByDescending(x => x.MinorVersion)
                        .Select(x => x.Id)
                        .FirstAsync(cancellationToken);
                var nodeCount = await _db.LegalDocumentNodes
                    .CountAsync(x => x.LegalDocumentRevisionId == revisionId, cancellationToken);
                return new LegalDocumentImportResult(existing.Id, revisionId, nodeCount, false);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var layout = await _db.DocumentLayoutTemplates
                .FirstOrDefaultAsync(x => x.Code == "DEFAULT_A4" && x.IsActive, cancellationToken)
                ?? throw new BusinessException("Le modèle de mise en page DEFAULT_A4 est introuvable.");
            var definition = new LegalDocumentDefinition
            {
                Code = source.Code.Trim(),
                Name = source.Name.Trim(),
                Description = source.Description,
                Type = source.Type,
                CreatedBy = userName
            };
            var revision = new LegalDocumentRevision
            {
                LegalDocumentDefinition = definition,
                MajorVersion = 1,
                MinorVersion = 0,
                Status = DocumentRevisionStatus.Draft,
                DocumentLayoutTemplate = layout,
                CreatedBy = userName,
                ChangeSummary = source.ChangeSummary ?? "Import documentaire initial"
            };
            var root = new LegalDocumentNode
            {
                LegalDocumentRevision = revision,
                Type = DocumentNodeType.Document,
                Title = source.Name.Trim(),
                StableKey = Guid.NewGuid().ToString("N"),
                SortOrder = SortStep,
                IncludeInTableOfContents = false
            };

            LegalDocumentNode BuildNode(
                LegalDocumentImportNode imported,
                LegalDocumentNode parent,
                int sortOrder)
            {
                DocumentStructureService.ValidateChildType(parent.Type, imported.Type);

                var node = new LegalDocumentNode
                {
                    LegalDocumentRevision = revision,
                    ParentNode = parent,
                    Type = imported.Type,
                    Title = imported.Title.Trim(),
                    BusinessCode = imported.BusinessCode?.Trim(),
                    ContentHtml = imported.ContentHtml,
                    PlainText = imported.PlainText,
                    StableKey = Guid.NewGuid().ToString("N"),
                    SortOrder = sortOrder,
                    IncludeInTableOfContents = imported.IncludeInTableOfContents,
                    StartOnNewPage = imported.StartOnNewPage,
                    KeepWithNext = imported.KeepWithNext,
                    NumberingStyle = imported.NumberingStyle
                };

                for (var index = 0; index < imported.Children.Count; index++)
                {
                    node.Children.Add(BuildNode(imported.Children[index], node, (index + 1) * SortStep));
                }

                return node;
            }

            for (var index = 0; index < source.Nodes.Count; index++)
            {
                root.Children.Add(BuildNode(source.Nodes[index], root, (index + 1) * SortStep));
            }

            _db.LegalDocumentDefinitions.Add(definition);
            _db.LegalDocumentRevisions.Add(revision);
            _db.LegalDocumentNodes.Add(root);
            await _db.SaveChangesAsync(cancellationToken);

            definition.CurrentDraftRevisionId = revision.Id;
            revision.ContentHash = await ComputeRevisionHashAsync(revision.Id, cancellationToken);
            _db.DocumentAuditEvents.Add(new DocumentAuditEvent
            {
                Action = DocumentAuditAction.Created,
                LegalDocumentDefinitionId = definition.Id,
                LegalDocumentRevisionId = revision.Id,
                LegalDocumentNodeId = root.Id,
                DetailJson = JsonSerializer.Serialize(new
                {
                    source.Code,
                    sourceFile = Path.GetFileName(filePath),
                    importType = "structured-json"
                }),
                CreatedBy = userName
            });
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new LegalDocumentImportResult(
                definition.Id,
                revision.Id,
                CountNodes(root),
                true);
        }

        private async Task<string> ComputeRevisionHashAsync(
            int revisionId,
            CancellationToken cancellationToken)
        {
            var nodes = await _db.LegalDocumentNodes
                .AsNoTracking()
                .Where(x => x.LegalDocumentRevisionId == revisionId)
                .OrderBy(x => x.ParentNodeId)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.StableKey)
                .Select(x => new
                {
                    x.StableKey,
                    x.ParentNodeId,
                    x.Type,
                    x.BusinessCode,
                    x.Title,
                    x.ContentHtml,
                    x.PlainText,
                    x.SortOrder,
                    x.DisplayConditionJson
                })
                .ToListAsync(cancellationToken);

            var json = JsonSerializer.Serialize(nodes);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        }

        private static int CountNodes(LegalDocumentNode node) =>
            1 + node.Children.Sum(CountNodes);
    }
}
