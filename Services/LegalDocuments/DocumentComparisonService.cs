using api.Data;
using api.Dtos.LegalDocuments;
using api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.Services.LegalDocuments
{
    public sealed class DocumentComparisonService : IDocumentComparisonService
    {
        private readonly ApplicationDBContext _db;

        public DocumentComparisonService(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<RevisionComparisonDto> CompareAsync(int leftRevisionId, int rightRevisionId, CancellationToken cancellationToken = default)
        {
            var left = await _db.LegalDocumentNodes
                .AsNoTracking()
                .Where(x => x.LegalDocumentRevisionId == leftRevisionId)
                .ToDictionaryAsync(x => x.StableKey, cancellationToken);

            var right = await _db.LegalDocumentNodes
                .AsNoTracking()
                .Where(x => x.LegalDocumentRevisionId == rightRevisionId)
                .ToDictionaryAsync(x => x.StableKey, cancellationToken);

            var added = right.Keys.Except(left.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            var removed = left.Keys.Except(right.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            var changed = left.Keys.Intersect(right.Keys, StringComparer.OrdinalIgnoreCase)
                .Where(key => left[key].Title != right[key].Title ||
                              left[key].ContentHtml != right[key].ContentHtml ||
                              left[key].ParentNodeId != right[key].ParentNodeId ||
                              left[key].SortOrder != right[key].SortOrder)
                .OrderBy(x => x)
                .ToList();

            return new RevisionComparisonDto(leftRevisionId, rightRevisionId, added, removed, changed);
        }
    }
}
