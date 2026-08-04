using api.Data;
using api.Dtos.LegalDocuments;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.LegalDocuments
{
    public sealed class DocumentStructureService : IDocumentStructureService
    {
        private const int SortStep = 1000;

        private static readonly DocumentNodeType[] ReusableNodeTypes =
        new[]
        {
            DocumentNodeType.Chapter,
            DocumentNodeType.Article,
            DocumentNodeType.Paragraph
        };

        private static readonly IReadOnlyDictionary<DocumentNodeType, DocumentNodeType[]> AllowedChildren =
            new Dictionary<DocumentNodeType, DocumentNodeType[]>
            {
                [DocumentNodeType.Document] = [DocumentNodeType.Part, DocumentNodeType.Title, DocumentNodeType.Chapter],
                [DocumentNodeType.Part] = [DocumentNodeType.Title, DocumentNodeType.Chapter],
                [DocumentNodeType.Title] = [DocumentNodeType.Chapter, DocumentNodeType.Section],
                [DocumentNodeType.Chapter] = [DocumentNodeType.Section, DocumentNodeType.Article],
                [DocumentNodeType.Section] = [DocumentNodeType.Article],
                [DocumentNodeType.Article] = [DocumentNodeType.Article, DocumentNodeType.Paragraph, DocumentNodeType.Clause, DocumentNodeType.Table, DocumentNodeType.Callout, DocumentNodeType.PageBreak],
                [DocumentNodeType.Clause] = [DocumentNodeType.Paragraph],
                [DocumentNodeType.Table] = [],
                [DocumentNodeType.Callout] = [DocumentNodeType.Paragraph],
                [DocumentNodeType.Paragraph] = [],
                [DocumentNodeType.PageBreak] = []
            };

        private readonly ApplicationDBContext _db;
        private readonly IDocumentNumberingService _numberingService;
        private readonly IDocumentAuditService _auditService;

        public DocumentStructureService(
            ApplicationDBContext db,
            IDocumentNumberingService numberingService,
            IDocumentAuditService auditService)
        {
            _db = db;
            _numberingService = numberingService;
            _auditService = auditService;
        }

        public async Task<IReadOnlyList<LegalDocumentDefinitionListDto>> GetDefinitionsAsync(bool? isLibrary, CancellationToken cancellationToken = default)
        {
            var query = _db.LegalDocumentDefinitions.AsNoTracking();

            if (isLibrary is not null)
            {
                query = query.Where(x => x.IsLibrary == isLibrary.Value);
            }

            var definitions = await query
                .OrderBy(x => x.Code)
                .ToListAsync(cancellationToken);

            return definitions.Select(LegalDocumentMapping.ToListDto).ToList();
        }

        public async Task<LegalDocumentDefinitionDto?> GetDefinitionAsync(int definitionId, CancellationToken cancellationToken = default)
        {
            var definition = await _db.LegalDocumentDefinitions
                .AsNoTracking()
                .Include(x => x.Revisions)
                .FirstOrDefaultAsync(x => x.Id == definitionId, cancellationToken);

            return definition is null ? null : LegalDocumentMapping.ToDto(definition);
        }

        public async Task<LegalDocumentDefinitionDto> CreateDefinitionAsync(CreateLegalDocumentDefinitionDto dto, string? userName, CancellationToken cancellationToken = default)
        {
            if (await _db.LegalDocumentDefinitions.AnyAsync(x => x.Code == dto.Code, cancellationToken))
            {
                throw new BusinessException($"A legal document definition with code '{dto.Code}' already exists.");
            }

            var layout = await GetOrCreateDefaultLayoutAsync(cancellationToken);
            var definition = new LegalDocumentDefinition
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Description = dto.Description,
                Type = dto.Type,
                IsLibrary = dto.IsLibrary,
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
                ChangeSummary = "Création initiale"
            };

            var root = new LegalDocumentNode
            {
                LegalDocumentRevision = revision,
                Type = DocumentNodeType.Document,
                Title = dto.Name.Trim(),
                StableKey = Guid.NewGuid().ToString("N"),
                SortOrder = SortStep
            };

            _db.LegalDocumentDefinitions.Add(definition);
            _db.LegalDocumentRevisions.Add(revision);
            _db.LegalDocumentNodes.Add(root);
            await _db.SaveChangesAsync(cancellationToken);

            definition.CurrentDraftRevisionId = revision.Id;
            revision.ContentHash = await ComputeRevisionHashAsync(revision.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await _auditService.AddAsync(DocumentAuditAction.Created, definition.Id, revision.Id, root.Id, new { dto.Code }, userName, cancellationToken);

            return (await GetDefinitionAsync(definition.Id, cancellationToken))!;
        }

        public async Task<LegalDocumentRevisionDto?> GetRevisionAsync(int revisionId, CancellationToken cancellationToken = default)
        {
            var revision = await LoadRevisionWithNodesAsync(revisionId, tracking: false, cancellationToken);
            if (revision is null)
            {
                return null;
            }

            var numbers = _numberingService.GenerateNumbers(revision.Nodes);
            return LegalDocumentMapping.ToDto(revision, numbers);
        }

        public async Task<LegalDocumentNodeDto> AddNodeAsync(int revisionId, CreateLegalDocumentNodeDto dto, string? userName, CancellationToken cancellationToken = default)
        {
            var revision = await LoadRevisionWithNodesAsync(revisionId, tracking: true, cancellationToken)
                ?? throw new KeyNotFoundException("Revision not found.");
            EnsureDraft(revision);

            LegalDocumentNode? parent = null;
            if (dto.ParentNodeId is not null)
            {
                parent = revision.Nodes.FirstOrDefault(x => x.Id == dto.ParentNodeId.Value)
                    ?? throw new BusinessException("Parent node is not part of this revision.");
            }

            ValidateChildType(parent?.Type, dto.Type);

            var sortOrder = ComputeInsertionSortOrder(revision.Nodes, dto.ParentNodeId, dto.InsertRelativeToNodeId, dto.InsertPosition);
            var node = new LegalDocumentNode
            {
                LegalDocumentRevisionId = revisionId,
                ParentNodeId = dto.ParentNodeId,
                Type = dto.Type,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? LegalDocumentLexicon.GetNodeTypeLabel(dto.Type) : dto.Title.Trim(),
                BusinessCode = dto.BusinessCode,
                StableKey = Guid.NewGuid().ToString("N"),
                SortOrder = sortOrder,
                IncludeInTableOfContents = dto.Type is not DocumentNodeType.Paragraph and not DocumentNodeType.PageBreak
            };

            _db.LegalDocumentNodes.Add(node);
            await _db.SaveChangesAsync(cancellationToken);
            await RefreshRevisionHashAsync(revisionId, cancellationToken);
            await _auditService.AddAsync(DocumentAuditAction.Created, revision.LegalDocumentDefinitionId, revisionId, node.Id, new { node.Type, parentId = dto.ParentNodeId }, userName, cancellationToken);

            var fresh = await LoadRevisionWithNodesAsync(revisionId, tracking: false, cancellationToken)
                ?? throw new KeyNotFoundException("Revision not found.");
            var numbers = _numberingService.GenerateNumbers(fresh.Nodes);
            return LegalDocumentMapping.BuildNodeTree(fresh.Nodes, numbers).SelectMany(Flatten).First(x => x.Id == node.Id);
        }

        public async Task<LegalDocumentNodeDto> UpdateNodeAsync(int nodeId, UpdateLegalDocumentNodeDto dto, string? userName, CancellationToken cancellationToken = default)
        {
            var node = await _db.LegalDocumentNodes
                .Include(x => x.LegalDocumentRevision)
                .FirstOrDefaultAsync(x => x.Id == nodeId, cancellationToken)
                ?? throw new KeyNotFoundException("Node not found.");

            EnsureDraft(node.LegalDocumentRevision);
            SetOriginalRowVersion(node, dto.RowVersion);

            node.BusinessCode = dto.BusinessCode;
            node.Title = string.IsNullOrWhiteSpace(dto.Title) ? LegalDocumentLexicon.GetNodeTypeLabel(node.Type) : dto.Title.Trim();
            node.EditorJson = dto.EditorJson;
            node.ContentHtml = dto.ContentHtml;
            node.PlainText = dto.PlainText;
            node.IncludeInTableOfContents = dto.IncludeInTableOfContents;
            node.StartOnNewPage = dto.StartOnNewPage;
            node.KeepWithNext = dto.KeepWithNext;
            node.NumberingStyle = dto.NumberingStyle;
            node.IsConditional = dto.IsConditional;
            node.DisplayConditionJson = dto.DisplayConditionJson;

            await _db.SaveChangesAsync(cancellationToken);
            await RefreshRevisionHashAsync(node.LegalDocumentRevisionId, cancellationToken);
            await _auditService.AddAsync(DocumentAuditAction.Updated, node.LegalDocumentRevision.LegalDocumentDefinitionId, node.LegalDocumentRevisionId, node.Id, new { node.Title }, userName, cancellationToken);

            var revision = await LoadRevisionWithNodesAsync(node.LegalDocumentRevisionId, tracking: false, cancellationToken)
                ?? throw new KeyNotFoundException("Revision not found.");
            var numbers = _numberingService.GenerateNumbers(revision.Nodes);
            return LegalDocumentMapping.BuildNodeTree(revision.Nodes, numbers).SelectMany(Flatten).First(x => x.Id == nodeId);
        }

        public async Task MoveNodeAsync(int nodeId, MoveLegalDocumentNodeDto dto, string? userName, CancellationToken cancellationToken = default)
        {
            var node = await _db.LegalDocumentNodes
                .Include(x => x.LegalDocumentRevision)
                .FirstOrDefaultAsync(x => x.Id == nodeId, cancellationToken)
                ?? throw new KeyNotFoundException("Node not found.");

            EnsureDraft(node.LegalDocumentRevision);
            SetOriginalRowVersion(node, dto.RowVersion);

            var revisionNodes = await _db.LegalDocumentNodes
                .Where(x => x.LegalDocumentRevisionId == node.LegalDocumentRevisionId)
                .ToListAsync(cancellationToken);

            var newParent = dto.NewParentNodeId is null
                ? null
                : revisionNodes.FirstOrDefault(x => x.Id == dto.NewParentNodeId.Value)
                    ?? throw new BusinessException("Target parent node is not part of this revision.");

            if (node.Type == DocumentNodeType.Document || IsDescendant(revisionNodes, newParent?.Id, node.Id))
            {
                throw new BusinessException("This move would create an invalid document tree.");
            }

            ValidateChildType(newParent?.Type, node.Type);
            node.ParentNodeId = dto.NewParentNodeId;
            node.SortOrder = ComputeMoveSortOrder(revisionNodes.Where(x => x.Id != node.Id), dto);

            await _db.SaveChangesAsync(cancellationToken);
            await RefreshRevisionHashAsync(node.LegalDocumentRevisionId, cancellationToken);
            await _auditService.AddAsync(DocumentAuditAction.Moved, node.LegalDocumentRevision.LegalDocumentDefinitionId, node.LegalDocumentRevisionId, node.Id, dto, userName, cancellationToken);
        }

        public async Task<LegalDocumentNodeDto> DuplicateSubtreeAsync(int nodeId, string? userName, CancellationToken cancellationToken = default)
        {
            var source = await _db.LegalDocumentNodes
                .Include(x => x.LegalDocumentRevision)
                .FirstOrDefaultAsync(x => x.Id == nodeId, cancellationToken)
                ?? throw new KeyNotFoundException("Node not found.");

            EnsureDraft(source.LegalDocumentRevision);

            var allNodes = await _db.LegalDocumentNodes
                .Where(x => x.LegalDocumentRevisionId == source.LegalDocumentRevisionId)
                .ToListAsync(cancellationToken);

            var cloneMap = new Dictionary<int, LegalDocumentNode>();
            LegalDocumentNode Clone(LegalDocumentNode node, int? parentId)
            {
                var copy = new LegalDocumentNode
                {
                    LegalDocumentRevisionId = node.LegalDocumentRevisionId,
                    ParentNodeId = parentId,
                    StableKey = Guid.NewGuid().ToString("N"),
                    Type = node.Type,
                    BusinessCode = node.BusinessCode,
                    Title = $"{node.Title} (copy)",
                    EditorJson = node.EditorJson,
                    ContentHtml = node.ContentHtml,
                    PlainText = node.PlainText,
                    SortOrder = node.Id == source.Id ? NextSortOrder(allNodes, source.ParentNodeId) : node.SortOrder,
                    IncludeInTableOfContents = node.IncludeInTableOfContents,
                    StartOnNewPage = node.StartOnNewPage,
                    KeepWithNext = node.KeepWithNext,
                    NumberingStyle = node.NumberingStyle,
                    IsConditional = node.IsConditional,
                    DisplayConditionJson = node.DisplayConditionJson,
                    SourceClauseRevisionId = node.SourceClauseRevisionId
                };
                cloneMap[node.Id] = copy;
                return copy;
            }

            var rootClone = Clone(source, source.ParentNodeId);
            _db.LegalDocumentNodes.Add(rootClone);
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var descendant in GetDescendants(allNodes, source.Id).OrderBy(x => x.SortOrder))
            {
                var parentId = cloneMap[descendant.ParentNodeId!.Value].Id;
                _db.LegalDocumentNodes.Add(Clone(descendant, parentId));
                await _db.SaveChangesAsync(cancellationToken);
            }

            await RefreshRevisionHashAsync(source.LegalDocumentRevisionId, cancellationToken);
            await _auditService.AddAsync(DocumentAuditAction.Duplicated, source.LegalDocumentRevision.LegalDocumentDefinitionId, source.LegalDocumentRevisionId, rootClone.Id, new { sourceNodeId = nodeId }, userName, cancellationToken);

            var revision = await LoadRevisionWithNodesAsync(source.LegalDocumentRevisionId, tracking: false, cancellationToken)
                ?? throw new KeyNotFoundException("Revision not found.");
            var numbers = _numberingService.GenerateNumbers(revision.Nodes);
            return LegalDocumentMapping.BuildNodeTree(revision.Nodes, numbers).SelectMany(Flatten).First(x => x.Id == rootClone.Id);
        }

        public async Task<IReadOnlyList<ReusableDocumentNodeDto>> GetReusableNodesAsync(
            int excludeRevisionId,
            DocumentNodeType? type,
            string? search,
            CancellationToken cancellationToken = default)
        {
            if (type is not null && !ReusableNodeTypes.Contains(type.Value))
            {
                return [];
            }

            var sourceRevisionIds = await _db.LegalDocumentDefinitions
                .AsNoTracking()
                .Where(x => x.IsActive && x.IsLibrary)
                .Select(x => x.CurrentDraftRevisionId ?? x.CurrentPublishedRevisionId)
                .Where(x => x.HasValue && x.Value != excludeRevisionId)
                .Select(x => x!.Value)
                .ToListAsync(cancellationToken);

            if (sourceRevisionIds.Count == 0)
            {
                return [];
            }

            var query = _db.LegalDocumentNodes
                .AsNoTracking()
                .Include(x => x.LegalDocumentRevision)
                .ThenInclude(x => x.LegalDocumentDefinition)
                .Where(x =>
                    sourceRevisionIds.Contains(x.LegalDocumentRevisionId) &&
                    (x.Type == DocumentNodeType.Chapter ||
                     x.Type == DocumentNodeType.Article ||
                     x.Type == DocumentNodeType.Paragraph));

            if (type is not null)
            {
                query = query.Where(x => x.Type == type.Value);
            }

            var normalizedSearch = search?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(x =>
                    x.Title.Contains(normalizedSearch) ||
                    (x.PlainText != null && x.PlainText.Contains(normalizedSearch)) ||
                    (x.BusinessCode != null && x.BusinessCode.Contains(normalizedSearch)));
            }

            var matches = await query
                .OrderBy(x => x.LegalDocumentRevision.LegalDocumentDefinition.Code)
                .ThenBy(x => x.Type)
                .ThenBy(x => x.Title)
                .Take(500)
                .ToListAsync(cancellationToken);

            var allSourceNodes = await _db.LegalDocumentNodes
                .AsNoTracking()
                .Where(x => sourceRevisionIds.Contains(x.LegalDocumentRevisionId))
                .Select(x => new { x.Id, x.ParentNodeId })
                .ToListAsync(cancellationToken);
            var childrenByParent = allSourceNodes
                .Where(x => x.ParentNodeId.HasValue)
                .ToLookup(x => x.ParentNodeId!.Value);

            int CountDescendants(int parentId) =>
                childrenByParent[parentId].Sum(child => 1 + CountDescendants(child.Id));

            return matches
                .Select(node => new ReusableDocumentNodeDto(
                    node.Id,
                    node.Type,
                    node.Title,
                    node.PlainText,
                    node.BusinessCode,
                    CountDescendants(node.Id),
                    node.LegalDocumentRevisionId,
                    node.LegalDocumentRevision.LegalDocumentDefinition.Code,
                    node.LegalDocumentRevision.LegalDocumentDefinition.Name,
                    node.LegalDocumentRevision.MajorVersion,
                    node.LegalDocumentRevision.MinorVersion,
                    node.LegalDocumentRevision.Status))
                .ToList();
        }

        public async Task<LegalDocumentNodeDto> ImportSubtreeAsync(
            int revisionId,
            ImportDocumentNodeDto dto,
            string? userName,
            CancellationToken cancellationToken = default)
        {
            var destination = await LoadRevisionWithNodesAsync(revisionId, tracking: true, cancellationToken)
                ?? throw new KeyNotFoundException("Destination revision not found.");
            EnsureDraft(destination);

            var destinationParent = destination.Nodes.FirstOrDefault(x => x.Id == dto.ParentNodeId)
                ?? throw new BusinessException("L'élément de destination n'appartient pas à cette révision.");

            var source = await _db.LegalDocumentNodes
                .AsNoTracking()
                .Include(x => x.LegalDocumentRevision)
                    .ThenInclude(x => x.LegalDocumentDefinition)
                .FirstOrDefaultAsync(x => x.Id == dto.SourceNodeId, cancellationToken)
                ?? throw new KeyNotFoundException("Source node not found.");

            if (!ReusableNodeTypes.Contains(source.Type))
            {
                throw new BusinessException("Ce type de contenu ne peut pas être réutilisé.");
            }

            if (source.LegalDocumentRevisionId == revisionId)
            {
                throw new BusinessException("Utilisez la duplication pour copier un élément dans la même révision.");
            }

            ValidateChildType(destinationParent.Type, source.Type);

            var sourceNodes = await _db.LegalDocumentNodes
                .AsNoTracking()
                .Where(x => x.LegalDocumentRevisionId == source.LegalDocumentRevisionId)
                .ToListAsync(cancellationToken);
            var sourceById = sourceNodes.ToDictionary(x => x.Id);
            var sourceChildren = sourceNodes.ToLookup(x => x.ParentNodeId);

            LegalDocumentNode CloneSubtree(LegalDocumentNode sourceNode, LegalDocumentNode? parent)
            {
                var clone = new LegalDocumentNode
                {
                    LegalDocumentRevisionId = revisionId,
                    ParentNodeId = parent is null ? destinationParent.Id : null,
                    ParentNode = parent,
                    StableKey = Guid.NewGuid().ToString("N"),
                    Type = sourceNode.Type,
                    BusinessCode = sourceNode.BusinessCode,
                    Title = sourceNode.Title,
                    EditorJson = sourceNode.EditorJson,
                    ContentHtml = sourceNode.ContentHtml,
                    PlainText = sourceNode.PlainText,
                    SortOrder = parent is null
                        ? NextSortOrder(destination.Nodes, destinationParent.Id)
                        : sourceNode.SortOrder,
                    IncludeInTableOfContents = sourceNode.IncludeInTableOfContents,
                    StartOnNewPage = sourceNode.StartOnNewPage,
                    KeepWithNext = sourceNode.KeepWithNext,
                    NumberingStyle = sourceNode.NumberingStyle,
                    IsConditional = sourceNode.IsConditional,
                    DisplayConditionJson = sourceNode.DisplayConditionJson,
                    SourceClauseRevisionId = sourceNode.SourceClauseRevisionId
                };

                foreach (var child in sourceChildren[sourceNode.Id].OrderBy(x => x.SortOrder))
                {
                    clone.Children.Add(CloneSubtree(sourceById[child.Id], clone));
                }

                return clone;
            }

            var importedRoot = CloneSubtree(source, null);
            _db.LegalDocumentNodes.Add(importedRoot);
            await _db.SaveChangesAsync(cancellationToken);
            await RefreshRevisionHashAsync(revisionId, cancellationToken);
            await _auditService.AddAsync(
                DocumentAuditAction.ContentImported,
                destination.LegalDocumentDefinitionId,
                revisionId,
                importedRoot.Id,
                new
                {
                    sourceNodeId = source.Id,
                    sourceRevisionId = source.LegalDocumentRevisionId,
                    sourceDocumentCode = source.LegalDocumentRevision.LegalDocumentDefinition.Code
                },
                userName,
                cancellationToken);

            var revision = await LoadRevisionWithNodesAsync(revisionId, tracking: false, cancellationToken)
                ?? throw new KeyNotFoundException("Destination revision not found.");
            var numbers = _numberingService.GenerateNumbers(revision.Nodes);
            return LegalDocumentMapping.BuildNodeTree(revision.Nodes, numbers)
                .SelectMany(Flatten)
                .First(x => x.Id == importedRoot.Id);
        }

        public async Task DeleteNodeAsync(int nodeId, string rowVersion, string? userName, CancellationToken cancellationToken = default)
        {
            var node = await _db.LegalDocumentNodes
                .Include(x => x.LegalDocumentRevision)
                .FirstOrDefaultAsync(x => x.Id == nodeId, cancellationToken)
                ?? throw new KeyNotFoundException("Node not found.");

            EnsureDraft(node.LegalDocumentRevision);
            if (node.Type == DocumentNodeType.Document)
            {
                throw new BusinessException("The document root cannot be deleted.");
            }

            SetOriginalRowVersion(node, rowVersion);
            var allNodes = await _db.LegalDocumentNodes
                .Where(x => x.LegalDocumentRevisionId == node.LegalDocumentRevisionId)
                .ToListAsync(cancellationToken);
            var subtree = GetDescendants(allNodes, node.Id).Append(node).ToList();
            var byId = allNodes.ToDictionary(x => x.Id);
            var subtreeIds = subtree.Select(x => x.Id).ToHashSet();
            var linkedAuditEvents = await _db.DocumentAuditEvents
                .Where(x => x.LegalDocumentNodeId.HasValue && subtreeIds.Contains(x.LegalDocumentNodeId.Value))
                .ToListAsync(cancellationToken);
            var orderedSubtree = subtree
                .OrderByDescending(x => GetNodeDepth(byId, x))
                .ThenByDescending(x => x.SortOrder)
                .ToList();

            foreach (var auditEvent in linkedAuditEvents)
            {
                auditEvent.LegalDocumentNodeId = null;
            }

            _db.LegalDocumentNodes.RemoveRange(orderedSubtree);
            await _db.SaveChangesAsync(cancellationToken);
            await RefreshRevisionHashAsync(node.LegalDocumentRevisionId, cancellationToken);
            await _auditService.AddAsync(
                DocumentAuditAction.Deleted,
                node.LegalDocumentRevision.LegalDocumentDefinitionId,
                node.LegalDocumentRevisionId,
                null,
                new { deletedNodeId = nodeId, count = subtree.Count },
                userName,
                cancellationToken);
        }

        internal static void ValidateChildType(DocumentNodeType? parentType, DocumentNodeType childType)
        {
            if (parentType is null)
            {
                if (childType != DocumentNodeType.Document)
                {
                    throw new BusinessException("Only the document root can exist without parent.");
                }

                return;
            }

            if (!AllowedChildren.TryGetValue(parentType.Value, out var allowed) || !allowed.Contains(childType))
            {
                throw new BusinessException($"{childType} cannot be added under {parentType}.");
            }
        }

        internal static bool IsAllowedChild(DocumentNodeType parentType, DocumentNodeType childType) =>
            AllowedChildren.TryGetValue(parentType, out var allowed) && allowed.Contains(childType);

        internal static void EnsureDraft(LegalDocumentRevision revision)
        {
            if (revision.Status != DocumentRevisionStatus.Draft)
            {
                throw new BusinessException("Only draft revisions can be modified.");
            }
        }

        private async Task<LegalDocumentRevision?> LoadRevisionWithNodesAsync(int revisionId, bool tracking, CancellationToken cancellationToken)
        {
            var query = _db.LegalDocumentRevisions
                .Include(x => x.LegalDocumentDefinition)
                .Include(x => x.DocumentLayoutTemplate)
                .Include(x => x.Nodes)
                .Where(x => x.Id == revisionId);

            if (!tracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<DocumentLayoutTemplate> GetOrCreateDefaultLayoutAsync(CancellationToken cancellationToken)
        {
            var layout = await _db.DocumentLayoutTemplates.FirstOrDefaultAsync(x => x.Code == "DEFAULT_A4" && x.IsActive, cancellationToken);
            if (layout is not null)
            {
                return layout;
            }

            layout = new DocumentLayoutTemplate
            {
                Code = "DEFAULT_A4",
                Name = "Default A4",
                Css = string.Empty,
                HeaderHtml = "<span class=\"doc-header-title\"></span>",
                FooterHtml = "<span class=\"pageNumber\"></span> / <span class=\"totalPages\"></span>"
            };
            _db.DocumentLayoutTemplates.Add(layout);
            await _db.SaveChangesAsync(cancellationToken);
            return layout;
        }

        private int ComputeInsertionSortOrder(IEnumerable<LegalDocumentNode> nodes, int? parentId, string? relativeNodeId, string? insertPosition)
        {
            if (int.TryParse(relativeNodeId, out var parsedRelativeId))
            {
                var siblings = nodes.Where(x => x.ParentNodeId == parentId).OrderBy(x => x.SortOrder).ToList();
                var relative = siblings.FirstOrDefault(x => x.Id == parsedRelativeId);
                if (relative is not null)
                {
                    var index = siblings.IndexOf(relative);
                    return string.Equals(insertPosition, "before", StringComparison.OrdinalIgnoreCase)
                        ? Between(siblings.ElementAtOrDefault(index - 1)?.SortOrder, relative.SortOrder)
                        : Between(relative.SortOrder, siblings.ElementAtOrDefault(index + 1)?.SortOrder);
                }
            }

            return NextSortOrder(nodes, parentId);
        }

        private static int ComputeMoveSortOrder(IEnumerable<LegalDocumentNode> nodes, MoveLegalDocumentNodeDto dto)
        {
            var siblings = nodes.Where(x => x.ParentNodeId == dto.NewParentNodeId).OrderBy(x => x.SortOrder).ToList();
            if (dto.First)
            {
                return Between(null, siblings.FirstOrDefault()?.SortOrder);
            }

            if (dto.Last || (dto.BeforeNodeId is null && dto.AfterNodeId is null))
            {
                return NextSortOrder(siblings, dto.NewParentNodeId);
            }

            if (dto.BeforeNodeId is not null)
            {
                var before = siblings.First(x => x.Id == dto.BeforeNodeId.Value);
                var index = siblings.IndexOf(before);
                return Between(siblings.ElementAtOrDefault(index - 1)?.SortOrder, before.SortOrder);
            }

            var after = siblings.First(x => x.Id == dto.AfterNodeId!.Value);
            var afterIndex = siblings.IndexOf(after);
            return Between(after.SortOrder, siblings.ElementAtOrDefault(afterIndex + 1)?.SortOrder);
        }

        private static int NextSortOrder(IEnumerable<LegalDocumentNode> nodes, int? parentId) =>
            (nodes.Where(x => x.ParentNodeId == parentId).Select(x => (int?)x.SortOrder).Max() ?? 0) + SortStep;

        private static int Between(int? previous, int? next)
        {
            if (previous is null && next is null)
            {
                return SortStep;
            }

            if (previous is null)
            {
                return Math.Max(SortStep, next!.Value / 2);
            }

            if (next is null)
            {
                return previous.Value + SortStep;
            }

            var distance = next.Value - previous.Value;
            return distance > 1 ? previous.Value + distance / 2 : previous.Value + 1;
        }

        private static bool IsDescendant(IEnumerable<LegalDocumentNode> nodes, int? candidateId, int ancestorId)
        {
            var currentId = candidateId;
            var byId = nodes.ToDictionary(x => x.Id);
            while (currentId is not null)
            {
                if (currentId.Value == ancestorId)
                {
                    return true;
                }

                currentId = byId.TryGetValue(currentId.Value, out var current) ? current.ParentNodeId : null;
            }

            return false;
        }

        private static IEnumerable<LegalDocumentNode> GetDescendants(IEnumerable<LegalDocumentNode> nodes, int parentId)
        {
            var byParent = nodes.GroupBy(x => x.ParentNodeId ?? 0).ToDictionary(x => x.Key, x => x.ToList());
            if (!byParent.TryGetValue(parentId, out var children))
            {
                yield break;
            }

            foreach (var child in children)
            {
                yield return child;
                foreach (var descendant in GetDescendants(nodes, child.Id))
                {
                    yield return descendant;
                }
            }
        }

        private static int GetNodeDepth(IReadOnlyDictionary<int, LegalDocumentNode> byId, LegalDocumentNode node)
        {
            var depth = 0;
            var current = node;
            while (current.ParentNodeId is not null && byId.TryGetValue(current.ParentNodeId.Value, out var parent))
            {
                depth++;
                current = parent;
            }

            return depth;
        }

        private static IEnumerable<LegalDocumentNodeDto> Flatten(LegalDocumentNodeDto node)
        {
            yield return node;
            foreach (var child in node.Children.SelectMany(Flatten))
            {
                yield return child;
            }
        }

        private void SetOriginalRowVersion(LegalDocumentNode node, string rowVersion)
        {
            if (!string.IsNullOrWhiteSpace(rowVersion))
            {
                _db.Entry(node).Property(x => x.RowVersion).OriginalValue = LegalDocumentMapping.FromRowVersion(rowVersion);
            }
        }

        private async Task RefreshRevisionHashAsync(int revisionId, CancellationToken cancellationToken)
        {
            var revision = await _db.LegalDocumentRevisions.FirstAsync(x => x.Id == revisionId, cancellationToken);
            revision.ContentHash = await ComputeRevisionHashAsync(revisionId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<string> ComputeRevisionHashAsync(int revisionId, CancellationToken cancellationToken)
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

            var json = System.Text.Json.JsonSerializer.Serialize(nodes);
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(bytes);
        }
    }
}
