using System.Security.Claims;
using api.Dtos.Admin;
using api.Interfaces;
using api.Security;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize]
    public sealed class AdminUsersController : ControllerBase
    {
        private readonly IUserAdministrationService _service;

        public AdminUsersController(IUserAdministrationService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ViewUsers)]
        public async Task<IActionResult> Search([FromQuery] AdminUserSearchRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.SearchUsersAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{userId:int}")]
        [Authorize(Policy = AuthorizationPolicies.ViewUsers)]
        public async Task<IActionResult> Get(int userId, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _service.GetUserAsync(userId, cancellationToken));
            }
            catch (AdminFunctionalException ex)
            {
                return Functional(ex);
            }
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
        public async Task<IActionResult> Create([FromBody] AdminCreateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var id = await _service.CreateUserAsync(request, CurrentUserId(), CurrentUsername(), cancellationToken);
                return CreatedAtAction(nameof(Get), new { userId = id }, new { id });
            }
            catch (AdminFunctionalException ex)
            {
                return Functional(ex);
            }
        }

        [HttpPut("{userId:int}")]
        [HttpPatch("{userId:int}")]
        [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
        public async Task<IActionResult> Update(int userId, [FromBody] AdminUpdateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _service.UpdateUserAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken);
                return NoContent();
            }
            catch (AdminFunctionalException ex)
            {
                return Functional(ex);
            }
        }

        [HttpPut("{userId:int}/roles")]
        [Authorize(Policy = AuthorizationPolicies.ManageUserRoles)]
        public async Task<IActionResult> AssignRoles(int userId, [FromBody] AssignRolesRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _service.AssignRolesAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken);
                return Ok(new AdminActionResult("USER_ROLES_UPDATED", "Les rôles ont été mis à jour."));
            }
            catch (AdminFunctionalException ex)
            {
                return Functional(ex);
            }
        }

        [HttpPut("{userId:int}/person-link")]
        [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
        public async Task<IActionResult> SetPersonLink(int userId, [FromBody] LinkUserPersonRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _service.SetUserPersonLinkAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken);
                return Ok(new AdminActionResult("USER_PERSON_LINK_UPDATED", "Le rattachement personne/utilisateur a été mis à jour."));
            }
            catch (AdminFunctionalException ex)
            {
                return Functional(ex);
            }
        }

        [HttpPost("{userId:int}/suspend")]
        [Authorize(Policy = AuthorizationPolicies.SuspendUsers)]
        public Task<IActionResult> Suspend(int userId, [FromBody] SuspendUserRequest request, CancellationToken cancellationToken) =>
            RunAction(() => _service.SuspendUserAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken), "USER_SUSPENDED", "Utilisateur suspendu.");

        [HttpPost("{userId:int}/reactivate")]
        [Authorize(Policy = AuthorizationPolicies.SuspendUsers)]
        public Task<IActionResult> Reactivate(int userId, [FromBody] ReasonedUserActionRequest request, CancellationToken cancellationToken) =>
            RunAction(() => _service.ReactivateUserAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken), "USER_REACTIVATED", "Utilisateur réactivé.");

        [HttpPost("{userId:int}/revoke")]
        [Authorize(Policy = AuthorizationPolicies.RevokeUsers)]
        public Task<IActionResult> Revoke(int userId, [FromBody] RevokeUserRequest request, CancellationToken cancellationToken) =>
            RunAction(() => _service.RevokeUserAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken), "USER_REVOKED", "Utilisateur révoqué.");

        [HttpPost("{userId:int}/restore")]
        [Authorize(Policy = AuthorizationPolicies.RevokeUsers)]
        public Task<IActionResult> Restore(int userId, [FromBody] RestoreUserRequest request, CancellationToken cancellationToken) =>
            RunAction(() => _service.RestoreUserAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken), "USER_RESTORED", "Utilisateur restauré.");

        [HttpPost("{userId:int}/lock")]
        [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
        public Task<IActionResult> Lock(int userId, [FromBody] LockUserRequest request, CancellationToken cancellationToken) =>
            RunAction(() => _service.LockUserAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken), "USER_LOCKED", "Utilisateur verrouillé.");

        [HttpPost("{userId:int}/unlock")]
        [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
        public Task<IActionResult> Unlock(int userId, [FromBody] ReasonedUserActionRequest request, CancellationToken cancellationToken) =>
            RunAction(() => _service.UnlockUserAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken), "USER_UNLOCKED", "Utilisateur déverrouillé.");

        [HttpPost("{userId:int}/force-password-reset")]
        [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
        public Task<IActionResult> ForcePasswordReset(int userId, [FromBody] ForcePasswordResetRequest request, CancellationToken cancellationToken) =>
            RunAction(() => _service.ForcePasswordResetAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken), "PASSWORD_RESET_FORCED", "Réinitialisation demandée.");

        [HttpPost("{userId:int}/invalidate-sessions")]
        [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
        public Task<IActionResult> InvalidateSessions(int userId, [FromBody] InvalidateUserSessionsRequest request, CancellationToken cancellationToken) =>
            RunAction(() => _service.InvalidateSessionsAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken), "SESSIONS_INVALIDATED", "Sessions invalidées.");

        [HttpPost("{userId:int}/resend-confirmation-email")]
        [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
        public Task<IActionResult> ResendConfirmation(int userId, CancellationToken cancellationToken) =>
            RunAction(() => _service.ResendConfirmationEmailAsync(userId, CurrentUserId(), CurrentUsername(), cancellationToken), "CONFIRMATION_EMAIL_RESENT", "E-mail de confirmation renvoyé.");

        [HttpPost("{userId:int}/confirm-email")]
        [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
        public Task<IActionResult> ConfirmEmail(int userId, [FromBody] ReasonedUserActionRequest request, CancellationToken cancellationToken) =>
            RunAction(() => _service.ConfirmEmailAdministrativelyAsync(userId, request, CurrentUserId(), CurrentUsername(), cancellationToken), "EMAIL_CONFIRMED_ADMIN", "Adresse e-mail confirmée.");

        [HttpGet("{userId:int}/audit")]
        [Authorize(Policy = AuthorizationPolicies.ViewSecurityAudit)]
        public async Task<IActionResult> Audit(int userId, CancellationToken cancellationToken)
        {
            var result = await _service.GetUserAuditAsync(userId, cancellationToken);
            return Ok(result);
        }

        private async Task<IActionResult> RunAction(Func<Task> action, string code, string message)
        {
            try
            {
                await action();
                return Ok(new AdminActionResult(code, message));
            }
            catch (AdminFunctionalException ex)
            {
                return Functional(ex);
            }
        }

        private int? CurrentUserId() =>
            int.TryParse(User.FindFirst("userId")?.Value, out var userId) ? userId : null;

        private string? CurrentUsername() =>
            User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("username")?.Value;

        private ObjectResult Functional(AdminFunctionalException ex) =>
            StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
    }
}
