using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/payments/webhooks/helloasso")]
    [Route("api/webhooks/helloasso")]
    [AllowAnonymous]
    public sealed class HelloAssoWebhookController : ControllerBase
    {
        private readonly IPublicDonationService _service;

        public HelloAssoWebhookController(IPublicDonationService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Receive(CancellationToken cancellationToken)
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync(cancellationToken);
            Request.Body.Position = 0;

            var headers = Request.Headers
                .ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);

            await _service.ProcessWebhookAsync(
                rawBody,
                headers,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return Ok();
        }
    }
}
