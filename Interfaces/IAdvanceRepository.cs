using api.Dtos.Advance;

namespace api.Interfaces
{
    public interface IAdvanceRepository
    {
        Task<List<AdvanceDto>> GetAllAsync();
        Task<List<AdvanceDto>> GetByContractIdAsync(int contractId);
        Task<AdvanceDto?> GetByIdAsync(int id);
        Task<AdvanceDto> CreateAsync(CreateAdvanceRequestDto dto);
        Task<AdvanceDto?> UpdateAsync(int id, UpdateAdvanceRequestDto dto);
        Task<AdvanceTransactionDto> AddTransactionAsync(int advanceId, CreateAdvanceTransactionRequestDto dto);
        Task<AdvanceEligibilityDto?> GetEligibilityAsync(int contractId);
    }
}
