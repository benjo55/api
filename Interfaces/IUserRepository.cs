using System.Collections.Generic;
using System.Threading.Tasks;
using api.Dtos.Generic;
using api.Models;
using api.Helpers;

namespace api.Interfaces
{
    public interface IUserRepository
    {
        Task<PagedResult<User>> GetAllAsync(QueryObject query);
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User> CreateAsync(User user);
        Task<User?> UpdateAsync(int id, User user);
        Task<User?> DeleteAsync(int id);
        Task<bool> UsernameExistsAsync(string username);
    }
}
