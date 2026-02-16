using System.Threading.Tasks;
using api.Dtos.FinancialSupport;
using api.Dtos.Generic;
using api.Helpers;

namespace api.Interfaces
{
    public interface IFinancialSupportRepository
    {
        Task<PagedResult<FinancialSupportDto>> GetAllAsync(QueryObject query);
        Task<FinancialSupportDto?> GetByIdAsync(int id);
        Task<FinancialSupportDto?> GetByCodeAsync(string code);
        Task<FinancialSupportDto> CreateAsync(CreateFinancialSupportRequestDto createDto);
        Task<bool> AnyByIsinAsync(string isin);
        Task<FinancialSupportDto?> UpdateAsync(int id, UpdateFinancialSupportRequestDto updateDto);
        Task<FinancialSupportDto?> DeleteAsync(int id);
        Task<List<FinancialSupportDto>> TypeaheadAsync(string search);
    }
}
