using System.Security.Claims;
using api.Data;
using api.Dtos.Me;
using api.Helpers;
using api.Interfaces;
using api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [ApiController]
    [Route("api/me/donations")]
    [Authorize(Policy = AuthorizationPolicies.CerfaAccess)]
    public sealed class MeDonationsController : ControllerBase
    {
        private readonly IMeDonationsService _service;
        private readonly ApplicationDBContext _db;

        public MeDonationsController(IMeDonationsService service, ApplicationDBContext db)
        {
            _service = service;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine([FromQuery] QueryObject query, CancellationToken cancellationToken)
        {
            var userId = await GetVerifiedUserIdAsync(cancellationToken);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Utilisateur non authentifie." });
            }

            return Ok(await _service.GetMyDonationsAsync(userId.Value, query, cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> CreateDonationIntent([FromBody] CreateMeDonationIntentDto dto, CancellationToken cancellationToken)
        {
            var userId = await GetVerifiedUserIdAsync(cancellationToken);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Votre adresse e-mail doit être confirmée avant de pouvoir effectuer un don." });
            }

            return Ok(await _service.CreateDonationIntentAsync(userId.Value, dto, cancellationToken));
        }

        [HttpGet("{publicId}")]
        public async Task<IActionResult> GetMineByPublicId([FromRoute] string publicId, CancellationToken cancellationToken)
        {
            var userId = await GetVerifiedUserIdAsync(cancellationToken);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Utilisateur non authentifie." });
            }

            var donation = await _service.GetMyDonationAsync(userId.Value, publicId, cancellationToken);
            return donation is null ? NotFound() : Ok(donation);
        }

        [HttpGet("{publicId}/receipt")]
        public async Task<IActionResult> DownloadReceipt([FromRoute] string publicId, CancellationToken cancellationToken)
        {
            var userId = await GetVerifiedUserIdAsync(cancellationToken);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Utilisateur non authentifie." });
            }

            var file = await _service.DownloadMyReceiptAsync(userId.Value, publicId, cancellationToken);
            if (file is null)
            {
                return NotFound();
            }

            return File(file.Value.Content, "application/pdf", file.Value.FileName);
        }

        [HttpPost("{publicId}/receipt/resend")]
        public async Task<IActionResult> ResendReceipt([FromRoute] string publicId, CancellationToken cancellationToken)
        {
            var userId = await GetVerifiedUserIdAsync(cancellationToken);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Utilisateur non authentifie." });
            }

            var result = await _service.ResendMyReceiptAsync(
                userId.Value,
                publicId,
                User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("username")?.Value,
                cancellationToken);

            return result is null ? NotFound() : Ok(result);
        }

        private async Task<int?> GetVerifiedUserIdAsync(CancellationToken cancellationToken)
        {
            var rawUserId = User.FindFirst("userId")?.Value;
            if (!int.TryParse(rawUserId, out var userId))
            {
                return null;
            }

            var emailConfirmed = await _db.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => (bool?)x.EmailConfirmed)
                .FirstOrDefaultAsync(cancellationToken);

            if (emailConfirmed != true)
            {
                return null;
            }

            return userId;
        }
    }
}
