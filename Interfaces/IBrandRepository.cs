using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Generic;
using api.Dtos.Brand;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IBrandRepository
    {
        Task<PagedResult<Brand>> GetAllAsync(QueryObject query);
        Task<Brand?> GetByIdAsync(int id);
        Task<Brand> CreateAsync(Brand brandModel);
        Task<Brand?> UpdateAsync(int id, UpdateBrandRequestDto brandDto);
        Task<Brand?> DeleteAsync(int id);
        Task<Brand?> PatchLockedAsync(int id, bool locked);
    }
}