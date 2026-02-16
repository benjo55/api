using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using api.Data;

namespace api.Helpers
{
    public class PermissionHelper
    {
        private readonly ApplicationDBContext _context;

        public PermissionHelper(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<bool> UserHasPermission(int userId, string permissionCode)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId && ur.Role != null)
                .SelectMany(ur => ur.Role!.RolePermissions)
                .AnyAsync(rp => rp.Permission != null && rp.Permission.PermissionCode == permissionCode);
        }
    }
}