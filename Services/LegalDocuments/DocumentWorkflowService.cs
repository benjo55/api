using api.Data;
using api.Dtos.LegalDocuments;
using api.Exceptions;
using api.Interfaces;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.LegalDocuments
{
    public sealed class DocumentWorkflowService : IDocumentWorkflowService
    {
        private readonly ApplicationDBContext _db;
        private readonly IDocumentValidationService _validationService;
        private readonly IDocumentRenderService _renderService;
        private readonly IDocumentNumberingService _numberingService;
        private readonly IDocumentAuditService _auditService;

        public DocumentWorkflowService(
            ApplicationDBContext db,
            IDocumentValidationService validationService,
            IDocumentRenderService renderService,
            IDocumentNumberingService numberingService,
            IDocumentAuditService auditService)
        {
            _db = db;
            _validationService = validationService;
            _renderService = renderService;
            _numberingService = numberingService;
            _auditService = auditService;
        }

        public async Task<LegalDocumentRevisionDto> SubmitForReviewAsync(int revisionId, WorkflowTransitionDto dto, string? userName, CancellationToken cancellationToken = default)
        {
            var revision = await LoadRevisionAsync(revisionId, cancellationToken);
            DocumentStructureService.EnsureDraft(revision);
            SetOriginalRowVersion(revision, dto.RowVersion);
            revision.Status = DocumentRevisionStatus.InReview;
            revision.ValidationComment = dto.Comment;
            await _db.SaveChangesAsync(cancellationToken);
            await _auditService.AddAsync(DocumentAuditAction.Submitted, revision.LegalDocumentDefinitionId, revision.Id, null, new { dto.Comment }, userName, cancellationToken);
            return await ToDtoAsync(revision.Id, cancellationToken);
        }

        public async Task<DocumentValidationResultDto> ValidateAsync(int revisionId, WorkflowTransitionDto dto, string? userName, CancellationToken cancellationToken = default)
        {
            var revision = await LoadRevisionAsync(revisionId, cancellationToken);
            if (revision.Status is not DocumentRevisionStatus.InReview and not DocumentRevisionStatus.Draft)
            {
                throw new BusinessException("Only draft or in-review revisions can be validated.");
            }

            SetOriginalRowVersion(revision, dto.RowVersion);
            var result = await _validationService.ValidateRevisionAsync(revisionId, includePdfGeneration: true, cancellationToken);
            if (!result.IsValid)
            {
                return result;
            }

            revision.Status = DocumentRevisionStatus.Validated;
            revision.ValidationComment = dto.Comment;
            revision.ValidatedAt = DateTime.UtcNow;
            revision.ValidatedBy = userName;
            await _db.SaveChangesAsync(cancellationToken);
            await _renderService.GeneratePreviewAsync(revisionId, LegalDocumentMapping.ToRowVersion(revision.RowVersion), userName, cancellationToken);
            await _auditService.AddAsync(DocumentAuditAction.Validated, revision.LegalDocumentDefinitionId, revision.Id, null, new { dto.Comment }, userName, cancellationToken);
            return result;
        }

        public async Task<LegalDocumentRevisionDto> PublishAsync(int revisionId, WorkflowTransitionDto dto, string? userName, CancellationToken cancellationToken = default)
        {
            var revision = await LoadRevisionAsync(revisionId, cancellationToken);
            if (revision.Status != DocumentRevisionStatus.Validated)
            {
                throw new BusinessException("Only validated revisions can be published.");
            }

            SetOriginalRowVersion(revision, dto.RowVersion);
            var definition = await _db.LegalDocumentDefinitions
                .Include(x => x.CurrentPublishedRevision)
                .FirstAsync(x => x.Id == revision.LegalDocumentDefinitionId, cancellationToken);

            if (definition.CurrentPublishedRevision is not null && definition.CurrentPublishedRevision.Id != revision.Id)
            {
                definition.CurrentPublishedRevision.Status = DocumentRevisionStatus.Superseded;
            }

            revision.Status = DocumentRevisionStatus.Published;
            revision.PublishedAt = DateTime.UtcNow;
            revision.PublishedBy = userName;
            definition.CurrentPublishedRevisionId = revision.Id;
            if (definition.CurrentDraftRevisionId == revision.Id)
            {
                definition.CurrentDraftRevisionId = null;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await _auditService.AddAsync(DocumentAuditAction.Published, definition.Id, revision.Id, null, new { dto.Comment }, userName, cancellationToken);
            return await ToDtoAsync(revision.Id, cancellationToken);
        }

        private async Task<api.Models.LegalDocumentRevision> LoadRevisionAsync(int revisionId, CancellationToken cancellationToken) =>
            await _db.LegalDocumentRevisions
                .Include(x => x.Nodes)
                .FirstOrDefaultAsync(x => x.Id == revisionId, cancellationToken)
            ?? throw new KeyNotFoundException("Revision not found.");

        private void SetOriginalRowVersion(api.Models.LegalDocumentRevision revision, string rowVersion)
        {
            if (!string.IsNullOrWhiteSpace(rowVersion))
            {
                _db.Entry(revision).Property(x => x.RowVersion).OriginalValue = LegalDocumentMapping.FromRowVersion(rowVersion);
            }
        }

        private async Task<LegalDocumentRevisionDto> ToDtoAsync(int revisionId, CancellationToken cancellationToken)
        {
            var revision = await _db.LegalDocumentRevisions
                .AsNoTracking()
                .Include(x => x.LegalDocumentDefinition)
                .Include(x => x.Nodes)
                .FirstAsync(x => x.Id == revisionId, cancellationToken);
            var numbers = _numberingService.GenerateNumbers(revision.Nodes);
            return LegalDocumentMapping.ToDto(revision, numbers);
        }
    }
}
