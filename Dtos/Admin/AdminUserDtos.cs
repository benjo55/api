using System.ComponentModel.DataAnnotations;
using api.Models;

namespace api.Dtos.Admin
{
    public sealed class AdminUserSearchRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int? RoleId { get; set; }
        public string? Role { get; set; }
        public bool? EmailConfirmed { get; set; }
        public bool? Locked { get; set; }
        public bool? Expired { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? LastLoginFrom { get; set; }
        public DateTime? LastLoginTo { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
    }

    public sealed record AdminUserListItemDto(
        int Id,
        string UserName,
        string Email,
        string? PhoneNumber,
        string Status,
        bool EmailConfirmed,
        IReadOnlyCollection<string> Roles,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        DateTime? LastActivityAt,
        DateTime? AccountExpiresAt,
        bool Locked,
        AdminLinkedPersonDto? LinkedPerson);

    public sealed record AdminLinkedPersonDto(
        int Id,
        string FirstName,
        string LastName,
        string FullName,
        string? Email,
        string? PhoneNumber,
        string Role,
        string Status);

    public sealed record AdminRoleSummaryDto(
        int Id,
        string RoleCode,
        string RoleName,
        string Description,
        bool IsSystem,
        int PrivilegeRank,
        IReadOnlyCollection<int> PermissionIds);

    public sealed record AdminPermissionDto(
        int Id,
        string PermissionCode,
        string PermissionName,
        string Description,
        bool IsSystem);

    public sealed record AdminUserDetailsDto(
        int Id,
        string UserName,
        string Email,
        string? PhoneNumber,
        string Status,
        bool EmailConfirmed,
        DateTime? EmailConfirmedAt,
        IReadOnlyCollection<AdminRoleSummaryDto> Roles,
        IReadOnlyCollection<string> EffectivePermissions,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? LastLoginAt,
        DateTime? LastActivityAt,
        DateTime? SuspendedAt,
        DateTime? SuspensionEndsAt,
        string? SuspensionReason,
        DateTime? RevokedAt,
        string? RevocationReason,
        DateTime? AccountExpiresAt,
        bool MustChangePassword,
        DateTime? PasswordChangedAt,
        int FailedLoginAttempts,
        DateTime? LockedUntil,
        int SessionVersion,
        string RowVersion,
        AdminLinkedPersonDto? LinkedPerson);

    public sealed class AdminCreateUserRequest
    {
        [Required, MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(32)]
        public string? PhoneNumber { get; set; }

        public IReadOnlyCollection<int> RoleIds { get; set; } = Array.Empty<int>();

        public DateTime? AccountExpiresAt { get; set; }

        public UserStatus Status { get; set; } = UserStatus.PendingEmailConfirmation;

        public bool SendActivationEmail { get; set; } = true;
    }

    public sealed class AdminUpdateUserRequest
    {
        [Required, MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(32)]
        public string? PhoneNumber { get; set; }

        public DateTime? AccountExpiresAt { get; set; }

        [Required]
        public string RowVersion { get; set; } = string.Empty;
    }

    public sealed class AssignRolesRequest
    {
        public IReadOnlyCollection<int> RoleIds { get; set; } = Array.Empty<int>();

        [Required, MinLength(4), MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public string RowVersion { get; set; } = string.Empty;
    }

    public sealed class LinkUserPersonRequest : ReasonedUserActionRequest
    {
        public int? PersonId { get; set; }
    }

    public class ReasonedUserActionRequest
    {
        [Required, MinLength(4), MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public bool NotifyUser { get; set; }
    }

    public sealed class SuspendUserRequest : ReasonedUserActionRequest
    {
        public DateTime? SuspensionEndsAt { get; set; }
        public bool InvalidateSessions { get; set; } = true;
    }

    public sealed class RevokeUserRequest : ReasonedUserActionRequest
    {
        public bool InvalidateSessions { get; set; } = true;
    }

    public sealed class RestoreUserRequest : ReasonedUserActionRequest
    {
        public bool ForcePasswordReset { get; set; } = true;
    }

    public sealed class LockUserRequest : ReasonedUserActionRequest
    {
        public DateTime? LockedUntil { get; set; }
    }

    public sealed class ForcePasswordResetRequest : ReasonedUserActionRequest
    {
        public bool SendEmail { get; set; } = true;
        public bool InvalidateSessions { get; set; } = true;
    }

    public sealed class InvalidateUserSessionsRequest : ReasonedUserActionRequest
    {
    }

    public sealed record AdminAuditEventDto(
        int Id,
        int? ActingUserId,
        string? ActingUsername,
        int? TargetUserId,
        int? TargetRoleId,
        string Action,
        string? Reason,
        string? ResultCode,
        string? DetailsJson,
        DateTime CreatedAt);

    public sealed record AdminActionResult(string Code, string Message);
}
