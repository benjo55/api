using api.Data;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services.LegalDocuments
{
    public sealed class ClauseCatalogService : IClauseCatalogService
    {
        private readonly ApplicationDBContext _db;

        public ClauseCatalogService(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ClauseDefinition>> GetClausesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.ClauseDefinitions
                .AsNoTracking()
                .Include(x => x.Revisions)
                .Where(x => x.IsActive)
                .OrderBy(x => x.Code)
                .ToListAsync(cancellationToken);
        }
    }
}
