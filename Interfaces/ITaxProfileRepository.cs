using api.Dtos.TaxProfile;
using api.Models;
using api.Models.Enum;

namespace api.Interfaces
{
    public interface ITaxProfileRepository
    {
        Task<List<TaxProfileDto>> GetAllAsync();
        Task<TaxProfileDto?> GetByIdAsync(int id);
        Task<TaxProfileDto?> GetByFamilyAsync(ContractFamily family);
        Task<TaxProfile> CreateAsync(TaxProfile profile);
        Task<TaxProfile?> UpdateAsync(int id, UpdateTaxProfileDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface ITaxEngineService
    {
        Task<TaxSimulationResult> SimulateAsync(TaxSimulationRequest request);
        Task<List<TaxComputationDto>> GetRecentComputationsAsync(int take = 50);
    }
}
