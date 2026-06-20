using api.Dtos.Advance;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/advances")]
    [ApiController]
    public class AdvanceController : ControllerBase
    {
        private readonly IAdvanceRepository _advanceRepository;

        public AdvanceController(IAdvanceRepository advanceRepository)
        {
            _advanceRepository = advanceRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _advanceRepository.GetAllAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var advance = await _advanceRepository.GetByIdAsync(id);
            return advance == null ? NotFound() : Ok(advance);
        }

        [HttpGet("contract/{contractId:int}")]
        public async Task<IActionResult> GetByContract([FromRoute] int contractId)
        {
            return Ok(await _advanceRepository.GetByContractIdAsync(contractId));
        }

        [HttpGet("contract/{contractId:int}/eligibility")]
        public async Task<IActionResult> GetEligibility([FromRoute] int contractId)
        {
            var eligibility = await _advanceRepository.GetEligibilityAsync(contractId);
            return eligibility == null ? NotFound() : Ok(eligibility);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAdvanceRequestDto dto)
        {
            try
            {
                var created = await _advanceRepository.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateAdvanceRequestDto dto)
        {
            try
            {
                var updated = await _advanceRepository.UpdateAsync(id, dto);
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
