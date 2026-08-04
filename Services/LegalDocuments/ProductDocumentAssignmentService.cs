using api.Data;
using api.Dtos.LegalDocuments;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.LegalDocuments
{
    public sealed class ProductDocumentAssignmentService : IProductDocumentAssignmentService
    {
        private readonly ApplicationDBContext _db;
        private readonly IDocumentAuditService _auditService;

        public ProductDocumentAssignmentService(ApplicationDBContext db, IDocumentAuditService auditService)
        {
            _db = db;
            _auditService = auditService;
        }

        public async Task<IReadOnlyList<ProductDocumentAssignmentDto>> GetProductAssignmentsAsync(int productId, CancellationToken cancellationToken = default)
        {
            var assignments = await _db.ProductDocumentAssignments
                .AsNoTracking()
                .Include(x => x.LegalDocumentRevision)
                    .ThenInclude(x => x.LegalDocumentDefinition)
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.IsCurrent)
                .ThenBy(x => x.Role)
                .ThenByDescending(x => x.ValidFrom)
                .ToListAsync(cancellationToken);

            var revisionIds = assignments.Select(x => x.LegalDocumentRevisionId).Distinct().ToList();
            var latestArtifacts = await _db.DocumentArtifacts
                .AsNoTracking()
                .Where(a => a.LegalDocumentRevisionId.HasValue &&
                            revisionIds.Contains(a.LegalDocumentRevisionId.Value) &&
                            (a.Type == DocumentArtifactType.ValidatedPdf || a.Type == DocumentArtifactType.PreviewPdf))
                .GroupBy(a => a.LegalDocumentRevisionId!.Value)
                .Select(g => new
                {
                    RevisionId = g.Key,
                    ArtifactId = g
                        .OrderByDescending(a => a.Type == DocumentArtifactType.ValidatedPdf)
                        .ThenByDescending(a => a.GeneratedAt)
                        .Select(a => (int?)a.Id)
                        .FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.RevisionId, x => x.ArtifactId, cancellationToken);

            return assignments
                .Select(x => new ProductDocumentAssignmentDto(
                    x.Id,
                    x.ProductId,
                    x.LegalDocumentRevisionId,
                    x.LegalDocumentRevision.LegalDocumentDefinition.Code,
                    x.LegalDocumentRevision.LegalDocumentDefinition.Name,
                    x.LegalDocumentRevision.MajorVersion,
                    x.LegalDocumentRevision.MinorVersion,
                    x.Role,
                    x.ValidFrom,
                    x.ValidTo,
                    x.IsCurrent,
                    latestArtifacts.GetValueOrDefault(x.LegalDocumentRevisionId),
                    LegalDocumentMapping.ToRowVersion(x.RowVersion)))
                .ToList();
        }

        public async Task<ProductDocumentAssignmentDto> AssignAsync(CreateProductDocumentAssignmentDto dto, string? userName, CancellationToken cancellationToken = default)
        {
            if (dto.ValidTo is not null && dto.ValidTo.Value.Date < dto.ValidFrom.Date)
            {
                throw new BusinessException("Assignment end date cannot be before start date.");
            }

            var productExists = await _db.Products.AnyAsync(x => x.Id == dto.ProductId, cancellationToken);
            if (!productExists)
            {
                throw new KeyNotFoundException("Product not found.");
            }

            var revision = await _db.LegalDocumentRevisions
                .Include(x => x.LegalDocumentDefinition)
                .FirstOrDefaultAsync(x => x.Id == dto.LegalDocumentRevisionId, cancellationToken)
                ?? throw new KeyNotFoundException("Legal document revision not found.");

            if (revision.Status != DocumentRevisionStatus.Published)
            {
                throw new BusinessException("Only published legal document revisions can be assigned to a product.");
            }

            if (dto.Role == ProductDocumentRole.GeneralTerms &&
                revision.LegalDocumentDefinition.Type != LegalDocumentType.ProductGeneralTerms)
            {
                throw new BusinessException("General terms assignments require a ProductGeneralTerms document.");
            }

            if (await HasOverlapAsync(dto.ProductId, dto.Role, dto.ValidFrom, dto.ValidTo, excludedAssignmentId: null, cancellationToken))
            {
                throw new BusinessException("Product document assignment periods cannot overlap for the same role.");
            }

            if (dto.IsCurrent)
            {
                var currentAssignments = await _db.ProductDocumentAssignments
                    .Where(x => x.ProductId == dto.ProductId && x.Role == dto.Role && x.IsCurrent)
                    .ToListAsync(cancellationToken);
                foreach (var current in currentAssignments)
                {
                    current.IsCurrent = false;
                }
            }

            var assignment = new ProductDocumentAssignment
            {
                ProductId = dto.ProductId,
                LegalDocumentRevisionId = dto.LegalDocumentRevisionId,
                Role = dto.Role,
                ValidFrom = dto.ValidFrom.Date,
                ValidTo = dto.ValidTo?.Date,
                IsCurrent = dto.IsCurrent
            };

            _db.ProductDocumentAssignments.Add(assignment);
            await _db.SaveChangesAsync(cancellationToken);
            await _auditService.AddAsync(
                DocumentAuditAction.ProductAssigned,
                revision.LegalDocumentDefinitionId,
                revision.Id,
                null,
                new { assignment.ProductId, assignment.Role, assignment.ValidFrom, assignment.ValidTo },
                userName,
                cancellationToken);

            return (await GetProductAssignmentsAsync(dto.ProductId, cancellationToken)).First(x => x.Id == assignment.Id);
        }

        public async Task DeleteAsync(int assignmentId, string rowVersion, string? userName, CancellationToken cancellationToken = default)
        {
            var assignment = await _db.ProductDocumentAssignments
                .Include(x => x.LegalDocumentRevision)
                .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken)
                ?? throw new KeyNotFoundException("Product document assignment not found.");

            if (!string.IsNullOrWhiteSpace(rowVersion))
            {
                _db.Entry(assignment).Property(x => x.RowVersion).OriginalValue = LegalDocumentMapping.FromRowVersion(rowVersion);
            }

            _db.ProductDocumentAssignments.Remove(assignment);
            await _db.SaveChangesAsync(cancellationToken);
            await _auditService.AddAsync(
                DocumentAuditAction.ProductAssigned,
                null,
                assignment.LegalDocumentRevisionId,
                null,
                new { removedAssignmentId = assignmentId, assignment.ProductId, assignment.Role },
                userName,
                cancellationToken);
        }

        private async Task<bool> HasOverlapAsync(
            int productId,
            ProductDocumentRole role,
            DateTime validFrom,
            DateTime? validTo,
            int? excludedAssignmentId,
            CancellationToken cancellationToken)
        {
            var newStart = validFrom.Date;
            var newEnd = validTo?.Date ?? DateTime.MaxValue.Date;

            return await _db.ProductDocumentAssignments
                .Where(x => x.ProductId == productId && x.Role == role)
                .Where(x => excludedAssignmentId == null || x.Id != excludedAssignmentId.Value)
                .AnyAsync(x => x.ValidFrom <= newEnd && (x.ValidTo ?? DateTime.MaxValue.Date) >= newStart, cancellationToken);
        }
    }
}
