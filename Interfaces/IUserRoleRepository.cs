using System.Collections.Generic;
using System.Threading.Tasks;
using api.Models;

namespace api.Interfaces
{
    public interface IUserUserRepository
    {
        Task<List<UserRole>> GetAllAsync();
        Task<List<Role>> GetRolesByUserIdAsync(int roleId);
        Task<UserRole?> AddRoleToUserAsync(int roleId, int permissionId);
        Task<bool> RemoveRoleFromUserAsync(int roleId, int permissionId);
    }
}
