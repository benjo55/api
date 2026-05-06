using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Generic;
using api.Dtos.Brand;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using api.Services;
using Mapster;

namespace api.Repository
{
    public class BrandRepository : IBrandRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly EntityHistoryService _entityHistoryService;  // Service d'historisation
        public BrandRepository(ApplicationDBContext context, EntityHistoryService entityHistoryService)
        {
            _context = context;
            _entityHistoryService = entityHistoryService;
        }

        public async Task<Brand> CreateAsync(Brand brandModel)
        {
            await _context.Brands.AddAsync(brandModel);
            await _context.SaveChangesAsync();
            return brandModel;
        }

        public async Task<Brand?> DeleteAsync(int id)
        {
            var brandModel = await _context.Brands.FirstOrDefaultAsync(c => c.Id == id);
            if (brandModel == null) return null;
            _context.Brands.Remove(brandModel);
            await _context.SaveChangesAsync();
            return brandModel;
        }

        public async Task<PagedResult<Brand>> GetAllAsync(QueryObject query)
        {
            var brands = _context.Brands.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                brands = brands.Where(p => p.BrandCode.Contains(query.Search) || p.BrandName.Contains(query.Search));
            }
            brands.OrderBy(p => p.BrandCode);

            // Calcul du total avant pagination
            var totalCount = await brands.CountAsync();

            // Calcul du nombre total de pages
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            // Pagination
            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            var pagedBrands = await brands.Skip(skipNumber).Take(query.PageSize).ToListAsync();

            // Indique s'il reste une page après celle-ci
            var hasNextPage = query.PageNumber < totalPages;

            // Retour des résultats
            return new PagedResult<Brand>
            {
                Items = pagedBrands,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                CurrentPage = query.PageNumber
            };
        }

        public async Task<Brand?> GetByIdAsync(int id)
        {
            return await _context.Brands.FindAsync(id);
        }

        public async Task<Brand?> UpdateAsync(int id, UpdateBrandRequestDto updateBrandDto)
        {
            var existingBrand = await _context.Brands.FindAsync(id);
            if (existingBrand == null) return null;

            // Sauvegarde de l'état initial pour l'historique
            var originalBrand = new Brand();
            existingBrand.Adapt(originalBrand);

            // Mise à jour avec Mapster
            updateBrandDto.Adapt(existingBrand);
            existingBrand.UpdatedDate = DateTime.UtcNow;

            // Historisation des changements
            await _entityHistoryService.TrackChangesAsync(originalBrand, existingBrand, "Admin"); // Remplace "Admin" par l'utilisateur courant

            await _context.SaveChangesAsync();
            return existingBrand;
        }

        public async Task<Brand?> PatchLockedAsync(int id, bool locked)
        {
            var brand = await _context.Brands.FirstOrDefaultAsync(c => c.Id == id);
            if (brand == null)
                return null;

            var original = new Brand();
            brand.Adapt(original);

            brand.Locked = locked;
            brand.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Historisation
            await _entityHistoryService.TrackChangesAsync(original, brand, "Admin");

            return brand;
        }
    }
}
