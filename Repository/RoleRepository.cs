using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Generic;
using api.Dtos.Role;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace api.Repository

{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDBContext _context;

        public RoleRepository(ApplicationDBContext context)
        {
            _context = context;
        }
        public async Task<PagedResult<RoleDto>> GetAllRolesAsync(QueryObject query)
        {
            var roles = _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission) // ✅ Charge les permissions sans boucle infinie
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                roles = roles.Where(r => r.RoleName.Contains(query.Search));
            }
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                roles = query.SortBy switch
                {
                    "Name" => query.IsDescending
                        ? roles.OrderByDescending(r => r.RoleName)
                        : roles.OrderBy(r => r.RoleName),
                    _ => roles.OrderBy(r => r.Id)
                };
            }

            var totalCount = await roles.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            var pagedRoles = await roles.OrderBy(r => r.Id).Skip(skipNumber).Take(query.PageSize).ToListAsync();
            var hasNextPage = query.PageNumber < totalPages;

            // ✅ Transformation des données pour inclure RolePermissions sans récursion infinie
            var rolesWithPermissions = pagedRoles.Select(role => new RoleDto
            {
                Id = role.Id,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                Description = role.Description,
                CreatedDate = role.CreatedDate,
                UpdatedDate = role.UpdatedDate,
                RolePermissions = role.RolePermissions.Select(rp => new RolePermissionDto
                {
                    RoleId = rp.RoleId,
                    PermissionId = rp.PermissionId,
                    Permission = rp.Permission != null ? new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        PermissionCode = rp.Permission.PermissionCode,
                        PermissionName = rp.Permission.PermissionName
                    } : null
                }).ToList()
            }).ToList();

            return new PagedResult<RoleDto>
            {
                Items = rolesWithPermissions,
                HasNextPage = hasNextPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                CurrentPage = query.PageNumber
            };
        }

        public async Task<Role?> GetByIdAsync(int id) =>
            await _context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Id == id);

        public async Task<Role?> GetByNameAsync(string name) =>
            await _context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.RoleCode == name);

        public async Task<Role> CreateAsync(Role role)
        {
            if (string.IsNullOrWhiteSpace(role.RoleName) || string.IsNullOrWhiteSpace(role.RoleCode))
            {
                throw new ArgumentException("Le rôle doit avoir un RoleName et un RoleCode.");
            }
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        public async Task<Role?> UpdateAsync(int id, Role role)
        {
            var existingRole = await _context.Roles.FindAsync(id);
            if (existingRole == null) return null;

            existingRole.RoleCode = role.RoleCode;
            existingRole.RoleName = role.RoleName;
            existingRole.Description = role.Description;
            existingRole.UpdatedDate = DateTime.Now;


            await _context.SaveChangesAsync();
            return existingRole;
        }

        public async Task<Role?> DeleteAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return null;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return role;
        }
    }

}