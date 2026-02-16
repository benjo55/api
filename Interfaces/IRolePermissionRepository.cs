using System.Collections.Generic;
using System.Threading.Tasks;
using api.Models;

namespace api.Interfaces
{
    public interface IRolePermissionRepository
    {
        Task<List<RolePermission>> GetAllAsync();
        Task<List<Permission>> GetPermissionsByRoleIdAsync(int roleId);
        Task<RolePermission?> AddPermissionToRoleAsync(int roleId, int permissionId);
        Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId);
    }
}
