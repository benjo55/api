using Microsoft.AspNetCore.Mvc;
using api.Interfaces;
using api.Models;
using api.Dtos.Generic;
using api.Helpers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using api.Data;

namespace api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")] // Seuls les admins peuvent gérer les utilisateurs
    public class UserController : ControllerBase
    {

        private readonly ApplicationDBContext _context;
        private readonly IUserRepository _userRepository;

        public UserController(ApplicationDBContext context, IUserRepository userRepository)
        {
            _userRepository = userRepository;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] QueryObject query)
        {
            var users = await _userRepository.GetAllAsync(query);
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (await _userRepository.UsernameExistsAsync(user.Username))
                return BadRequest("Ce nom d'utilisateur existe déjà.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash); // Hash du mot de passe
            var createdUser = await _userRepository.CreateAsync(user);
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User updatedUser)
        {
            var user = await _userRepository.UpdateAsync(id, updatedUser);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deletedUser = await _userRepository.DeleteAsync(id);
            if (deletedUser == null) return NotFound();
            return NoContent();
        }

        [HttpPut("{userId}/roles")]
        public async Task<IActionResult> UpdateUserRoles(int userId, [FromBody] UpdateUserRoleRequest request)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound(new { message = "Utilisateur introuvable" });

            // Supprime les anciens rôles
            _context.UserRoles.RemoveRange(_context.UserRoles.Where(ur => ur.UserId == userId));

            // Ajoute les nouveaux rôles
            if (request.Roles.Count > 0)
            {
                foreach (var roleId in request.Roles)
                {
                    _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Rôles mis à jour avec succès" });
        }

        // DTO pour recevoir la liste des permissions à mettre à jour
        public class UpdateUserRoleRequest
        {
            public List<int> Roles { get; set; } = new();
        }
    }
}
