using api.Dtos.TaxReceipts;
using api.Helpers;
using api.Interfaces;
using api.Exceptions;
using api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace api.Controllers
{
    [Route("api/tax-receipts")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.CerfaAccess)]
    public sealed class TaxReceiptsController : ControllerBase
    {
        private readonly ITaxReceiptService _service;
        private readonly ITaxReceiptEmailService _emailService;

        public TaxReceiptsController(ITaxReceiptService service, ITaxReceiptEmailService emailService)
        {
            _service = service;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetAllAsync(query, cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
        {
            var receipt = await _service.GetByIdAsync(id, cancellationToken);
            return receipt is null ? NotFound() : Ok(receipt);
        }

        [HttpPost("{id:int}/validate")]
        public async Task<IActionResult> Validate([FromRoute] int id, CancellationToken cancellationToken)
        {
            var receipt = await _service.ValidateAsync(id, cancellationToken);
            return receipt is null ? NotFound() : Ok(receipt);
        }

        [HttpPost("{id:int}/generate")]
        public async Task<IActionResult> Generate([FromRoute] int id, CancellationToken cancellationToken)
        {
            return Ok(await _service.GenerateAsync(id, User.Identity?.Name, cancellationToken));
        }

        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> DownloadPdf([FromRoute] int id, CancellationToken cancellationToken)
        {
            var pdf = await _service.GetPdfAsync(id, cancellationToken);
            return File(pdf.Content, "application/pdf", pdf.FileName);
        }

        [HttpPost("{id:int}/send-email")]
        public async Task<IActionResult> SendEmail([FromRoute] int id, [FromBody] SendTaxReceiptEmailDto dto, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _emailService.SendAsync(
                    id,
                    dto,
                    CurrentUsername(),
                    cancellationToken,
                    CurrentUserId(),
                    CanAccessAllReceipts()));
            }
            catch (BusinessException ex) when (ex.Message == "TaxReceiptForbidden")
            {
                return Forbid();
            }
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel([FromRoute] int id, [FromBody] string? reason, CancellationToken cancellationToken)
        {
            var receipt = await _service.CancelAsync(id, reason, cancellationToken);
            return receipt is null ? NotFound() : Ok(receipt);
        }

        [HttpPost("{id:int}/replace")]
        public async Task<IActionResult> Replace([FromRoute] int id, CancellationToken cancellationToken)
        {
            return Ok(await _service.ReplaceAsync(id, User.Identity?.Name, cancellationToken));
        }

        [HttpGet("{id:int}/email-history")]
        public async Task<IActionResult> GetEmailHistory([FromRoute] int id, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetEmailHistoryAsync(id, cancellationToken));
        }

        private int? CurrentUserId() =>
            int.TryParse(User.FindFirst("userId")?.Value, out var userId) ? userId : null;

        private string? CurrentUsername() =>
            User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("username")?.Value;

        private bool CanAccessAllReceipts() =>
            User.IsInRole("Admin")
            || User.IsInRole("Administrator")
            || User.IsInRole("SuperAdministrator");
    }
}
