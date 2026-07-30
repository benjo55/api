using api.Dtos.Advance;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/advances")]
    [ApiController]
    [Authorize]
    public class AdvanceController : ControllerBase
    {
        private readonly IAdvanceRepository _advanceRepository;
        private readonly ICurrentUserAccessService _access;

        public AdvanceController(
            IAdvanceRepository advanceRepository,
            ICurrentUserAccessService access)
        {
            _advanceRepository = advanceRepository;
            _access = access;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var scope = await _access.GetScopeAsync();
            if (scope.IsBackOffice)
                return Ok(await _advanceRepository.GetAllAsync());

            if (!scope.LinkedPersonId.HasValue)
                return Ok(Array.Empty<AdvanceDto>());

            var advances = await _advanceRepository.GetAllAsync();
            var visibleAdvances = new List<AdvanceDto>();

            foreach (var advance in advances)
            {
                if (await _access.CanReadContractAsync(advance.ContractId))
                    visibleAdvances.Add(advance);
            }

            return Ok(visibleAdvances);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var advance = await _advanceRepository.GetByIdAsync(id);
            if (advance == null)
                return NotFound();

            if (!await _access.CanReadContractAsync(advance.ContractId))
                return NotFound();

            return Ok(advance);
        }

        [HttpGet("contract/{contractId:int}")]
        public async Task<IActionResult> GetByContract([FromRoute] int contractId)
        {
            if (!await _access.CanReadContractAsync(contractId))
                return NotFound();

            return Ok(await _advanceRepository.GetByContractIdAsync(contractId));
        }

        [HttpGet("contract/{contractId:int}/eligibility")]
        public async Task<IActionResult> GetEligibility([FromRoute] int contractId)
        {
            if (!await _access.CanReadContractAsync(contractId))
                return NotFound();

            var eligibility = await _advanceRepository.GetEligibilityAsync(contractId);
            return eligibility == null ? NotFound() : Ok(eligibility);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAdvanceRequestDto dto)
        {
            if (!(await _access.GetScopeAsync()).IsBackOffice)
                return Forbid();

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
            if (!(await _access.GetScopeAsync()).IsBackOffice)
                return Forbid();

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
