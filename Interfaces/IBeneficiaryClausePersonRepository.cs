using api.Dtos.BeneficiaryClause;
using api.Dtos.Generic;
using api.Helpers;
using api.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace api.Interfaces
{
    public interface IBeneficiaryClausePersonRepository
    {
        Task<PagedResult<BeneficiaryClausePerson>> GetAllAsync(QueryObject query);
        Task<List<BeneficiaryClausePersonExportDto>> GetAllRawAsync(QueryObject query);
        Task<bool> AssignBeneficiaryAsync(BeneficiaryClausePerson beneficiaryClausePerson);
        Task<bool> RemoveBeneficiaryAsync(int clauseId, int personId);
        Task<List<BeneficiaryClausePerson>> GetByPersonIdAsync(int personId);
    }
}
