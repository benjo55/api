using api.Data;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class ContractOptionTypeRepository : IContractOptionTypeRepository
    {
        private readonly ApplicationDBContext _context;

        public ContractOptionTypeRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ContractOptionType>> GetAllAsync()
        {
            return await _context.ContractOptionTypes
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ContractOptionType?> GetByIdAsync(int id)
        {
            return await _context.ContractOptionTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ContractOptionType> CreateAsync(ContractOptionType optionType)
        {
            _context.ContractOptionTypes.Add(optionType);
            await _context.SaveChangesAsync();
            return optionType;
        }

        public async Task<ContractOptionType?> UpdateAsync(int id, ContractOptionType optionType)
        {
            var existing = await _context.ContractOptionTypes.FindAsync(id);
            if (existing == null) return null;

            existing.Code = optionType.Code;
            existing.Category = optionType.Category;
            existing.Label = optionType.Label;
            existing.Objective = optionType.Objective;
            existing.Mechanism = optionType.Mechanism;
            existing.DefaultCost = optionType.DefaultCost;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ContractOptionTypes.FindAsync(id);
            if (entity == null) return false;

            _context.ContractOptionTypes.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CountAsync()
        {
            return await _context.ContractOptionTypes.CountAsync();
        }
    }
}
