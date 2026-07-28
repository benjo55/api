using api.Dtos.Auth;
using Microsoft.AspNetCore.Http;

namespace api.Interfaces
{
    public interface IAuthenticationAccountService
    {
        Task<RegisterAccountResult> RegisterAsync(
            RegisterRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<AuthActionResult> ConfirmEmailAsync(
            ConfirmEmailRequestDto request,
            CancellationToken cancellationToken = default);

        Task<AuthActionResult> ResendConfirmationEmailAsync(
            ResendConfirmationEmailRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<AuthActionResult> RequestPasswordResetAsync(
            ForgotPasswordRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<AuthActionResult> ResetPasswordAsync(
            ResetPasswordRequestDto request,
            CancellationToken cancellationToken = default);
    }

    public sealed record RegisterAccountResult(
        int UserId,
        string UserName,
        string FirstName,
        string LastName,
        string Email,
        string MaskedEmail,
        string Message);

    public sealed record AuthActionResult(
        string Code,
        string Message,
        int StatusCode = StatusCodes.Status200OK,
        string? Field = null,
        bool EmailDelivered = false);
}
