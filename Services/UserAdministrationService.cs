using System.Text.Json;
using api.Data;
using api.Dtos.Admin;
using api.Dtos.Auth;
using api.Dtos.Generic;
using api.Interfaces;
using api.Models;
using api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public sealed class UserAdministrationService : IUserAdministrationService
    {
        private readonly ApplicationDBContext _db;
        private readonly IAuthenticationAccountService _accountService;
        private readonly ILogger<UserAdministrationService> _logger;

        public UserAdministrationService(
            ApplicationDBContext db,
            IAuthenticationAccountService accountService,
            ILogger<UserAdministrationService> logger)
        {
            _db = db;
            _accountService = accountService;
            _logger = logger;
        }

        public async Task<PagedResult<AdminUserListItemDto>> SearchUsersAsync(
            AdminUserSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var now = DateTime.UtcNow;

            var query = _db.Users
                .AsNoTracking()
                .Include(u => u.Person)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToUpperInvariant();
                query = query.Where(u =>
                    u.NormalizedUsername.Contains(search)
                    || u.NormalizedEmail.Contains(search)
                    || u.PhoneNumber.Contains(request.Search.Trim()));
            }

            if (Enum.TryParse<UserStatus>(request.Status, ignoreCase: true, out var status))
            {
                query = query.Where(u => u.Status == status);
            }

            if (request.RoleId.HasValue)
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == request.RoleId.Value));
            }
            else if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var role = request.Role.Trim();
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleCode == role));
            }

            if (request.EmailConfirmed.HasValue)
            {
                query = query.Where(u => u.EmailConfirmed == request.EmailConfirmed.Value);
            }

            if (request.Locked.HasValue)
            {
                query = request.Locked.Value
                    ? query.Where(u => u.Status == UserStatus.Locked || (u.LockedUntil != null && u.LockedUntil > now))
                    : query.Where(u => u.Status != UserStatus.Locked && (u.LockedUntil == null || u.LockedUntil <= now));
            }

            if (request.Expired.HasValue)
            {
                query = request.Expired.Value
                    ? query.Where(u => u.AccountExpiresAt != null && u.AccountExpiresAt <= now)
                    : query.Where(u => u.AccountExpiresAt == null || u.AccountExpiresAt > now);
            }

            if (request.CreatedFrom.HasValue)
            {
                query = query.Where(u => u.CreatedDate >= request.CreatedFrom.Value);
            }

            if (request.CreatedTo.HasValue)
            {
                query = query.Where(u => u.CreatedDate <= request.CreatedTo.Value);
            }

            if (request.LastLoginFrom.HasValue)
            {
                query = query.Where(u => u.LastLoginAt >= request.LastLoginFrom.Value);
            }

            if (request.LastLoginTo.HasValue)
            {
                query = query.Where(u => u.LastLoginAt <= request.LastLoginTo.Value);
            }

            var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            query = (request.SortBy ?? "").ToLowerInvariant() switch
            {
                "username" => descending ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
                "lastlogin" or "lastloginat" => descending ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
                "status" => descending ? query.OrderByDescending(u => u.Status) : query.OrderBy(u => u.Status),
                _ => descending ? query.OrderByDescending(u => u.CreatedDate) : query.OrderBy(u => u.CreatedDate)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminUserListItemDto>
            {
                Items = users.Select(ToListItem).ToList(),
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasNextPage = page * pageSize < totalCount,
                CurrentPage = page
            };
        }

        public async Task<AdminUserDetailsDto> GetUserAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var user = await LoadUserAsync(userId, cancellationToken);
            return ToDetails(user);
        }

        public async Task<int> CreateUserAsync(
            AdminCreateUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            var userName = request.UserName.Trim();
            var email = request.Email.Trim();
            if (await _db.Users.AnyAsync(u => u.NormalizedUsername == Normalize(userName), cancellationToken))
            {
                throw Conflict("USERNAME_ALREADY_EXISTS", "Ce nom d'utilisateur est déjà utilisé.");
            }

            if (await _db.Users.AnyAsync(u => u.NormalizedEmail == Normalize(email), cancellationToken))
            {
                throw Conflict("EMAIL_ALREADY_EXISTS", "Cette adresse e-mail est déjà utilisée.");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var user = new User
            {
                Username = userName,
                NormalizedUsername = Normalize(userName),
                Email = email,
                NormalizedEmail = Normalize(email),
                PhoneNumber = AuthenticationAccountService.NormalizePhoneNumber(request.PhoneNumber ?? ""),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                CreatedDate = now,
                Status = request.Status == UserStatus.Active ? UserStatus.PendingEmailConfirmation : request.Status,
                EmailConfirmed = false,
                AccountExpiresAt = request.AccountExpiresAt,
                MustChangePassword = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            await SetRolesAsync(user, request.RoleIds, actingUserId, cancellationToken);
            await AddAuditAsync("USER_CREATED", actingUserId, actingUsername, user.Id, null, "Création administrative", new
            {
                user.Username,
                user.Email,
                request.RoleIds
            }, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (request.SendActivationEmail)
            {
                await _accountService.ResendConfirmationEmailAsync(
                    new ResendConfirmationEmailRequestDto { Email = user.Email },
                    null,
                    cancellationToken);
            }

            return user.Id;
        }

        public async Task UpdateUserAsync(
            int userId,
            AdminUpdateUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            var user = await LoadUserAsync(userId, cancellationToken);
            ApplyRowVersion(user, request.RowVersion);

            var newUserName = request.UserName.Trim();
            var newEmail = request.Email.Trim();
            var normalizedUserName = Normalize(newUserName);
            var normalizedEmail = Normalize(newEmail);

            if (await _db.Users.AnyAsync(u => u.Id != user.Id && u.NormalizedUsername == normalizedUserName, cancellationToken))
            {
                throw Conflict("USERNAME_ALREADY_EXISTS", "Ce nom d'utilisateur est déjà utilisé.");
            }

            if (await _db.Users.AnyAsync(u => u.Id != user.Id && u.NormalizedEmail == normalizedEmail, cancellationToken))
            {
                throw Conflict("EMAIL_ALREADY_EXISTS", "Cette adresse e-mail est déjà utilisée.");
            }

            var emailChanged = user.NormalizedEmail != normalizedEmail;
            user.Username = newUserName;
            user.NormalizedUsername = normalizedUserName;
            user.Email = newEmail;
            user.NormalizedEmail = normalizedEmail;
            user.PhoneNumber = AuthenticationAccountService.NormalizePhoneNumber(request.PhoneNumber ?? "");
            user.AccountExpiresAt = request.AccountExpiresAt;
            user.UpdatedDate = DateTime.UtcNow;

            if (emailChanged)
            {
                user.EmailConfirmed = false;
                user.EmailConfirmedAt = null;
                user.Status = UserStatus.PendingEmailConfirmation;
                user.SessionVersion += 1;
            }

            await AddAuditAsync("USER_UPDATED", actingUserId, actingUsername, user.Id, null, emailChanged ? "Changement d'adresse e-mail" : "Modification utilisateur", new
            {
                emailChanged,
                user.Username,
                user.Email
            }, cancellationToken);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw Functional("CONCURRENT_UPDATE", "Les données ont été modifiées par un autre administrateur.", StatusCodes.Status409Conflict);
            }

            if (emailChanged)
            {
                await _accountService.ResendConfirmationEmailAsync(
                    new ResendConfirmationEmailRequestDto { Email = user.Email },
                    null,
                    cancellationToken);
            }
        }

        public async Task AssignRolesAsync(
            int userId,
            AssignRolesRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(request.Reason);
            var user = await LoadUserAsync(userId, cancellationToken);
            ApplyRowVersion(user, request.RowVersion);

            if (userId == actingUserId)
            {
                await EnsureSelfStillAdminAsync(user, request.RoleIds, cancellationToken);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            await SetRolesAsync(user, request.RoleIds, actingUserId, cancellationToken);
            user.SessionVersion += 1;
            user.UpdatedDate = DateTime.UtcNow;
            await AddAuditAsync("USER_ROLES_ASSIGNED", actingUserId, actingUsername, user.Id, null, request.Reason, new
            {
                request.RoleIds
            }, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public async Task SetUserPersonLinkAsync(
            int userId,
            LinkUserPersonRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(request.Reason);
            var user = await LoadUserAsync(userId, cancellationToken);
            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(cancellationToken)
                : null;

            var currentPerson = await _db.Persons
                .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

            if (!request.PersonId.HasValue)
            {
                if (currentPerson == null)
                {
                    throw Functional("USER_PERSON_LINK_NOT_FOUND", "Cet utilisateur n'est rattaché à aucune personne.", StatusCodes.Status404NotFound);
                }

                currentPerson.UserId = null;
                currentPerson.UpdatedDate = DateTime.UtcNow;
                user.UpdatedDate = DateTime.UtcNow;
                await AddAuditAsync("USER_PERSON_UNLINKED", actingUserId, actingUsername, user.Id, null, request.Reason, new
                {
                    personId = currentPerson.Id
                }, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                if (transaction != null) await transaction.CommitAsync(cancellationToken);
                return;
            }

            var targetPerson = await _db.Persons
                .FirstOrDefaultAsync(p => p.Id == request.PersonId.Value, cancellationToken)
                ?? throw NotFound("PERSON_NOT_FOUND", "Personne introuvable.");

            if (targetPerson.UserId.HasValue && targetPerson.UserId.Value != user.Id)
            {
                throw Conflict("PERSON_ALREADY_LINKED", "Cette personne est déjà rattachée à un autre utilisateur.");
            }

            if (currentPerson != null && currentPerson.Id != targetPerson.Id)
            {
                currentPerson.UserId = null;
                currentPerson.UpdatedDate = DateTime.UtcNow;
            }

            targetPerson.UserId = user.Id;
            targetPerson.UpdatedDate = DateTime.UtcNow;
            user.UpdatedDate = DateTime.UtcNow;

            await AddAuditAsync("USER_PERSON_LINKED", actingUserId, actingUsername, user.Id, null, request.Reason, new
            {
                personId = targetPerson.Id,
                previousPersonId = currentPerson?.Id == targetPerson.Id ? null : currentPerson?.Id
            }, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
        }

        public async Task SuspendUserAsync(
            int userId,
            SuspendUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(request.Reason);
            EnsureNotSelf(userId, actingUserId, "CANNOT_SUSPEND_SELF", "Vous ne pouvez pas suspendre votre propre compte.");
            var user = await LoadUserAsync(userId, cancellationToken);
            if (user.Status == UserStatus.Suspended)
            {
                throw Functional("USER_ALREADY_SUSPENDED", "Cet utilisateur est déjà suspendu.", StatusCodes.Status409Conflict);
            }

            user.Status = UserStatus.Suspended;
            user.SuspendedAt = DateTime.UtcNow;
            user.SuspensionEndsAt = request.SuspensionEndsAt;
            user.SuspensionReason = request.Reason;
            user.UpdatedDate = DateTime.UtcNow;
            if (request.InvalidateSessions) user.SessionVersion += 1;
            await AddAuditAsync("USER_SUSPENDED", actingUserId, actingUsername, user.Id, null, request.Reason, request, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ReactivateUserAsync(
            int userId,
            ReasonedUserActionRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(request.Reason);
            var user = await LoadUserAsync(userId, cancellationToken);
            if (user.Status != UserStatus.Suspended && user.Status != UserStatus.Locked)
            {
                throw Functional("USER_NOT_SUSPENDED", "Cet utilisateur n'est pas suspendu.", StatusCodes.Status409Conflict);
            }

            user.Status = user.EmailConfirmed ? UserStatus.Active : UserStatus.PendingEmailConfirmation;
            user.SuspendedAt = null;
            user.SuspensionEndsAt = null;
            user.SuspensionReason = null;
            user.LockedUntil = null;
            user.FailedLoginAttempts = 0;
            user.UpdatedDate = DateTime.UtcNow;
            await AddAuditAsync("USER_REACTIVATED", actingUserId, actingUsername, user.Id, null, request.Reason, null, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RevokeUserAsync(
            int userId,
            RevokeUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(request.Reason);
            EnsureNotSelf(userId, actingUserId, "CANNOT_REVOKE_SELF", "Vous ne pouvez pas révoquer votre propre compte.");
            var user = await LoadUserAsync(userId, cancellationToken);
            if (user.Status == UserStatus.Revoked)
            {
                throw Functional("USER_ALREADY_REVOKED", "Cet utilisateur est déjà révoqué.", StatusCodes.Status409Conflict);
            }

            await EnsureLastSuperAdministratorProtectedAsync(user, cancellationToken);
            user.Status = UserStatus.Revoked;
            user.RevokedAt = DateTime.UtcNow;
            user.RevocationReason = request.Reason;
            user.UpdatedDate = DateTime.UtcNow;
            if (request.InvalidateSessions) user.SessionVersion += 1;
            await RevokeActiveTokensAsync(user.Id, cancellationToken);
            await AddAuditAsync("USER_REVOKED", actingUserId, actingUsername, user.Id, null, request.Reason, request, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RestoreUserAsync(
            int userId,
            RestoreUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(request.Reason);
            var user = await LoadUserAsync(userId, cancellationToken);
            if (user.Status != UserStatus.Revoked)
            {
                throw Functional("USER_CANNOT_BE_RESTORED", "Seul un utilisateur révoqué peut être restauré.", StatusCodes.Status409Conflict);
            }

            user.Status = user.EmailConfirmed ? UserStatus.Active : UserStatus.PendingEmailConfirmation;
            user.RevokedAt = null;
            user.RevocationReason = null;
            user.MustChangePassword = request.ForcePasswordReset;
            user.SessionVersion += 1;
            user.UpdatedDate = DateTime.UtcNow;
            await AddAuditAsync("USER_RESTORED", actingUserId, actingUsername, user.Id, null, request.Reason, request, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task LockUserAsync(
            int userId,
            LockUserRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(request.Reason);
            EnsureNotSelf(userId, actingUserId, "CANNOT_MODIFY_SELF", "Vous ne pouvez pas verrouiller votre propre compte.");
            var user = await LoadUserAsync(userId, cancellationToken);
            user.Status = UserStatus.Locked;
            user.LockedUntil = request.LockedUntil;
            user.SessionVersion += 1;
            user.UpdatedDate = DateTime.UtcNow;
            await AddAuditAsync("USER_LOCKED", actingUserId, actingUsername, user.Id, null, request.Reason, request, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UnlockUserAsync(
            int userId,
            ReasonedUserActionRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(request.Reason);
            var user = await LoadUserAsync(userId, cancellationToken);
            if (user.Status != UserStatus.Locked && user.LockedUntil == null && user.FailedLoginAttempts == 0)
            {
                throw Functional("USER_NOT_LOCKED", "Cet utilisateur n'est pas verrouillé.", StatusCodes.Status409Conflict);
            }

            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            if (user.Status == UserStatus.Locked)
            {
                user.Status = user.EmailConfirmed ? UserStatus.Active : UserStatus.PendingEmailConfirmation;
            }
            user.UpdatedDate = DateTime.UtcNow;
            await AddAuditAsync("USER_UNLOCKED", actingUserId, actingUsername, user.Id, null, request.Reason, null, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ForcePasswordResetAsync(
            int userId,
            ForcePasswordResetRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(request.Reason);
            var user = await LoadUserAsync(userId, cancellationToken);
            user.MustChangePassword = true;
            user.UpdatedDate = DateTime.UtcNow;
            if (request.InvalidateSessions) user.SessionVersion += 1;
            await AddAuditAsync("USER_FORCE_PASSWORD_RESET", actingUserId, actingUsername, user.Id, null, request.Reason, request, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            if (request.SendEmail)
            {
                await _accountService.RequestPasswordResetAsync(
                    new ForgotPasswordRequestDto { Email = user.Email },
                    null,
                    cancellationToken);
            }
        }

        public async Task InvalidateSessionsAsync(
            int userId,
            InvalidateUserSessionsRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(request.Reason);
            var user = await LoadUserAsync(userId, cancellationToken);
            user.SessionVersion += 1;
            user.SessionsInvalidatedAt = DateTime.UtcNow;
            user.UpdatedDate = DateTime.UtcNow;
            await AddAuditAsync("USER_SESSIONS_INVALIDATED", actingUserId, actingUsername, user.Id, null, request.Reason, null, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ResendConfirmationEmailAsync(
            int userId,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            var user = await LoadUserAsync(userId, cancellationToken);
            await _accountService.ResendConfirmationEmailAsync(
                new ResendConfirmationEmailRequestDto { Email = user.Email },
                null,
                cancellationToken);
            await AddAuditAsync("USER_CONFIRMATION_EMAIL_RESENT", actingUserId, actingUsername, user.Id, null, "Renvoi confirmation", null, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ConfirmEmailAdministrativelyAsync(
            int userId,
            ReasonedUserActionRequest request,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(request.Reason);
            var user = await LoadUserAsync(userId, cancellationToken);
            user.EmailConfirmed = true;
            user.EmailConfirmedAt = DateTime.UtcNow;
            if (user.Status == UserStatus.PendingEmailConfirmation)
            {
                user.Status = UserStatus.Active;
            }
            user.UpdatedDate = DateTime.UtcNow;
            await AddAuditAsync("USER_EMAIL_CONFIRMED_ADMIN", actingUserId, actingUsername, user.Id, null, request.Reason, null, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<AdminAuditEventDto>> GetUserAuditAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _db.AdminAuditEvents
                .AsNoTracking()
                .Where(e => e.TargetUserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(200)
                .Select(e => new AdminAuditEventDto(
                    e.Id,
                    e.ActingUserId,
                    e.ActingUsername,
                    e.TargetUserId,
                    e.TargetRoleId,
                    e.Action,
                    e.Reason,
                    e.ResultCode,
                    e.DetailsJson,
                    e.CreatedAt))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<AdminRoleSummaryDto>> GetRolesAsync(
            CancellationToken cancellationToken = default) =>
            await _db.Roles
                .AsNoTracking()
                .Include(r => r.RolePermissions)
                .OrderByDescending(r => r.PrivilegeRank)
                .ThenBy(r => r.RoleName)
                .Select(r => new AdminRoleSummaryDto(
                    r.Id,
                    r.RoleCode,
                    r.RoleName,
                    r.Description,
                    r.IsSystem,
                    r.PrivilegeRank,
                    r.RolePermissions.Select(rp => rp.PermissionId).ToList()))
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<AdminPermissionDto>> GetPermissionsAsync(
            CancellationToken cancellationToken = default) =>
            await _db.Permissions
                .AsNoTracking()
                .OrderBy(p => p.PermissionCode)
                .Select(p => new AdminPermissionDto(p.Id, p.PermissionCode, p.PermissionName, p.Description, p.IsSystem))
                .ToListAsync(cancellationToken);

        public async Task<AdminRoleSummaryDto> CreateRoleAsync(
            Role role,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            role.RoleCode = role.RoleCode.Trim();
            role.RoleName = role.RoleName.Trim();
            role.Description = role.Description?.Trim() ?? string.Empty;
            role.CreatedDate = DateTime.UtcNow;
            if (await _db.Roles.AnyAsync(r => r.RoleCode == role.RoleCode, cancellationToken))
            {
                throw Conflict("ROLE_ALREADY_EXISTS", "Ce rôle existe déjà.");
            }
            _db.Roles.Add(role);
            await _db.SaveChangesAsync(cancellationToken);
            await AddAuditAsync("ROLE_CREATED", actingUserId, actingUsername, null, role.Id, "Création rôle", new { role.RoleCode }, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return new AdminRoleSummaryDto(role.Id, role.RoleCode, role.RoleName, role.Description, role.IsSystem, role.PrivilegeRank, Array.Empty<int>());
        }

        public async Task<AdminRoleSummaryDto> UpdateRoleAsync(
            int roleId,
            Role role,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            var existing = await _db.Roles.FindAsync(new object[] { roleId }, cancellationToken)
                ?? throw NotFound("ROLE_NOT_FOUND", "Rôle introuvable.");
            if (existing.IsSystem && existing.RoleCode != role.RoleCode)
            {
                throw Functional("ROLE_IS_SYSTEM_PROTECTED", "Le code d'un rôle système ne peut pas être modifié.", StatusCodes.Status409Conflict);
            }

            var nextRoleCode = role.RoleCode.Trim();
            if (!existing.RoleCode.Equals(nextRoleCode, StringComparison.OrdinalIgnoreCase)
                && await _db.Roles.AnyAsync(r => r.Id != roleId && r.RoleCode == nextRoleCode, cancellationToken))
            {
                throw Conflict("ROLE_ALREADY_EXISTS", "Ce rôle existe déjà.");
            }

            if (!existing.IsSystem)
            {
                existing.RoleCode = nextRoleCode;
            }
            existing.RoleName = role.RoleName.Trim();
            existing.Description = role.Description?.Trim() ?? string.Empty;
            existing.PrivilegeRank = role.PrivilegeRank;
            existing.UpdatedDate = DateTime.UtcNow;
            await AddAuditAsync("ROLE_UPDATED", actingUserId, actingUsername, null, existing.Id, "Modification rôle", new { existing.RoleCode }, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return new AdminRoleSummaryDto(existing.Id, existing.RoleCode, existing.RoleName, existing.Description, existing.IsSystem, existing.PrivilegeRank, Array.Empty<int>());
        }

        public async Task DeleteRoleAsync(
            int roleId,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            var role = await _db.Roles
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
                ?? throw NotFound("ROLE_NOT_FOUND", "Rôle introuvable.");
            if (role.IsSystem)
            {
                throw Functional("ROLE_IS_SYSTEM_PROTECTED", "Un rôle système ne peut pas être supprimé.", StatusCodes.Status409Conflict);
            }
            if (role.UserRoles.Any())
            {
                throw Functional("ROLE_STILL_ASSIGNED", "Ce rôle est encore affecté à des utilisateurs.", StatusCodes.Status409Conflict);
            }
            _db.Roles.Remove(role);
            await AddAuditAsync("ROLE_DELETED", actingUserId, actingUsername, null, role.Id, "Suppression rôle", new { role.RoleCode }, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateRolePermissionsAsync(
            int roleId,
            IReadOnlyCollection<int> permissionIds,
            string reason,
            int? actingUserId,
            string? actingUsername,
            CancellationToken cancellationToken = default)
        {
            RequireReason(reason);
            var role = await _db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
                ?? throw NotFound("ROLE_NOT_FOUND", "Rôle introuvable.");

            if (role.RoleCode == SystemRoles.SuperAdministrator && actingUsername != null)
            {
                // The policy still protects this endpoint; this guard keeps the critical role visible in code.
                _logger.LogInformation("Modification des permissions SuperAdministrator par {User}", actingUsername);
            }

            _db.RolePermissions.RemoveRange(role.RolePermissions);
            foreach (var permissionId in permissionIds.Distinct())
            {
                _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
            }
            await AddAuditAsync("ROLE_PERMISSIONS_UPDATED", actingUserId, actingUsername, null, role.Id, reason, new { permissionIds }, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<User> LoadUserAsync(int userId, CancellationToken cancellationToken) =>
            await _db.Users
                .Include(u => u.Person)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r!.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw NotFound("USER_NOT_FOUND", "Utilisateur introuvable.");

        private async Task SetRolesAsync(
            User user,
            IReadOnlyCollection<int> roleIds,
            int? actingUserId,
            CancellationToken cancellationToken)
        {
            var roles = await _db.Roles
                .Where(r => roleIds.Contains(r.Id))
                .ToListAsync(cancellationToken);
            if (roles.Count != roleIds.Distinct().Count())
            {
                throw NotFound("ROLE_NOT_FOUND", "Un rôle demandé est introuvable.");
            }

            if (roles.Any(r => r.RoleCode == SystemRoles.SuperAdministrator))
            {
                var actorIsSuperAdmin = actingUserId.HasValue
                    && await _db.UserRoles.AnyAsync(ur =>
                        ur.UserId == actingUserId.Value
                        && ur.Role != null
                        && ur.Role.RoleCode == SystemRoles.SuperAdministrator,
                        cancellationToken);
                if (!actorIsSuperAdmin)
                {
                    throw Functional("INSUFFICIENT_PERMISSION", "Seul un super-administrateur peut attribuer ce rôle.", StatusCodes.Status403Forbidden);
                }
            }

            _db.UserRoles.RemoveRange(_db.UserRoles.Where(ur => ur.UserId == user.Id));
            foreach (var role in roles)
            {
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            }
        }

        private async Task EnsureSelfStillAdminAsync(
            User user,
            IReadOnlyCollection<int> requestedRoleIds,
            CancellationToken cancellationToken)
        {
            var adminRoleIds = await _db.Roles
                .Where(r => r.RolePermissions.Any(rp =>
                    rp.Permission != null
                    && (rp.Permission.PermissionCode == SystemPermissions.UsersView
                        || rp.Permission.PermissionCode == SystemPermissions.UsersUpdate)))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            if (!requestedRoleIds.Any(adminRoleIds.Contains))
            {
                throw Functional("CANNOT_REMOVE_LAST_ADMIN_ROLE", "Vous ne pouvez pas retirer votre dernier rôle d'administration.", StatusCodes.Status409Conflict);
            }
        }

        private async Task EnsureLastSuperAdministratorProtectedAsync(
            User user,
            CancellationToken cancellationToken)
        {
            if (!user.UserRoles.Any(ur => ur.Role?.RoleCode == SystemRoles.SuperAdministrator))
            {
                return;
            }

            var activeSuperAdmins = await _db.Users
                .CountAsync(u =>
                    u.Id != user.Id
                    && u.Status == UserStatus.Active
                    && u.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleCode == SystemRoles.SuperAdministrator),
                    cancellationToken);

            if (activeSuperAdmins == 0)
            {
                throw Functional("LAST_SUPER_ADMINISTRATOR", "Il doit rester au moins un super-administrateur actif.", StatusCodes.Status409Conflict);
            }
        }

        private async Task RevokeActiveTokensAsync(int userId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var tokens = await _db.UserSecurityTokens
                .Where(t => t.UserId == userId && t.UsedAt == null && t.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var token in tokens)
            {
                token.RevokedAt = now;
            }
        }

        private async Task AddAuditAsync(
            string action,
            int? actingUserId,
            string? actingUsername,
            int? targetUserId,
            int? targetRoleId,
            string? reason,
            object? details,
            CancellationToken cancellationToken)
        {
            _db.AdminAuditEvents.Add(new AdminAuditEvent
            {
                ActingUserId = actingUserId,
                ActingUsername = actingUsername,
                TargetUserId = targetUserId,
                TargetRoleId = targetRoleId,
                Action = action,
                Reason = reason,
                ResultCode = "OK",
                DetailsJson = details == null ? null : JsonSerializer.Serialize(details),
                CreatedAt = DateTime.UtcNow
            });
            await Task.CompletedTask;
        }

        private static AdminUserListItemDto ToListItem(User user)
        {
            var locked = user.Status == UserStatus.Locked
                || (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow);
            return new AdminUserListItemDto(
                user.Id,
                user.Username,
                user.Email,
                user.PhoneNumber,
                user.Status.ToString(),
                user.EmailConfirmed,
                user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.RoleCode).ToList(),
                user.CreatedDate,
                user.LastLoginAt,
                user.LastActivityAt,
                user.AccountExpiresAt,
                locked,
                ToLinkedPerson(user.Person));
        }

        private static AdminUserDetailsDto ToDetails(User user)
        {
            var roles = user.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => new AdminRoleSummaryDto(
                    ur.Role!.Id,
                    ur.Role.RoleCode,
                    ur.Role.RoleName,
                    ur.Role.Description,
                    ur.Role.IsSystem,
                    ur.Role.PrivilegeRank,
                    ur.Role.RolePermissions.Select(rp => rp.PermissionId).ToList()))
                .ToList();

            var permissions = user.UserRoles
                .Where(ur => ur.Role != null)
                .SelectMany(ur => ur.Role!.RolePermissions)
                .Where(rp => rp.Permission != null)
                .Select(rp => rp.Permission!.PermissionCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code)
                .ToList();

            return new AdminUserDetailsDto(
                user.Id,
                user.Username,
                user.Email,
                user.PhoneNumber,
                user.Status.ToString(),
                user.EmailConfirmed,
                user.EmailConfirmedAt,
                roles,
                permissions,
                user.CreatedDate,
                user.UpdatedDate,
                user.LastLoginAt,
                user.LastActivityAt,
                user.SuspendedAt,
                user.SuspensionEndsAt,
                user.SuspensionReason,
                user.RevokedAt,
                user.RevocationReason,
                user.AccountExpiresAt,
                user.MustChangePassword,
                user.PasswordChangedAt,
                user.FailedLoginAttempts,
                user.LockedUntil,
                user.SessionVersion,
                Convert.ToBase64String(user.RowVersion),
                ToLinkedPerson(user.Person));
        }

        private static AdminLinkedPersonDto? ToLinkedPerson(Person? person) =>
            person == null
                ? null
                : new AdminLinkedPersonDto(
                    person.Id,
                    person.FirstName,
                    person.LastName,
                    $"{person.FirstName} {person.LastName}".Trim(),
                    string.IsNullOrWhiteSpace(person.Email1) ? null : person.Email1,
                    string.IsNullOrWhiteSpace(person.PhoneNumber) ? null : person.PhoneNumber,
                    person.Role,
                    person.Status);

        private void ApplyRowVersion(User user, string rowVersion)
        {
            if (string.IsNullOrWhiteSpace(rowVersion))
            {
                throw Functional("CONCURRENT_UPDATE", "La version de l'utilisateur est obligatoire.", StatusCodes.Status409Conflict);
            }
            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);
        }

        private static void RequireReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 4)
            {
                throw Functional("ACTION_REASON_REQUIRED", "Une justification est obligatoire.", StatusCodes.Status400BadRequest);
            }
        }

        private static void EnsureNotSelf(int userId, int? actingUserId, string code, string message)
        {
            if (actingUserId.HasValue && actingUserId.Value == userId)
            {
                throw Functional(code, message, StatusCodes.Status409Conflict);
            }
        }

        private static string Normalize(string value) => value.Trim().ToUpperInvariant();

        private static AdminFunctionalException Functional(string code, string message, int statusCode) =>
            new(code, message, statusCode);

        private static AdminFunctionalException NotFound(string code, string message) =>
            new(code, message, StatusCodes.Status404NotFound);

        private static AdminFunctionalException Conflict(string code, string message) =>
            new(code, message, StatusCodes.Status409Conflict);
    }

    public sealed class AdminFunctionalException : Exception
    {
        public AdminFunctionalException(string code, string message, int statusCode)
            : base(message)
        {
            Code = code;
            StatusCode = statusCode;
        }

        public string Code { get; }

        public int StatusCode { get; }
    }
}
