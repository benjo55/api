using api.Dtos.TaxReceipts;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/donors")]
    [ApiController]
    public sealed class DonorsController : ControllerBase
    {
        private readonly IDonorService _service;

        public DonorsController(IDonorService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetAllAsync(query, cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
        {
            var donor = await _service.GetByIdAsync(id, cancellationToken);
            return donor is null ? NotFound() : Ok(donor);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveDonorDto dto, CancellationToken cancellationToken)
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SaveDonorDto dto, CancellationToken cancellationToken)
        {
            var updated = await _service.UpdateAsync(id, dto, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Archive([FromRoute] int id, CancellationToken cancellationToken)
        {
            var archived = await _service.ArchiveAsync(id, cancellationToken);
            return archived ? NoContent() : NotFound();
        }

        [HttpGet("{id:int}/donations")]
        public async Task<IActionResult> GetDonations([FromRoute] int id, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetDonationsAsync(id, cancellationToken));
        }

        [HttpPost("duplicates")]
        public async Task<IActionResult> FindDuplicates([FromBody] SaveDonorDto dto, CancellationToken cancellationToken)
        {
            return Ok(await _service.FindDuplicatesAsync(dto, cancellationToken));
        }
    }
}
