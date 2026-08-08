namespace api.Security
{
    public static class AuthorizationPolicies
    {
        public const string ManageUsers = "ManageUsers";
        public const string ViewUsers = "ViewUsers";
        public const string ManageUserRoles = "ManageUserRoles";
        public const string SuspendUsers = "SuspendUsers";
        public const string RevokeUsers = "RevokeUsers";
        public const string ManageRoles = "ManageRoles";
        public const string ViewRoles = "ViewRoles";
        public const string ViewSecurityAudit = "ViewSecurityAudit";
    }

    public static class SystemPermissions
    {
        public const string UsersView = "users.view";
        public const string UsersCreate = "users.create";
        public const string UsersUpdate = "users.update";
        public const string UsersAssignRoles = "users.assign-roles";
        public const string UsersSuspend = "users.suspend";
        public const string UsersReactivate = "users.reactivate";
        public const string UsersRevoke = "users.revoke";
        public const string UsersRestore = "users.restore";
        public const string UsersUnlock = "users.unlock";
        public const string UsersLock = "users.lock";
        public const string UsersForcePasswordReset = "users.force-password-reset";
        public const string UsersInvalidateSessions = "users.invalidate-sessions";
        public const string UsersConfirmEmail = "users.confirm-email";
        public const string UsersExport = "users.export";
        public const string RolesView = "roles.view";
        public const string RolesCreate = "roles.create";
        public const string RolesUpdate = "roles.update";
        public const string RolesDelete = "roles.delete";
        public const string RolesAssignPermissions = "roles.assign-permissions";
        public const string AuditView = "audit.view";

        public static readonly IReadOnlyList<string> UserManagerPermissions =
        [
            UsersView,
            UsersCreate,
            UsersUpdate,
            UsersAssignRoles,
            UsersSuspend,
            UsersReactivate,
            UsersUnlock,
            UsersForcePasswordReset,
            UsersInvalidateSessions,
            UsersConfirmEmail,
            RolesView
        ];

        public static readonly IReadOnlyList<string> AdministratorPermissions =
        [
            .. UserManagerPermissions,
            UsersRevoke,
            UsersRestore,
            UsersLock,
            UsersExport,
            RolesCreate,
            RolesUpdate,
            RolesAssignPermissions,
            AuditView
        ];

        public static readonly IReadOnlyList<string> SuperAdministratorPermissions =
        [
            .. AdministratorPermissions,
            RolesDelete
        ];
    }

    public static class SystemRoles
    {
        public const string SuperAdministrator = "SuperAdministrator";
        public const string Administrator = "Administrator";
        public const string UserManager = "UserManager";
        public const string SecurityAuditor = "SecurityAuditor";
        public const string StandardUser = "StandardUser";
        public const string Cartography = "Cartographie";
        public const string Donor = "Donateur";
        public const string LegacyAdmin = "Admin";
        public const string LegacyUser = "User";
    }
}
