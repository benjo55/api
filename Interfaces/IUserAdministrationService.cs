using api.Dtos.Admin;
using api.Dtos.Generic;
using api.Models;

namespace api.Interfaces
{
    public interface IUserAdministrationService
    {
        Task<PagedResult<AdminUserListItemDto>> SearchUsersAsync(
            AdminUserSearchRequest request,
            CancellationToken cancellationToken = default);

        Task<AdminUserDetailsDto> GetUserAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<int> CreateUserAsync(
            AdminCreateUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task UpdateUserAsync(
            int userId,
            AdminUpdateUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task AssignRolesAsync(
            int userId,
            AssignRolesRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task SuspendUserAsync(
            int userId,
            SuspendUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task ReactivateUserAsync(
            int userId,
            ReasonedUserActionRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task RevokeUserAsync(
            int userId,
            RevokeUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task RestoreUserAsync(
            int userId,
            RestoreUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task LockUserAsync(
            int userId,
            LockUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task UnlockUserAsync(
            int userId,
            ReasonedUserActionRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task ForcePasswordResetAsync(
            int userId,
            ForcePasswordResetRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task InvalidateSessionsAsync(
            int userId,
            InvalidateUserSessionsRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task ResendConfirmationEmailAsync(
            int userId,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task ConfirmEmailAdministrativelyAsync(
            int userId,
            ReasonedUserActionRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<AdminAuditEventDto>> GetUserAuditAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<AdminRoleSummaryDto>> GetRolesAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<AdminPermissionDto>> GetPermissionsAsync(
            CancellationToken cancellationToken = default);

        Task<AdminRoleSummaryDto> CreateRoleAsync(
            Role role,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task<AdminRoleSummaryDto> UpdateRoleAsync(
            int roleId,
            Role role,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task DeleteRoleAsync(
            int roleId,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);

        Task UpdateRolePermissionsAsync(
            int roleId,
            IReadOnlyCollection<int> permissionIds,
            string reason,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default);
    }
}
