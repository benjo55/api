using api.Data;
using api.Models;
using api.Security;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public sealed class AuthorizationSeedService
    {
        private readonly ApplicationDBContext _db;
        private readonly ILogger<AuthorizationSeedService> _logger;

        public AuthorizationSeedService(
            ApplicationDBContext db,
            ILogger<AuthorizationSeedService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            var permissions = new Dictionary<string, string>
            {
                [SystemPermissions.UsersView] = "Voir les utilisateurs",
                [SystemPermissions.UsersCreate] = "Créer des utilisateurs",
                [SystemPermissions.UsersUpdate] = "Modifier les utilisateurs",
                [SystemPermissions.UsersAssignRoles] = "Affecter des rôles",
                [SystemPermissions.UsersSuspend] = "Suspendre des utilisateurs",
                [SystemPermissions.UsersReactivate] = "Réactiver des utilisateurs",
                [SystemPermissions.UsersRevoke] = "Révoquer des utilisateurs",
                [SystemPermissions.UsersRestore] = "Restaurer des utilisateurs",
                [SystemPermissions.UsersUnlock] = "Déverrouiller des utilisateurs",
                [SystemPermissions.UsersLock] = "Verrouiller des utilisateurs",
                [SystemPermissions.UsersForcePasswordReset] = "Forcer une réinitialisation du mot de passe",
                [SystemPermissions.UsersInvalidateSessions] = "Invalider les sessions utilisateur",
                [SystemPermissions.UsersConfirmEmail] = "Confirmer ou renvoyer une confirmation e-mail",
                [SystemPermissions.UsersExport] = "Exporter la liste des utilisateurs",
                [SystemPermissions.RolesView] = "Voir les rôles",
                [SystemPermissions.RolesCreate] = "Créer des rôles",
                [SystemPermissions.RolesUpdate] = "Modifier les rôles",
                [SystemPermissions.RolesDelete] = "Supprimer des rôles",
                [SystemPermissions.RolesAssignPermissions] = "Affecter des permissions aux rôles",
                [SystemPermissions.AuditView] = "Voir l'audit de sécurité"
            };

            foreach (var permission in permissions)
            {
                var existing = await _db.Permissions
                    .FirstOrDefaultAsync(p => p.PermissionCode == permission.Key, cancellationToken);

                if (existing == null)
                {
                    _db.Permissions.Add(new Permission
                    {
                        PermissionCode = permission.Key,
                        PermissionName = permission.Value,
                        Description = permission.Value,
                        IsSystem = true
                    });
                }
                else
                {
                    existing.PermissionName = permission.Value;
                    existing.Description = string.IsNullOrWhiteSpace(existing.Description)
                        ? permission.Value
                        : existing.Description;
                    existing.IsSystem = true;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            var roleDefinitions = new[]
            {
                new RoleDefinition(SystemRoles.SuperAdministrator, "Super administrateur", 100, SystemPermissions.SuperAdministratorPermissions),
                new RoleDefinition(SystemRoles.Administrator, "Administrateur", 80, SystemPermissions.AdministratorPermissions),
                new RoleDefinition(SystemRoles.UserManager, "Gestionnaire utilisateurs", 60, SystemPermissions.UserManagerPermissions),
                new RoleDefinition(SystemRoles.SecurityAuditor, "Auditeur sécurité", 40, new[] { SystemPermissions.UsersView, SystemPermissions.RolesView, SystemPermissions.AuditView }),
                new RoleDefinition(SystemRoles.StandardUser, "Utilisateur standard", 10, Array.Empty<string>()),
                new RoleDefinition(SystemRoles.Cartography, "Cartographie", 10, Array.Empty<string>()),
                new RoleDefinition(SystemRoles.Donor, "Donateur", 10, Array.Empty<string>()),
                new RoleDefinition(SystemRoles.LegacyUser, "Utilisateur", 10, Array.Empty<string>()),
                new RoleDefinition(SystemRoles.LegacyAdmin, "Administrateur historique", 80, SystemPermissions.AdministratorPermissions)
            };

            foreach (var definition in roleDefinitions)
            {
                var role = await _db.Roles
                    .Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.RoleCode == definition.Code, cancellationToken);

                if (role == null)
                {
                    role = new Role
                    {
                        RoleCode = definition.Code,
                        RoleName = definition.Name,
                        Description = definition.Name,
                        CreatedDate = DateTime.UtcNow,
                        IsSystem = true,
                        PrivilegeRank = definition.PrivilegeRank
                    };
                    _db.Roles.Add(role);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    role.RoleName = string.IsNullOrWhiteSpace(role.RoleName) ? definition.Name : role.RoleName;
                    role.IsSystem = true;
                    role.PrivilegeRank = Math.Max(role.PrivilegeRank, definition.PrivilegeRank);
                }

                var permissionIds = await _db.Permissions
                    .Where(p => definition.PermissionCodes.Contains(p.PermissionCode))
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);

                foreach (var permissionId in permissionIds)
                {
                    if (!role.RolePermissions.Any(rp => rp.PermissionId == permissionId))
                    {
                        role.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = permissionId
                        });
                    }
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Catalogue rôles/permissions d'administration synchronisé.");
        }

        private sealed record RoleDefinition(
            string Code,
            string Name,
            int PrivilegeRank,
            IReadOnlyCollection<string> PermissionCodes);
    }
}
