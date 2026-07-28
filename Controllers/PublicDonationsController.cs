using api.Dtos.PublicDonations;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/public/donations")]
    [AllowAnonymous]
    public sealed class PublicDonationsController : ControllerBase
    {
        private readonly IPublicDonationService _service;

        public PublicDonationsController(IPublicDonationService service)
        {
            _service = service;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] PublicDonationCheckoutRequest request, CancellationToken cancellationToken)
        {
            var response = await _service.InitializeCheckoutAsync(request, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{publicId}/status")]
        public async Task<IActionResult> GetStatus([FromRoute] string publicId, CancellationToken cancellationToken)
        {
            var status = await _service.GetPublicStatusAsync(publicId, cancellationToken);
            return status is null ? NotFound() : Ok(status);
        }

        [HttpPost("{publicId}/receipt-token")]
        public async Task<IActionResult> CreateReceiptToken([FromRoute] string publicId, CancellationToken cancellationToken)
        {
            var token = await _service.CreateReceiptTokenAsync(publicId, cancellationToken);
            return token is null ? NotFound() : Ok(token);
        }

        [HttpGet("{publicId}/receipt")]
        public async Task<IActionResult> DownloadReceipt([FromRoute] string publicId, [FromQuery] string token, CancellationToken cancellationToken)
        {
            var file = await _service.DownloadReceiptAsync(publicId, token, cancellationToken);
            if (file is null)
            {
                return Unauthorized();
            }

            return File(file.Value.Content, "application/pdf", file.Value.FileName);
        }
    }
}
