using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Contract;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
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
                entities = entities.Where(e =>
                    e.EntityName.Contains(query.Search) ||
                    e.FieldName.Contains(query.Search));

            var totalCount = await entities.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            var items = await entities
                .OrderBy(e => e.Id)
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
            return await _context.FieldDescriptions
                .Where(f => f.EntityName.ToLower() == entityName.ToLower())
                .ToListAsync();
        }

        public async Task<FieldDescription> CreateAsync(FieldDescription fieldDescription)
        {
            _context.FieldDescriptions.Add(fieldDescription);
            await _context.SaveChangesAsync();
            return fieldDescription;
        }

        public async Task<FieldDescription?> UpdateAsync(int id, FieldDescription fieldDescription)
        {
            var existing = await _context.FieldDescriptions.FindAsync(id);
            if (existing == null) return null;
            existing.Description = fieldDescription.Description;
            existing.FieldName = fieldDescription.FieldName;
            existing.EntityName = fieldDescription.EntityName;
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
    }
}