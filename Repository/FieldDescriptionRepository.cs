using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class FieldDescriptionRepository : IFieldDescriptionRepository
    {
        private readonly ApplicationDBContext _context;

        public FieldDescriptionRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<FieldDescription>> GetAllAsync(QueryObject query)
        {
            var entities = _context.FieldDescriptions.AsQueryable();

            // Gestion de la recherche simple sur entityName ou fieldName
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                entities = entities.Where(e =>
                    e.EntityName.Contains(search) ||
                    e.FieldName.Contains(search) ||
                    e.Description.Contains(search));
            }

            var totalCount = await entities.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            var items = await entities
                .OrderBy(e => e.EntityName)
                .ThenBy(e => e.FieldName)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<FieldDescription>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = query.PageNumber < totalPages,
                CurrentPage = query.PageNumber
            };
        }


        public async Task<FieldDescription?> GetByIdAsync(int id) =>
            await _context.FieldDescriptions.FindAsync(id);

        public async Task<IEnumerable<FieldDescription>> GetByEntityNameAsync(string entityName)
        {
            var normalizedEntityName = NormalizeKey(entityName);
            return await _context.FieldDescriptions
                .Where(f => f.EntityName.ToLower() == normalizedEntityName.ToLower())
                .OrderBy(f => f.FieldName)
                .ToListAsync();
        }

        public async Task<FieldDescription> CreateAsync(FieldDescription fieldDescription)
        {
            fieldDescription.EntityName = NormalizeKey(fieldDescription.EntityName);
            fieldDescription.FieldName = NormalizeKey(fieldDescription.FieldName);
            fieldDescription.Description = fieldDescription.Description.Trim();
            fieldDescription.CreatedDate = DateTime.UtcNow;
            fieldDescription.UpdatedDate = DateTime.UtcNow;

            var duplicate = await _context.FieldDescriptions
                .AnyAsync(f =>
                    f.EntityName.ToLower() == fieldDescription.EntityName.ToLower() &&
                    f.FieldName.ToLower() == fieldDescription.FieldName.ToLower());
            if (duplicate)
                throw new InvalidOperationException(
                    $"Une description existe déjà pour {fieldDescription.EntityName}.{fieldDescription.FieldName}.");

            _context.FieldDescriptions.Add(fieldDescription);
            await _context.SaveChangesAsync();
            return fieldDescription;
        }

        public async Task<FieldDescription?> UpdateAsync(int id, FieldDescription fieldDescription)
        {
            var existing = await _context.FieldDescriptions.FindAsync(id);
            if (existing == null) return null;
            var entityName = NormalizeKey(fieldDescription.EntityName);
            var fieldName = NormalizeKey(fieldDescription.FieldName);
            var duplicate = await _context.FieldDescriptions
                .AnyAsync(f =>
                    f.Id != id &&
                    f.EntityName.ToLower() == entityName.ToLower() &&
                    f.FieldName.ToLower() == fieldName.ToLower());
            if (duplicate)
            {
                throw new InvalidOperationException(
                    $"Une description existe déjà pour {entityName}.{fieldName}.");
            }

            existing.Description = fieldDescription.Description.Trim();
            existing.FieldName = fieldName;
            existing.EntityName = entityName;
            existing.UpdatedDate = DateTime.UtcNow;
            existing.Locked = fieldDescription.Locked;
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<FieldDescription?> DeleteAsync(int id)
        {
            var existing = await _context.FieldDescriptions.FindAsync(id);
            if (existing == null) return null;
            _context.FieldDescriptions.Remove(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        private static string NormalizeKey(string value)
        {
            return value.Trim();
        }
    }
}
