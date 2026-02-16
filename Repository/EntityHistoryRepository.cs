using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class EntityHistoryRepository : IEntityHistoryRepository
    {
        private readonly ApplicationDBContext _context;
        public EntityHistoryRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task SaveEntityHistoryAsync(EntityHistory history)
        {
            await _context.EntityHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<EntityHistory>> GetHistoryForEntityAsync(string entityName, int entityId)
        {
            return await _context.EntityHistories.Where(e => e.EntityName == entityName && e.EntityId == entityId).ToListAsync();
        }
    }
}
