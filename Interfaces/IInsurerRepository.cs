using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Insurer;
using api.Dtos.Generic;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IInsurerRepository
    {
        Task<PagedResult<Insurer>> GetAllAsync(QueryObject query);
        Task<Insurer?> GetByIdAsync(int id);
        Task<Insurer> CreateAsync(Insurer InsurerModel);
        Task<Insurer?> UpdateAsync(int id, UpdateInsurerRequestDto InsurerDto);
        Task<Insurer?> DeleteAsync(int id);
        Task<bool> InsurerExists(int id);
        Task<Insurer?> PatchLockedAsync(int id, bool locked);
    }
}