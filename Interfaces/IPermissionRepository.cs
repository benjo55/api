using System.Collections.Generic;
using System.Threading.Tasks;
using api.Dtos.Generic;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IPermissionRepository
    {
        Task<PagedResult<Permission>> GetAllAsync(QueryObject query);
        Task<Permission?> GetByIdAsync(int id);
        Task<Permission> CreateAsync(Permission permission);
        Task<Permission?> UpdateAsync(int id, Permission permission);
        Task<Permission?> DeleteAsync(int id);
    }
}
