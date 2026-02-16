using Microsoft.AspNetCore.Mvc;
using api.Interfaces;
using api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Helpers;

namespace api.Controllers
{
    [ApiController]
    [Route("api/roles")]
    //[Authorize(Roles = "Admin")] / Seuls les admins peuvent gérer les rôles
    public class RoleController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IRoleRepository _roleRepository;

        public RoleController(ApplicationDBContext context, IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles([FromQuery] QueryObject query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var roles = await _roleRepository.GetAllRolesAsync(query);

            return Ok(roles);
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetRoleById(int id)
        {

            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) return NotFound();
            return Ok(role);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] Role role)
        {
            var createdRole = await _roleRepository.CreateAsync(role);
            return CreatedAtAction(nameof(GetRoleById), new { id = createdRole.Id }, createdRole);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] Role role)
        {
            var updatedRole = await _roleRepository.UpdateAsync(id, role);
            if (updatedRole == null) return NotFound();
            return Ok(updatedRole);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var deletedRole = await _roleRepository.DeleteAsync(id);
            if (deletedRole == null) return NotFound();
            return NoContent();
        }

        [HttpPut("{roleId}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsRequest request)
        {
            Console.WriteLine($"🔍 Requête reçue pour le rôle {roleId} avec permissions : {string.Join(", ", request.Permissions)}");

            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                Console.WriteLine($"❌ Rôle {roleId} introuvable.");
                return NotFound(new { message = "Rôle introuvable." });
            }

            // 🔹 Affichage des permissions actuelles AVANT mise à jour
            Console.WriteLine($"🔍 Permissions actuelles du rôle {roleId} : " +
                string.Join(", ", role.RolePermissions.Select(rp => rp.PermissionId)));

            // 🔹 Supprime toutes les anciennes permissions du rôle
            _context.RolePermissions.RemoveRange(_context.RolePermissions.Where(rp => rp.RoleId == roleId));

            // 🔹 Ajoute les nouvelles permissions
            // 🔹 Si `request.Permissions` est vide, on garde `RolePermissions` vide et on enregistre.
            if (request.Permissions.Count > 0)
            {
                foreach (var permId in request.Permissions)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permId
                    });
                }
            }

            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ Permissions mises à jour pour le rôle {roleId} : {string.Join(", ", request.Permissions)}");

            return Ok(new { message = "Permissions mises à jour avec succès." });
        }

        [HttpGet("{roleId}/users")]
        public async Task<IActionResult> GetUsersByRole(int roleId)
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                .Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId))
                .ToListAsync();

            return Ok(users);
        }

        // DTO pour recevoir la liste des permissions à mettre à jour
        public class UpdateRolePermissionsRequest
        {
            public List<int> Permissions { get; set; } = new();
        }

    }
}
