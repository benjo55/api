using api.Dtos.TaxReceipts;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/donations")]
    [ApiController]
    public sealed class DonationsController : ControllerBase
    {
        private readonly IDonationService _service;
        private readonly ITaxReceiptService _taxReceiptService;

        public DonationsController(IDonationService service, ITaxReceiptService taxReceiptService)
        {
            _service = service;
            _taxReceiptService = taxReceiptService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetAllAsync(query, cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
        {
            var donation = await _service.GetByIdAsync(id, cancellationToken);
            return donation is null ? NotFound() : Ok(donation);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveDonationDto dto, CancellationToken cancellationToken)
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SaveDonationDto dto, CancellationToken cancellationToken)
        {
            var updated = await _service.UpdateAsync(id, dto, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/validate")]
        public async Task<IActionResult> Validate([FromRoute] int id, CancellationToken cancellationToken)
        {
            var updated = await _service.ValidateAsync(id, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel([FromRoute] int id, CancellationToken cancellationToken)
        {
            var updated = await _service.CancelAsync(id, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpGet("{id:int}/receipts")]
        public async Task<IActionResult> GetReceipts([FromRoute] int id, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetReceiptsAsync(id, cancellationToken));
        }

        [HttpPost("{donationId:int}/tax-receipts")]
        public async Task<IActionResult> CreateTaxReceipt([FromRoute] int donationId, [FromBody] CreateTaxReceiptDto dto, CancellationToken cancellationToken)
        {
            var created = await _taxReceiptService.CreateForDonationAsync(donationId, dto, User.Identity?.Name, cancellationToken);
            return Ok(created);
        }
    }
}
