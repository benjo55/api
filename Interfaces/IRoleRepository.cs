using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Generic;
using api.Dtos.Brand;
using api.Helpers;
using api.Models;
using api.Dtos.Role;

namespace api.Interfaces

{

    public interface IRoleRepository
    {
        Task<PagedResult<RoleDto>> GetAllRolesAsync(QueryObject query);
        Task<Role?> GetByIdAsync(int id);
        Task<Role?> GetByNameAsync(string name);
        Task<Role> CreateAsync(Role role);
        Task<Role?> UpdateAsync(int id, Role role);
        Task<Role?> DeleteAsync(int id);
    }
}

