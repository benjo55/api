using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Auth
{
    public sealed class ConfirmEmailRequestDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
