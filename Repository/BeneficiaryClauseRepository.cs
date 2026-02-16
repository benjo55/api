using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.BeneficiaryClause;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Services;
using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class BeneficiaryClauseRepository : IBeneficiaryClauseRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly EntityHistoryService _entityHistoryService;
        private readonly IMapper _mapper;

        public BeneficiaryClauseRepository(ApplicationDBContext context, EntityHistoryService entityHistoryService, IMapper mapper)
        {
            _context = context;
            _entityHistoryService = entityHistoryService;
            _mapper = mapper;
        }

        public async Task<BeneficiaryClause> CreateAsync(BeneficiaryClause beneficiaryClauseModel)
        {
            await _context.BeneficiaryClauses.AddAsync(beneficiaryClauseModel);
            await _context.SaveChangesAsync();

            // PATCH du contrat associé, s'il existe
            if (beneficiaryClauseModel.ContractId > 0)
            {
                var contract = await _context.Contracts.FindAsync(beneficiaryClauseModel.ContractId);
                if (contract != null)
                {
                    contract.BeneficiaryClauseId = beneficiaryClauseModel.Id;
                    contract.UpdatedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            return beneficiaryClauseModel;
        }

        public async Task<BeneficiaryClause?> DeleteAsync(int id)
        {
            try
            {
                var beneficiaryClauseModel = await _context.BeneficiaryClauses.FirstOrDefaultAsync(p => p.Id == id);
                if (beneficiaryClauseModel == null) return null;
                _context.BeneficiaryClauses.Remove(beneficiaryClauseModel);
                await _context.SaveChangesAsync();
                return beneficiaryClauseModel;
            }
            catch (DbUpdateException ex) when ((ex.InnerException as SqlException)?.Number == 547)
            {
                throw new InvalidOperationException("Impossible de supprimer cette clause bénéficiaire car elle est référencée par un ou plusieurs personnes.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Une erreur inattendue s'est produite lors de la suppression.", ex);
            }
        }

        public async Task<PagedResult<BeneficiaryClauseListItemDto>> GetAllAsync(QueryObject query)
        {
            var clauses = _context.BeneficiaryClauses.AsQueryable();

            clauses = query.SortBy switch
            {
                "CreatedDate" => query.IsDescending ? clauses.OrderByDescending(c => c.CreatedDate) : clauses.OrderBy(c => c.CreatedDate),
                _ => clauses.OrderByDescending(c => c.CreatedDate)
            };

            var totalCount = await clauses.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
            var skipNumber = (query.PageNumber - 1) * query.PageSize;

            // **Voici la projection DTO qui évite le N+1 et n’inclut QUE le nécessaire**
            var pagedClauses = await clauses
                .Skip(skipNumber)
                .Take(query.PageSize)
                .Select(c => new BeneficiaryClauseListItemDto
                {
                    Id = c.Id,
                    ClauseType = c.ClauseType,
                    Locked = c.Locked,
                    Description = c.Description,
                    Status = c.Status,
                    ContractNumber = c.Contract != null ? c.Contract.ContractNumber : string.Empty,
                    CreatedDate = c.CreatedDate,
                    UpdatedDate = c.UpdatedDate,
                    Beneficiaries = c.Beneficiaries.Select(b => new BeneficiaryListPersonDto
                    {
                        PersonId = b.PersonId,
                        LastName = b.Person != null ? b.Person.LastName : string.Empty,
                        FirstName = b.Person != null ? b.Person.FirstName : string.Empty,
                        RelationWithClause = b.RelationWithClause,
                        Percentage = b.Percentage
                    }).ToList()
                })
                .ToListAsync();

            var hasNextPage = query.PageNumber < totalPages;

            return new PagedResult<BeneficiaryClauseListItemDto>
            {
                Items = pagedClauses,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                CurrentPage = query.PageNumber
            };
        }


        public async Task<BeneficiaryClause?> GetByIdAsync(int id)
        {
            return await _context.BeneficiaryClauses
                .Include(c => c.Beneficiaries)
                    .ThenInclude(b => b.Person)
                .Include(c => c.Contract)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> BeneficiaryClauseExists(int id)
        {
            return await _context.BeneficiaryClauses.AnyAsync(c => c.Id == id);
        }

        public async Task<BeneficiaryClause?> UpdateAsync(int id, UpdateBeneficiaryClauseRequestDto updateBeneficiaryClauseDto)
        {
            var existingBeneficiaryClause = await _context.BeneficiaryClauses.FindAsync(id);
            if (existingBeneficiaryClause == null) return null;

            // Sauvegarde de l'état initial pour l'historique
            var originalBeneficiaryClause = new BeneficiaryClause();
            _mapper.Map(existingBeneficiaryClause, originalBeneficiaryClause);

            // Mise à jour automatique des champs avec AutoMapper
            _mapper.Map(updateBeneficiaryClauseDto, existingBeneficiaryClause);
            existingBeneficiaryClause.UpdatedDate = DateTime.UtcNow;

            // Historisation des changements (à personnaliser avec l'utilisateur courant)
            await _entityHistoryService.TrackChangesAsync(
                originalBeneficiaryClause,
                existingBeneficiaryClause,
                "Admin"
            );

            await _context.SaveChangesAsync();
            return existingBeneficiaryClause;
        }

        public async Task<BeneficiaryClause?> PatchLockedAsync(int id, bool locked)
        {
            var originalClause = await _context.BeneficiaryClauses
                .AsNoTracking()
                .FirstOrDefaultAsync(bc => bc.Id == id);

            var clause = await _context.BeneficiaryClauses
                .FirstOrDefaultAsync(bc => bc.Id == id);

            if (clause == null || originalClause == null)
                return null;

            clause.Locked = locked;
            clause.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _entityHistoryService.TrackChangesAsync(originalClause, clause, "Admin");

            return clause;
        }
    }
}