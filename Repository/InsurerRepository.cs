using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Insurer;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Services;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace api.Repository
{
    public class InsurerRepository : IInsurerRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly EntityHistoryService _entityHistoryService;  // Service d'historisation
        private readonly IMapper _mapper; // AutoMapper pour éviter d'affecter manuellement les champs

        public InsurerRepository(ApplicationDBContext context, EntityHistoryService entityHistoryService, IMapper mapper)
        {
            _context = context;
            _entityHistoryService = entityHistoryService;
            _mapper = mapper;
        }

        public async Task<Insurer> CreateAsync(Insurer InsurerModel)
        {
            await _context.Insurers.AddAsync(InsurerModel);
            await _context.SaveChangesAsync();
            return InsurerModel;
        }

        public async Task<Insurer?> DeleteAsync(int id)
        {
            try
            {
                var InsurerModel = await _context.Insurers.FirstOrDefaultAsync(p => p.Id == id);
                if (InsurerModel == null) return null;
                _context.Insurers.Remove(InsurerModel);
                await _context.SaveChangesAsync();
                return InsurerModel;
            }
            catch (DbUpdateException ex) when ((ex.InnerException as SqlException)?.Number == 547)
            {
                throw new InvalidOperationException("Impossible de supprimer cet assureur car il est référencé par un ou plusieurs produits.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Une erreur inattendue s'est produite lors de la suppression.", ex);
            }

        }

        public async Task<PagedResult<Insurer>> GetAllAsync(QueryObject query)
        {
            var Insurers = _context.Insurers.AsQueryable();
            Insurers = Insurers.OrderByDescending(p => p.CreatedDate);

            // Calcul du total avant pagination
            var totalCount = await Insurers.CountAsync();

            // Calcul du nombre total de pages
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            // Pagination
            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            var pagedInsurers = await Insurers.Skip(skipNumber).Take(query.PageSize).ToListAsync();

            // Indique s'il reste une page après celle-ci
            var hasNextPage = query.PageNumber < totalPages;

            // Retour des résultats
            return new PagedResult<Insurer>
            {
                Items = pagedInsurers,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                CurrentPage = query.PageNumber
            };
        }
        public async Task<Insurer?> GetByIdAsync(int id)
        {
            return await _context.Insurers.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> InsurerExists(int id)
        {
            return await _context.Insurers.AnyAsync(p => p.Id == id);
        }
        public async Task<Insurer?> UpdateAsync(int id, UpdateInsurerRequestDto updateInsurerDto)
        {
            var existingInsurer = await _context.Insurers.FirstOrDefaultAsync(p => p.Id == id);
            if (existingInsurer == null) return null;

            // 1️⃣ Cloner l'état initial pour l'historisation
            var originalInsurer = new Insurer();
            _mapper.Map(existingInsurer, originalInsurer);

            // 2️⃣ Mise à jour automatique avec AutoMapper
            _mapper.Map(updateInsurerDto, existingInsurer);
            existingInsurer.UpdatedDate = DateTime.UtcNow;

            // 3️⃣ Historisation des changements
            await _entityHistoryService.TrackChangesAsync(originalInsurer, existingInsurer, "Admin"); // Remplace "Admin" par l'utilisateur courant

            // 4️⃣ Sauvegarde
            await _context.SaveChangesAsync();
            return existingInsurer;
        }

        public async Task<Insurer?> PatchLockedAsync(int id, bool locked)
        {
            var insurer = await _context.Insurers.FirstOrDefaultAsync(p => p.Id == id);
            if (insurer == null) return null;

            insurer.Locked = locked;
            await _context.SaveChangesAsync();
            return insurer;
        }

    }
}