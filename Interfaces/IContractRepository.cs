using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Contract;
using api.Dtos.Generic;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IContractRepository
    {
        Task<PagedResult<Contract>> GetAllAsync(QueryObject query);
        Task<Contract?> GetByIdAsync(int id);
        Task<Contract> CreateAsync(Contract contractModel, CreateContractRequestDto contractDto);
        Task<Contract?> UpdateAsync(int id, UpdateContractRequestDto contractDto);
        Task<Contract?> PatchBeneficiaryClauseIdAsync(int contractId, int clauseId);
        Task<Contract?> DeleteAsync(int id);
        Task<int> CountContractsByProductIdAsync(int productId);
        Task<Contract?> PatchLockedAsync(int id, bool locked);
        Task<IEnumerable<FinancialSupportAllocation>> GetAvailableSupportsAsync(int contractId, int? compartmentId);
        Task<Contract?> UpdateCurrentValueAsync(int contractId, decimal newValue);
        Task<List<ContractSupportHoldingDto>> GetHoldingsByContractAsync(int contractId);
        Task<Contract?> LoadContractById(int id);
        void DetachAllEntities();
        Task<object?> RecalculateAndLoadAsync(int contractId, IContractValuationService valuationService);
        Task<object?> RecalculateValueAsync(int id, IContractValuationService valuationService, string source = "Unknown");
        Task<object?> RebuildHoldingsAsync(int contractId);

    }
}