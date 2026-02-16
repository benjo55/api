using api.Data;
using api.Interfaces;
using api.Models;
using api.Dtos.Generic;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Helpers;

namespace api.Repository
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly ApplicationDBContext _context;

        public PermissionRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Permission>> GetAllAsync(QueryObject query)
        {
            var permissions = _context.Permissions.AsQueryable();

            // Pagination
            var totalCount = await permissions.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            var pagedPermissions = await permissions.Skip(skipNumber).Take(query.PageSize).ToListAsync();
            var hasNextPage = query.PageNumber < totalPages;

            return new PagedResult<Permission>
            {
                Items = pagedPermissions,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                CurrentPage = query.PageNumber
            };
        }

        public async Task<Permission?> GetByIdAsync(int id)
        {
            return await _context.Permissions.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Permission> CreateAsync(Permission permission)
        {
            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();
            return permission;
        }

        public async Task<Permission?> UpdateAsync(int id, Permission permission)
        {
            var existingPermission = await _context.Permissions.FindAsync(id);
            if (existingPermission == null) return null;

            existingPermission.PermissionCode = permission.PermissionCode;
            existingPermission.PermissionName = permission.PermissionName;
            existingPermission.Description = permission.Description;
            await _context.SaveChangesAsync();
            return existingPermission;
        }

        public async Task<Permission?> DeleteAsync(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null) return null;

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();
            return permission;
        }
    }
}
