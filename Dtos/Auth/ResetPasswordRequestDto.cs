using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Auth
{
    public sealed class ResetPasswordRequestDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire.")]
        [MinLength(10, ErrorMessage = "Le mot de passe doit contenir au moins 10 caractères.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation du mot de passe est obligatoire.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
