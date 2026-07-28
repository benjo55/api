using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Auth
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom d'utilisateur est obligatoire.")]
        [MinLength(3, ErrorMessage = "Le nom d'utilisateur doit contenir au moins 3 caractères.")]
        [MaxLength(100, ErrorMessage = "Le nom d'utilisateur ne peut pas dépasser 100 caractères.")]
        [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Le nom d'utilisateur contient des caractères non autorisés.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [EmailAddress(ErrorMessage = "L'adresse e-mail n'est pas valide.")]
        [MaxLength(254, ErrorMessage = "L'adresse e-mail ne peut pas dépasser 254 caractères.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le numéro de téléphone est obligatoire.")]
        [MaxLength(32, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 32 caractères.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [MinLength(10, ErrorMessage = "Le mot de passe doit contenir au moins 10 caractères.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'acceptation de la politique de confidentialité est obligatoire.")]
        public bool AcceptPrivacyPolicy { get; set; }
    }
}
