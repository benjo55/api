using Microsoft.AspNetCore.Mvc;
using api.Interfaces;
using api.Models;
using api.Services;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;
using api.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using api.Data;

namespace api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly AuthService _authService;
        private readonly IRoleRepository _roleRepository;
        private readonly ApplicationDBContext _context;
        private readonly IAuthenticationAccountService _accountService;

        public AuthController(
            IUserRepository userRepository,
            IRolePermissionRepository rolePermissionRepository,
            AuthService authService,
            IRoleRepository roleRepository,
            ApplicationDBContext context,
            IAuthenticationAccountService accountService)
        {
            _userRepository = userRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _authService = authService;
            _roleRepository = roleRepository;
            _context = context;
            _accountService = accountService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto user, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _accountService.RegisterAsync(
                    user,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    cancellationToken);

                return Created("/api/auth/login", new
                {
                    id = result.UserId,
                    userName = result.UserName,
                    email = result.Email,
                    maskedEmail = result.MaskedEmail,
                    message = result.Message
                });
            }
            catch (AuthFunctionalException ex)
            {
                return StatusCode(ex.StatusCode, new
                {
                    field = ex.Field,
                    code = ex.Code,
                    message = ex.Message
                });
            }
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _accountService.ConfirmEmailAsync(request, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("resend-confirmation-email")]
        [HttpPost("resend-email-confirmation")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _accountService.ResendConfirmationEmailAsync(
                request,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _accountService.RequestPasswordResetAsync(
                request,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _accountService.ResetPasswordAsync(request, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto user)
        {
            var dbUser = await _userRepository.GetByUsernameOrEmailAsync(user.Username);
            if (dbUser == null)
            {
                return Unauthorized(new { message = "Nom d'utilisateur ou mot de passe invalide." });
            }

            var now = DateTime.UtcNow;
            if (dbUser.AccountExpiresAt.HasValue && dbUser.AccountExpiresAt.Value <= now)
            {
                dbUser.Status = UserStatus.Expired;
                await _context.SaveChangesAsync();
                return Unauthorized(new { code = "ACCOUNT_EXPIRED", message = "Votre compte a expiré. Contactez un administrateur." });
            }

            if (dbUser.LockedUntil.HasValue && dbUser.LockedUntil.Value <= now)
            {
                dbUser.LockedUntil = null;
                dbUser.FailedLoginAttempts = 0;
                if (dbUser.Status == UserStatus.Locked)
                {
                    dbUser.Status = dbUser.EmailConfirmed
                        ? UserStatus.Active
                        : UserStatus.PendingEmailConfirmation;
                }
            }

            if (dbUser.LockedUntil.HasValue && dbUser.LockedUntil.Value > now)
            {
                return Unauthorized(new { code = "USER_LOCKED", message = "Votre compte est temporairement verrouillé." });
            }

            if (dbUser.Status == UserStatus.Suspended)
            {
                return Unauthorized(new { code = "USER_SUSPENDED", message = "Votre compte est actuellement suspendu. Contactez un administrateur pour obtenir davantage d'informations." });
            }

            if (dbUser.Status == UserStatus.Revoked)
            {
                return Unauthorized(new { code = "USER_REVOKED", message = "Votre accès a été révoqué. Contactez un administrateur." });
            }

            if (dbUser.Status == UserStatus.Locked)
            {
                return Unauthorized(new { code = "USER_LOCKED", message = "Votre compte est verrouillé. Contactez un administrateur." });
            }

            if (!BCrypt.Net.BCrypt.Verify(user.Password, dbUser.PasswordHash))
            {
                dbUser.FailedLoginAttempts += 1;
                if (dbUser.FailedLoginAttempts >= 5)
                {
                    dbUser.Status = UserStatus.Locked;
                    dbUser.LockedUntil = now.AddMinutes(15);
                }
                await _context.SaveChangesAsync();
                return Unauthorized(new { message = "Nom d'utilisateur ou mot de passe invalide." });
            }

            if (!dbUser.EmailConfirmed)
            {
                dbUser.Status = UserStatus.PendingEmailConfirmation;
                await _context.SaveChangesAsync();
                return Unauthorized(new
                {
                    code = AuthenticationAccountService.EmailConfirmationRequiredCode,
                    message = "Votre adresse e-mail n'a pas encore été confirmée. Consultez votre messagerie ou demandez l'envoi d'un nouveau lien.",
                    email = dbUser.Email
                });
            }

            if (dbUser.Status == UserStatus.PendingEmailConfirmation)
            {
                dbUser.Status = UserStatus.Active;
            }

            if (dbUser.MustChangePassword)
            {
                await _context.SaveChangesAsync();
                return Unauthorized(new
                {
                    code = "PASSWORD_CHANGE_REQUIRED",
                    message = "Une réinitialisation du mot de passe est requise avant de pouvoir vous connecter."
                });
            }

            var roleIds = dbUser.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.RoleId)
                .ToList();

            var rolePermissionRows = await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId) && rp.Permission != null)
                .Select(rp => new
                {
                    rp.RoleId,
                    rp.PermissionId,
                    permissionCode = rp.Permission!.PermissionCode,
                    permissionName = rp.Permission!.PermissionName
                })
                .ToListAsync();

            var roles = dbUser.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.RoleCode) // Utilisation du null-forgiving operator '!'
                .ToList();

            var permissions = rolePermissionRows
                .Select(rp => rp.permissionCode)
                .Distinct()
                .ToList();

            var claims = new List<Claim>
            {
                new Claim("userId", dbUser.Id.ToString()),
                new Claim("sessionVersion", dbUser.SessionVersion.ToString()),
                new Claim("username", dbUser.Username),
                new Claim(ClaimTypes.Name, dbUser.Username),
                new Claim("email", dbUser.Email)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var token = _authService.GenerateJwtToken(claims);
            dbUser.LastLoginAt = now;
            dbUser.LastActivityAt = now;
            dbUser.FailedLoginAttempts = 0;
            dbUser.LockedUntil = null;
            await _context.SaveChangesAsync();

            var userResponse = new
            {
                id = dbUser.Id,
                firstName = dbUser.FirstName,
                lastName = dbUser.LastName,
                username = dbUser.Username,
                email = dbUser.Email,
                emailConfirmed = dbUser.EmailConfirmed,
                accountStatus = dbUser.Status.ToString(),
                lastLoginAt = dbUser.LastLoginAt,
                roles = dbUser.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => new
                    {
                        id = ur.Role!.Id,
                        roleCode = ur.Role!.RoleCode,
                        roleName = ur.Role!.RoleName,
                        description = ur.Role!.Description,
                        rolePermissions = rolePermissionRows
                            .Where(rp => rp.RoleId == ur.Role!.Id)
                            .Select(rp => new
                            {
                                rp.RoleId,
                                rp.PermissionId,
                                rp.permissionCode,
                                rp.permissionName
                            })
                            .ToList()
                    })
                    .ToList()
            };

            return Ok(new
            {
                token,
                username = dbUser.Username,
                email = dbUser.Email,
                roles,
                permissions,
                user = userResponse
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine("❌ Utilisateur non authentifié - Token invalide ?");
                return Unauthorized(new { message = "Utilisateur non authentifié." });
            }

            Console.WriteLine("🔍 Requête `/me` pour l'utilisateur : " + username);

            var dbUser = await _context.Users
                .Include(u => u.Person)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (dbUser == null)
            {
                Console.WriteLine("❌ Utilisateur introuvable !");
                return NotFound(new { message = "Utilisateur introuvable." });
            }

            // ✅ Vérification sécurisée pour éviter `null`
            var userResponse = new
            {
                id = dbUser.Id,
                firstName = dbUser.FirstName,
                lastName = dbUser.LastName,
                username = dbUser.Username,
                email = dbUser.Email,
                emailConfirmed = dbUser.EmailConfirmed,
                personId = dbUser.Person?.Id,
                accountStatus = dbUser.Status.ToString(),
                lastLoginAt = dbUser.LastLoginAt,
                accessibleSpaces = dbUser.Person == null
                    ? new[] { "Donations" }
                    : new[] { "PrivateSpace", "Contracts", "Donations" },
                roles = dbUser.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => new
                    {
                        roleCode = ur.Role!.RoleCode,
                        roleName = ur.Role!.RoleName,
                        RolePermissions = _context.RolePermissions // ✅ Requête séparée pour éviter `null`
                            .Where(rp => rp.RoleId == ur.Role!.Id && rp.Permission != null)
                            .Select(rp => new
                            {
                                permissionCode = rp.Permission!.PermissionCode,
                                permissionName = rp.Permission!.PermissionName
                            }).ToList()
                    })
                    .ToList()
            };

            Console.WriteLine($"✅ Utilisateur trouvé: {userResponse.username}");
            Console.WriteLine($"✅ Rôles trouvés: {string.Join(", ", userResponse.roles.Select(r => r.roleName))}");

            return Ok(userResponse);
        }

    }
}
