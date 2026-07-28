using api.Data;
using api.Dtos.LegalDocuments;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.LegalDocuments
{
    public sealed class DocumentVersioningService : IDocumentVersioningService
    {
        private readonly ApplicationDBContext _db;
        private readonly IDocumentNumberingService _numberingService;
        private readonly IDocumentAuditService _auditService;

        public DocumentVersioningService(
            ApplicationDBContext db,
            IDocumentNumberingService numberingService,
            IDocumentAuditService auditService)
        {
            _db = db;
            _numberingService = numberingService;
            _auditService = auditService;
        }

        public async Task<LegalDocumentRevisionDto> CreateVersionAsync(int definitionId, CreateDocumentVersionDto dto, string? userName, CancellationToken cancellationToken = default)
        {
            var definition = await _db.LegalDocumentDefinitions
                .Include(x => x.Revisions)
                .FirstOrDefaultAsync(x => x.Id == definitionId, cancellationToken)
                ?? throw new KeyNotFoundException("Definition not found.");

            if (definition.CurrentDraftRevisionId is not null)
            {
                throw new BusinessException("This definition already has a current draft revision.");
            }

            var source = await _db.LegalDocumentRevisions
                .Include(x => x.Nodes)
                .FirstOrDefaultAsync(x => x.Id == dto.SourceRevisionId && x.LegalDocumentDefinitionId == definitionId, cancellationToken)
                ?? throw new KeyNotFoundException("Source revision not found.");

            if (source.Status == DocumentRevisionStatus.Draft)
            {
                throw new BusinessException("Create a new version from a validated or published revision, not from a draft.");
            }

            var nextMajor = source.MajorVersion;
            var nextMinor = source.MinorVersion;
            if (dto.BumpType == VersionBumpType.Major)
            {
                nextMajor++;
                nextMinor = 0;
            }
            else
            {
                nextMinor++;
            }

            var revision = new LegalDocumentRevision
            {
                LegalDocumentDefinitionId = definitionId,
                BasedOnRevisionId = source.Id,
                MajorVersion = nextMajor,
                MinorVersion = nextMinor,
                Status = DocumentRevisionStatus.Draft,
                ChangeSummary = dto.ChangeSummary,
                DocumentLayoutTemplateId = source.DocumentLayoutTemplateId,
                EffectiveFrom = source.EffectiveFrom,
                EffectiveTo = source.EffectiveTo,
                CreatedBy = userName
            };

            _db.LegalDocumentRevisions.Add(revision);
            await _db.SaveChangesAsync(cancellationToken);

            var cloneBySourceId = new Dictionary<int, LegalDocumentNode>();
            foreach (var sourceNode in source.Nodes.OrderBy(x => x.ParentNodeId.HasValue).ThenBy(x => x.SortOrder))
            {
                var clone = new LegalDocumentNode
                {
                    LegalDocumentRevisionId = revision.Id,
                    ParentNodeId = sourceNode.ParentNodeId is null ? null : cloneBySourceId[sourceNode.ParentNodeId.Value].Id,
                    StableKey = sourceNode.StableKey,
                    Type = sourceNode.Type,
                    BusinessCode = sourceNode.BusinessCode,
                    Title = sourceNode.Title,
                    EditorJson = sourceNode.EditorJson,
                    ContentHtml = sourceNode.ContentHtml,
                    PlainText = sourceNode.PlainText,
                    SortOrder = sourceNode.SortOrder,
                    IncludeInTableOfContents = sourceNode.IncludeInTableOfContents,
                    StartOnNewPage = sourceNode.StartOnNewPage,
                    KeepWithNext = sourceNode.KeepWithNext,
                    NumberingStyle = sourceNode.NumberingStyle,
                    IsConditional = sourceNode.IsConditional,
                    DisplayConditionJson = sourceNode.DisplayConditionJson,
                    SourceClauseRevisionId = sourceNode.SourceClauseRevisionId
                };
                _db.LegalDocumentNodes.Add(clone);
                await _db.SaveChangesAsync(cancellationToken);
                cloneBySourceId[sourceNode.Id] = clone;
            }

            definition.CurrentDraftRevisionId = revision.Id;
            revision.ContentHash = await ComputeRevisionHashAsync(revision.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await _auditService.AddAsync(DocumentAuditAction.VersionCreated, definition.Id, revision.Id, null, new { sourceRevisionId = source.Id, dto.BumpType }, userName, cancellationToken);

            var fresh = await _db.LegalDocumentRevisions
                .AsNoTracking()
                .Include(x => x.LegalDocumentDefinition)
                .Include(x => x.Nodes)
                .FirstAsync(x => x.Id == revision.Id, cancellationToken);
            var numbers = _numberingService.GenerateNumbers(fresh.Nodes);
            return LegalDocumentMapping.ToDto(fresh, numbers);
        }

        private async Task<string> ComputeRevisionHashAsync(int revisionId, CancellationToken cancellationToken)
        {
            var nodes = await _db.LegalDocumentNodes
                .AsNoTracking()
                .Where(x => x.LegalDocumentRevisionId == revisionId)
                .OrderBy(x => x.ParentNodeId)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.StableKey)
                .Select(x => new { x.StableKey, x.ParentNodeId, x.Type, x.Title, x.ContentHtml, x.SortOrder })
                .ToListAsync(cancellationToken);

            var json = System.Text.Json.JsonSerializer.Serialize(nodes);
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
        }
    }
}
