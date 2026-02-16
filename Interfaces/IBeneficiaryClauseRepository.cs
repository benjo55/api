using api.Dtos.BeneficiaryClause;
using api.Dtos.Generic;
using api.Helpers;
using api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace api.Interfaces
{
    public interface IBeneficiaryClauseRepository
    {
        // Task<PagedResult<BeneficiaryClause>> GetAllAsync(QueryObject query);
        Task<PagedResult<BeneficiaryClauseListItemDto>> GetAllAsync(QueryObject query);
        Task<BeneficiaryClause?> GetByIdAsync(int id);
        Task<BeneficiaryClause> CreateAsync(BeneficiaryClause beneficiaryClauseModel);
        Task<BeneficiaryClause?> UpdateAsync(int id, UpdateBeneficiaryClauseRequestDto beneficiaryClauseDto);
        Task<BeneficiaryClause?> DeleteAsync(int id);
        Task<bool> BeneficiaryClauseExists(int id);
        Task<BeneficiaryClause?> PatchLockedAsync(int id, bool locked);
    }
}
