using Microsoft.AspNetCore.Mvc;
using api.Interfaces;
using api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using api.Helpers;

namespace api.Controllers
{
    [ApiController]
    [Route("api/permissions")]
    //[Authorize(Roles = "Admin")]  Seuls les admins peuvent gérer les permissions
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionController(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var permissions = await _permissionRepository.GetAllAsync(query);
            return Ok(permissions);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPermissionById(int id)
        {
            var permission = await _permissionRepository.GetByIdAsync(id);
            if (permission == null) return NotFound();
            return Ok(permission);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePermission([FromBody] Permission permission)
        {
            var createdPermission = await _permissionRepository.CreateAsync(permission);
            return CreatedAtAction(nameof(GetPermissionById), new { id = createdPermission.Id }, createdPermission);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePermission(int id, [FromBody] Permission permission)
        {
            var updatedPermission = await _permissionRepository.UpdateAsync(id, permission);
            if (updatedPermission == null) return NotFound();
            return Ok(updatedPermission);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            var deletedPermission = await _permissionRepository.DeleteAsync(id);
            if (deletedPermission == null) return NotFound();
            return NoContent();
        }
    }
}
