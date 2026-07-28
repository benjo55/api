using System.Security.Claims;
using api.Data;
using api.Dtos.Me;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [ApiController]
    [Route("api/my-space/donations")]
    [Authorize]
    public sealed class MeDonationPaymentsController : ControllerBase
    {
        private readonly IMeDonationPaymentService _service;
        private readonly ApplicationDBContext _db;

        public MeDonationPaymentsController(IMeDonationPaymentService service, ApplicationDBContext db)
        {
            _service = service;
            _db = db;
        }

        [HttpGet("{publicId}/payment-options")]
        public async Task<IActionResult> GetPaymentOptions([FromRoute] string publicId, CancellationToken cancellationToken)
        {
            var userId = await GetVerifiedUserIdAsync(cancellationToken);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Votre adresse e-mail doit etre confirmee avant de payer un don." });
            }

            var result = await _service.GetPaymentOptionsAsync(userId.Value, publicId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("{publicId}/payments/helloasso")]
        public async Task<IActionResult> StartHelloAsso([FromRoute] string publicId, CancellationToken cancellationToken)
        {
            var userId = await GetVerifiedUserIdAsync(cancellationToken);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Votre adresse e-mail doit etre confirmee avant de payer un don." });
            }

            var result = await _service.StartHelloAssoPaymentAsync(userId.Value, publicId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("{publicId}/payments/bank-transfer")]
        public async Task<IActionResult> StartBankTransfer([FromRoute] string publicId, CancellationToken cancellationToken)
        {
            var userId = await GetVerifiedUserIdAsync(cancellationToken);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Votre adresse e-mail doit etre confirmee avant de payer un don." });
            }

            var result = await _service.StartBankTransferAsync(userId.Value, publicId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("{publicId}/payments/bank-transfer/declaration")]
        public async Task<IActionResult> DeclareBankTransfer([FromRoute] string publicId, [FromBody] DeclareBankTransferDto dto, CancellationToken cancellationToken)
        {
            var userId = await GetVerifiedUserIdAsync(cancellationToken);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Votre adresse e-mail doit etre confirmee avant de payer un don." });
            }

            var result = await _service.DeclareBankTransferAsync(userId.Value, publicId, dto, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("{publicId}/payment-status")]
        public async Task<IActionResult> GetPaymentStatus([FromRoute] string publicId, CancellationToken cancellationToken)
        {
            var userId = await GetVerifiedUserIdAsync(cancellationToken);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Utilisateur non authentifie." });
            }

            var result = await _service.GetPaymentStatusAsync(userId.Value, publicId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        private async Task<int?> GetVerifiedUserIdAsync(CancellationToken cancellationToken)
        {
            var rawUserId = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(rawUserId, out var userId))
            {
                return null;
            }

            var emailConfirmed = await _db.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => (bool?)x.EmailConfirmed)
                .FirstOrDefaultAsync(cancellationToken);

            return emailConfirmed == true ? userId : null;
        }
    }
}
