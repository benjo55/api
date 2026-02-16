using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace api.Dtos.Auth
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Le nom d'utilisateur est obligatoire.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "L'email n'est pas valide.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Au moins un rôle est obligatoire.")]
        public List<string> Roles { get; set; } = new();
    }
}
