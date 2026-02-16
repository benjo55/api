using api.Models;

namespace api.Interfaces
{
    public interface IContractOptionTypeRepository
    {
        Task<IEnumerable<ContractOptionType>> GetAllAsync();
        Task<ContractOptionType?> GetByIdAsync(int id);
        Task<ContractOptionType> CreateAsync(ContractOptionType optionType);
        Task<ContractOptionType?> UpdateAsync(int id, ContractOptionType optionType);
        Task<bool> DeleteAsync(int id);
        Task<int> CountAsync();
    }
}
