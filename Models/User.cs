using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public enum UserStatus
    {
        PendingEmailConfirmation = 0,
        Active = 1,
        Suspended = 2,
        Revoked = 3,
        Locked = 4,
        Expired = 5
    }

    public enum UserOrigin
    {
        Life = 1,
        Cerfa = 2,
        Urbanisation = 3
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string NormalizedUsername { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(254)]
        public string NormalizedEmail { get; set; } = string.Empty;

        [Required, MaxLength(32)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        public bool EmailConfirmed { get; set; }

        public DateTime? EmailConfirmedAt { get; set; }

        public DateTime? LastEmailConfirmationSentAt { get; set; }

        public DateTime? PasswordChangedAt { get; set; }

        public UserStatus Status { get; set; } = UserStatus.PendingEmailConfirmation;

        public UserOrigin Origin { get; set; } = UserOrigin.Life;

        public DateTime? SuspendedAt { get; set; }

        [MaxLength(500)]
        public string? SuspensionReason { get; set; }

        public DateTime? SuspensionEndsAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        [MaxLength(500)]
        public string? RevocationReason { get; set; }

        public DateTime? AccountExpiresAt { get; set; }

        public bool MustChangePassword { get; set; }

        public DateTime? PrivacyPolicyAcceptedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public DateTime? LastActivityAt { get; set; }

        public int FailedLoginAttempts { get; set; }

        public DateTime? LockedUntil { get; set; }

        public int SessionVersion { get; set; }

        public DateTime? SessionsInvalidatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public ICollection<UserSecurityToken> SecurityTokens { get; set; } = new List<UserSecurityToken>();

        public ICollection<UserMfaFactor> MfaFactors { get; set; } = new List<UserMfaFactor>();

        public Person? Person { get; set; }
    }

    public static class UserSecurityTokenTypes
    {
        public const string EmailConfirmation = "EmailConfirmation";
        public const string PasswordReset = "PasswordReset";
        public const string SubscriptionMfa = "SubscriptionMfa";
    }

    public class UserSecurityToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required, MaxLength(40)]
        public string TokenType { get; set; } = string.Empty;

        [Required, MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        [MaxLength(64)]
        public string? CreatedByIpAddress { get; set; }
    }

    public static class UserMfaFactorTypes
    {
        public const string Totp = "Totp";
    }

    public class UserMfaFactor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required, MaxLength(40)]
        public string FactorType { get; set; } = string.Empty;

        [Required, MaxLength(120)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        public string ProtectedSecret { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ActivatedAt { get; set; }

        public DateTime? LastUsedAt { get; set; }

        public DateTime? RevokedAt { get; set; }
    }

    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public bool IsSystem { get; set; }
        public int PrivilegeRank { get; set; }

        // Relation Many-to-Many avec `User` via `UserRole`
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        // Relation Many-to-Many avec `Permission` via `RolePermission`
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }


    // Nouvelle table de liaison Many-to-Many entre User et Role
    public class UserRole
    {
        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public Role? Role { get; set; }
    }

    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string PermissionName { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string PermissionCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSystem { get; set; }

        // ✅ Relation Many-to-Many avec `Role` via `RolePermission`
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class RolePermission
    {
        [Required]
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        [Required]
        public int PermissionId { get; set; }
        [ForeignKey("PermissionId")]
        public Permission? Permission { get; set; }
    }

    public class AdminAuditEvent
    {
        [Key]
        public int Id { get; set; }

        public int? ActingUserId { get; set; }

        [MaxLength(100)]
        public string? ActingUsername { get; set; }

        public int? TargetUserId { get; set; }

        public int? TargetRoleId { get; set; }

        [Required, MaxLength(80)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(60)]
        public string? ResultCode { get; set; }

        public string? DetailsJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
