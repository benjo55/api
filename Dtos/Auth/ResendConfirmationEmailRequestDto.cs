using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Auth
{
    public sealed class ResendConfirmationEmailRequestDto
    {
        [Required(ErrorMessage = "L'adresse e-mail est obligatoire.")]
        [EmailAddress(ErrorMessage = "L'adresse e-mail n'est pas valide.")]
        public string Email { get; set; } = string.Empty;
    }
}
