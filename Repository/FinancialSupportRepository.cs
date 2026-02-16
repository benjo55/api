using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.FinancialSupport;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using api.Services;
using AutoMapper;

namespace api.Repository
{
    public class FinancialSupportRepository : IFinancialSupportRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly EntityHistoryService _entityHistoryService;
        private readonly IMapper _mapper;

        public FinancialSupportRepository(ApplicationDBContext context, EntityHistoryService entityHistoryService, IMapper mapper)
        {
            _context = context;
            _entityHistoryService = entityHistoryService;
            _mapper = mapper;
        }

        public async Task<PagedResult<FinancialSupportDto>> GetAllAsync(QueryObject query)
        {
            var supports = _context.FinancialSupports.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                supports = supports.Where(s =>
                    s.Code.Contains(query.Search) ||
                    s.Label.Contains(query.Search));
            }

            supports = supports.OrderByDescending(s => s.CreatedDate);

            var totalCount = await supports.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            var paged = await supports.Skip(skipNumber).Take(query.PageSize).ToListAsync();

            var dtos = paged.Select(_mapper.Map<FinancialSupportDto>).ToList();

            return new PagedResult<FinancialSupportDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = query.PageNumber < totalPages,
                CurrentPage = query.PageNumber
            };
        }

        public async Task<FinancialSupportDto?> GetByIdAsync(int id)
        {
            var entity = await _context.FinancialSupports.FindAsync(id);
            return entity == null ? null : _mapper.Map<FinancialSupportDto>(entity);
        }

        public async Task<FinancialSupportDto?> GetByCodeAsync(string code)
        {
            var entity = await _context.FinancialSupports
                .FirstOrDefaultAsync(fs => fs.Code == code);
            return entity == null ? null : _mapper.Map<FinancialSupportDto>(entity);
        }

        public async Task<FinancialSupportDto> CreateAsync(CreateFinancialSupportRequestDto createDto)
        {
            var entity = _mapper.Map<FinancialSupport>(createDto);
            entity.CreatedDate = DateTime.UtcNow;
            entity.UpdatedDate = DateTime.UtcNow;
            await _context.FinancialSupports.AddAsync(entity);
            await _context.SaveChangesAsync();
            return _mapper.Map<FinancialSupportDto>(entity);
        }

        public async Task<bool> AnyByIsinAsync(string isin)
        {
            // Si ISIN peut être null dans la base, adapte cette ligne
            return await _context.FinancialSupports
                .AnyAsync(s => s.ISIN != null && s.ISIN.ToUpper() == isin);
        }

        public async Task<FinancialSupportDto?> UpdateAsync(int id, UpdateFinancialSupportRequestDto updateDto)
        {
            var existing = await _context.FinancialSupports.FindAsync(id);
            if (existing == null) return null;

            var original = new FinancialSupport();
            _mapper.Map(existing, original);

            _mapper.Map(updateDto, existing);
            existing.UpdatedDate = DateTime.UtcNow;

            await _entityHistoryService.TrackChangesAsync(original, existing, "Admin");
            await _context.SaveChangesAsync();

            return _mapper.Map<FinancialSupportDto>(existing);
        }

        public async Task<FinancialSupportDto?> DeleteAsync(int id)
        {
            var entity = await _context.FinancialSupports.FindAsync(id);
            if (entity == null) return null;

            _context.FinancialSupports.Remove(entity);
            await _context.SaveChangesAsync();
            return _mapper.Map<FinancialSupportDto>(entity);
        }

        public async Task<List<FinancialSupportDto>> TypeaheadAsync(string search)
        {
            return await _context.FinancialSupports
                .Where(s => s.ISIN.Contains(search)
                    || s.Label.Contains(search)
                    || s.Code.Contains(search))
                .OrderBy(s => s.Label)
                .Take(10) // limite pour le typeahead
                .Select(s => _mapper.Map<FinancialSupportDto>(s))
                .ToListAsync();
        }
    }
}
