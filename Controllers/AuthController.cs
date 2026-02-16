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

        public AuthController(
            IUserRepository userRepository,
            IRolePermissionRepository rolePermissionRepository,
            AuthService authService,
            IRoleRepository roleRepository,
            ApplicationDBContext context)
        {
            _userRepository = userRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _authService = authService;
            _roleRepository = roleRepository;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto user)
        {
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password) || string.IsNullOrEmpty(user.Email))
            {
                return BadRequest(new { message = "Le nom d'utilisateur, l'email et le mot de passe sont requis." });
            }

            if (await _userRepository.UsernameExistsAsync(user.Username))
            {
                return BadRequest(new { message = "Nom d'utilisateur déjà pris." });
            }

            var roles = await _context.Roles
                .Where(r => user.Roles.Contains(r.RoleCode))
                .ToListAsync();

            if (!roles.Any())
            {
                return BadRequest(new { message = "Aucun rôle valide trouvé." });
            }

            var newUser = new User
            {
                Username = user.Username,
                Email = user.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password),
                UserRoles = roles.Select(role => new UserRole { RoleId = role.Id }).ToList()
            };

            await _userRepository.CreateAsync(newUser);
            return Ok(new { message = "Utilisateur créé avec succès." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto user)
        {
            var dbUser = await _userRepository.GetByUsernameAsync(user.Username);
            if (dbUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, dbUser.PasswordHash))
                return Unauthorized(new { message = "Nom d'utilisateur ou mot de passe invalide." });

            var roles = dbUser.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.RoleCode) // Utilisation du null-forgiving operator '!'
                .ToList();

            var permissions = await _context.RolePermissions
                .Where(rp => dbUser.UserRoles.Select(ur => ur.RoleId).Contains(rp.RoleId))
                .Select(rp => rp.Permission != null ? rp.Permission.PermissionCode : string.Empty)
                .Where(permissionCode => !string.IsNullOrEmpty(permissionCode))
                .Distinct()
                .ToListAsync();

            var claims = new List<Claim>
            {
                new Claim("username", dbUser.Username),
                new Claim("email", dbUser.Email)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            }

            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var token = _authService.GenerateJwtToken(claims);

            return Ok(new
            {
                token,
                username = dbUser.Username,
                email = dbUser.Email,
                roles,
                permissions
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
                username = dbUser.Username,
                email = dbUser.Email,
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
