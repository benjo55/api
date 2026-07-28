using System.Security.Claims;
using api.Dtos.Admin;
using api.Interfaces;
using api.Models;
using api.Security;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public sealed class AdminRolesController : ControllerBase
    {
        private readonly IUserAdministrationService _service;

        public AdminRolesController(IUserAdministrationService service)
        {
            _service = service;
        }

        [HttpGet("roles")]
        [Authorize(Policy = AuthorizationPolicies.ViewRoles)]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken) =>
            Ok(await _service.GetRolesAsync(cancellationToken));

        [HttpPost("roles")]
        [Authorize(Policy = AuthorizationPolicies.ManageRoles)]
        public async Task<IActionResult> CreateRole([FromBody] Role role, CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.CreateRoleAsync(role, CurrentUserId(), CurrentUsername(), cancellationToken);
                return CreatedAtAction(nameof(GetRoles), new { id = created.Id }, created);
            }
            catch (AdminFunctionalException ex)
            {
                return Functional(ex);
            }
        }

        [HttpPut("roles/{roleId:int}")]
        [Authorize(Policy = AuthorizationPolicies.ManageRoles)]
        public async Task<IActionResult> UpdateRole(int roleId, [FromBody] Role role, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _service.UpdateRoleAsync(roleId, role, CurrentUserId(), CurrentUsername(), cancellationToken));
            }
            catch (AdminFunctionalException ex)
            {
                return Functional(ex);
            }
        }

        [HttpDelete("roles/{roleId:int}")]
        [Authorize(Policy = AuthorizationPolicies.ManageRoles)]
        public async Task<IActionResult> DeleteRole(int roleId, CancellationToken cancellationToken)
        {
            try
            {
                await _service.DeleteRoleAsync(roleId, CurrentUserId(), CurrentUsername(), cancellationToken);
                return NoContent();
            }
            catch (AdminFunctionalException ex)
            {
                return Functional(ex);
            }
        }

        [HttpPut("roles/{roleId:int}/permissions")]
        [Authorize(Policy = AuthorizationPolicies.ManageRoles)]
        public async Task<IActionResult> UpdateRolePermissions(
            int roleId,
            [FromBody] UpdateRolePermissionsRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                await _service.UpdateRolePermissionsAsync(
                    roleId,
                    request.PermissionIds,
                    request.Reason,
                    CurrentUserId(),
                    CurrentUsername(),
                    cancellationToken);
                return Ok(new AdminActionResult("ROLE_PERMISSIONS_UPDATED", "Permissions du rôle mises à jour."));
            }
            catch (AdminFunctionalException ex)
            {
                return Functional(ex);
            }
        }

        [HttpGet("permissions")]
        [Authorize(Policy = AuthorizationPolicies.ViewRoles)]
        public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken) =>
            Ok(await _service.GetPermissionsAsync(cancellationToken));

        private int? CurrentUserId() =>
            int.TryParse(User.FindFirst("userId")?.Value, out var userId) ? userId : null;

        private string? CurrentUsername() =>
            User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("username")?.Value;

        private ObjectResult Functional(AdminFunctionalException ex) =>
            StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
    }

    public sealed class UpdateRolePermissionsRequest
    {
        public IReadOnlyCollection<int> PermissionIds { get; set; } = Array.Empty<int>();
        public string Reason { get; set; } = string.Empty;
    }
}
