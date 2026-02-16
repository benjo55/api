using Microsoft.AspNetCore.Mvc;
using api.Interfaces;
using api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers
{
    [ApiController]
    [Route("api/role-permissions")]
    [Authorize(Roles = "Admin")] // Seuls les admins peuvent gérer les permissions des rôles
    public class RolePermissionController : ControllerBase
    {
        private readonly IRolePermissionRepository _rolePermissionRepository;

        public RolePermissionController(IRolePermissionRepository rolePermissionRepository)
        {
            _rolePermissionRepository = rolePermissionRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRolePermissions()
        {
            var rolePermissions = await _rolePermissionRepository.GetAllAsync();
            return Ok(rolePermissions);
        }

        [HttpGet("{roleId:int}")]
        public async Task<IActionResult> GetPermissionsByRoleId(int roleId)
        {
            var permissions = await _rolePermissionRepository.GetPermissionsByRoleIdAsync(roleId);
            return Ok(permissions);
        }

        [HttpPost]
        public async Task<IActionResult> AddPermissionToRole(int roleId, int permissionId)
        {
            var rolePermission = await _rolePermissionRepository.AddPermissionToRoleAsync(roleId, permissionId);
            if (rolePermission == null) return BadRequest("Rôle ou permission invalide.");

            return CreatedAtAction(nameof(GetPermissionsByRoleId), new { roleId = roleId }, rolePermission);
        }

        [HttpDelete]
        public async Task<IActionResult> RemovePermissionFromRole(int roleId, int permissionId)
        {
            var success = await _rolePermissionRepository.RemovePermissionFromRoleAsync(roleId, permissionId);
            if (!success) return NotFound("Association rôle-permission non trouvée.");

            return NoContent();
        }
    }
}
