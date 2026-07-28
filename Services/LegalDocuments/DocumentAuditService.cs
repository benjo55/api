using System.Text.Json;
using api.Data;
using api.Dtos.LegalDocuments;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.LegalDocuments
{
    public sealed class DocumentAuditService : IDocumentAuditService
    {
        private readonly ApplicationDBContext _db;

        public DocumentAuditService(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task AddAsync(DocumentAuditAction action, int? definitionId, int? revisionId, int? nodeId, object? details, string? userName, CancellationToken cancellationToken = default)
        {
            _db.DocumentAuditEvents.Add(new DocumentAuditEvent
            {
                Action = action,
                LegalDocumentDefinitionId = definitionId,
                LegalDocumentRevisionId = revisionId,
                LegalDocumentNodeId = nodeId,
                DetailJson = details is null ? null : JsonSerializer.Serialize(details),
                CreatedBy = userName
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DocumentAuditEventDto>> GetHistoryAsync(int revisionId, CancellationToken cancellationToken = default)
        {
            return await _db.DocumentAuditEvents
                .AsNoTracking()
                .Where(x => x.LegalDocumentRevisionId == revisionId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new DocumentAuditEventDto(
                    x.Id,
                    x.Action,
                    x.LegalDocumentRevisionId,
                    x.LegalDocumentNodeId,
                    x.DetailJson,
                    x.CreatedAt,
                    x.CreatedBy))
                .ToListAsync(cancellationToken);
        }
    }
}
