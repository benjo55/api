using api.Dtos.TaxReceipts;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/beneficiary-organizations")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public sealed class BeneficiaryOrganizationsController : ControllerBase
    {
        private readonly IBeneficiaryOrganizationService _service;

        public BeneficiaryOrganizationsController(IBeneficiaryOrganizationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await _service.GetAllAsync(cancellationToken));
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var active = await _service.GetActiveAsync(cancellationToken);
            return active is null ? NotFound() : Ok(active);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
        {
            var organization = await _service.GetByIdAsync(id, cancellationToken);
            return organization is null ? NotFound() : Ok(organization);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveBeneficiaryOrganizationDto dto, CancellationToken cancellationToken)
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SaveBeneficiaryOrganizationDto dto, CancellationToken cancellationToken)
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
    }
}
