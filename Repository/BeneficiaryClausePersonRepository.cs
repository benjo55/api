using api.Data;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Dtos.BeneficiaryClause;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Services;

namespace api.Repository
{
    public class BeneficiaryClausePersonRepository : IBeneficiaryClausePersonRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly EntityHistoryService _entityHistoryService;

        public BeneficiaryClausePersonRepository(ApplicationDBContext context, EntityHistoryService entityHistoryService)
        {
            _context = context;
            _entityHistoryService = entityHistoryService;
        }


        public async Task<PagedResult<BeneficiaryClausePerson>> GetAllAsync(QueryObject query)
        {
            var beneficiaries = _context.BeneficiaryClausePersons
                .Include(bcp => bcp.Person)
                .Include(bcp => bcp.BeneficiaryClause)
                .AsQueryable();

            // 🔹 Filtrage par PersonId
            if (query.PersonId.HasValue)
            {
                Console.WriteLine("⚠️⚠️⚠️⚠️Filtrage par PersonId : " + query.PersonId.Value);
                beneficiaries = beneficiaries.Where(b => b.PersonId == query.PersonId.Value);
            }
            else
            {
                Console.WriteLine("⚠️⚠️⚠️⚠️Pas de filtrage par PersonId");
            }

            // 🔹 Filtrage par Type de clause
            if (query.Filters.ContainsKey("clauseType") && query.Filters["clauseType"] == "Nominative")
            {
                beneficiaries = beneficiaries.Where(b => b.BeneficiaryClause!.ClauseType == "Nominative");
            }

            // 🔹 Tri des résultats
            beneficiaries = query.SortBy switch
            {
                "RelationWithClause" => query.IsDescending
                    ? beneficiaries.OrderByDescending(bcp => bcp.RelationWithClause)
                    : beneficiaries.OrderBy(bcp => bcp.RelationWithClause),

                "Percentage" => query.IsDescending
                    ? beneficiaries.OrderByDescending(bcp => bcp.Percentage)
                    : beneficiaries.OrderBy(bcp => bcp.Percentage),

                _ => query.IsDescending
                    ? beneficiaries.OrderByDescending(bcp => bcp.CreatedDate)
                    : beneficiaries.OrderBy(bcp => bcp.CreatedDate)
            };


            // 🔹 Pagination
            var totalCount = await beneficiaries.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            var pagedBeneficiaries = await beneficiaries.Skip(skipNumber).Take(query.PageSize).ToListAsync();
            var hasNextPage = query.PageNumber < totalPages;

            return new PagedResult<BeneficiaryClausePerson>
            {
                Items = pagedBeneficiaries,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                CurrentPage = query.PageNumber
            };
        }

        public async Task<List<BeneficiaryClausePersonExportDto>> GetAllRawAsync(QueryObject query)
        {
            var beneficiaries = _context.BeneficiaryClausePersons
                .Include(bcp => bcp.Person)
                .AsQueryable();

            if (query.PersonId.HasValue)
            {
                beneficiaries = beneficiaries.Where(b => b.PersonId == query.PersonId.Value);
            }

            if (query.Filters.ContainsKey("clauseType") && query.Filters["clauseType"] == "Nominative")
            {
                beneficiaries = beneficiaries.Where(b => b.BeneficiaryClause!.ClauseType == "Nominative");
            }

            return await beneficiaries.Select(bcp => new BeneficiaryClausePersonExportDto
            {
                ClauseId = bcp.ClauseId,
                PersonId = bcp.PersonId,
                Percentage = bcp.Percentage,
            }).ToListAsync();
        }

        private string GetContractNumber(int clauseId)
        {
            var clause = _context.BeneficiaryClauses.Include(bc => bc.Contract).FirstOrDefault(bc => bc.Id == clauseId);
            return clause?.Contract?.ContractNumber ?? clauseId.ToString();
        }

        public async Task<bool> AssignBeneficiaryAsync(BeneficiaryClausePerson beneficiaryClausePerson)
        {
            _context.BeneficiaryClausePersons.Add(beneficiaryClausePerson);
            await _context.SaveChangesAsync();
            // Historisation dans la timeline du bénéficiaire
            await _entityHistoryService.TrackEventAsync(
                "Person",
                beneficiaryClausePerson.PersonId,
                "Ajouté comme bénéficiaire",
                null,
                $"Ajouté comme bénéficiaire dans le contrat n°{GetContractNumber(beneficiaryClausePerson.ClauseId)}",
                "Admin" // ou l’utilisateur réel si dispo
            );
            return true;
        }

        public async Task<bool> AssignMultipleBeneficiariesAsync(List<BeneficiaryClausePerson> beneficiaries)
        {
            await _context.BeneficiaryClausePersons.AddRangeAsync(beneficiaries);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveBeneficiaryAsync(int clauseId, int personId)
        {
            var entity = await _context.BeneficiaryClausePersons
                .FirstOrDefaultAsync(bcp => bcp.ClauseId == clauseId && bcp.PersonId == personId);
            if (entity == null) return false;

            _context.BeneficiaryClausePersons.Remove(entity);
            await _context.SaveChangesAsync();

            // Historisation dans la timeline du bénéficiaire
            await _entityHistoryService.TrackEventAsync(
                "Person",
                personId,
                "Retiré comme bénéficiaire",
                $"Bénéficiaire du contrat n°{GetContractNumber(clauseId)}",
                null,
                "Admin"
            );
            return true;
        }

        public async Task<List<BeneficiaryClausePerson>> GetByPersonIdAsync(int personId)
        {
            return await _context.BeneficiaryClausePersons
                .Include(bcp => bcp.Person)
                .Include(bcp => bcp.BeneficiaryClause)
                .Where(bcp => bcp.PersonId == personId && bcp.BeneficiaryClause!.ClauseType == "Nominative")
                .ToListAsync();
        }
    }
}
