using System.Security.Claims;
using api.Dtos.Me;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/me")]
    [Authorize]
    public sealed class MeController : ControllerBase
    {
        private readonly IMeProfileService _profileService;

        public MeController(IMeProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Utilisateur non authentifié." });
            }

            return Ok(await _profileService.GetDashboardAsync(userId.Value, cancellationToken));
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Utilisateur non authentifié." });
            }

            return Ok(await _profileService.GetProfileAsync(userId.Value, cancellationToken));
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] SaveMeProfileDto dto, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Utilisateur non authentifié." });
            }

            return Ok(await _profileService.UpdateProfileAsync(userId.Value, dto, cancellationToken));
        }

        [HttpGet("donation-organizations")]
        public async Task<IActionResult> GetDonationOrganizations(CancellationToken cancellationToken)
        {
            return Ok(await _profileService.GetDonationOrganizationsAsync(cancellationToken));
        }

        private int? CurrentUserId()
        {
            var rawUserId = User.FindFirst("userId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(rawUserId, out var userId) ? userId : null;
        }

    }
}
