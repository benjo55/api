using api.Dtos.Contract;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContractOptionTypesController : ControllerBase
    {
        private readonly IContractOptionTypeRepository _repository;

        public ContractOptionTypesController(IContractOptionTypeRepository repository)
        {
            _repository = repository;
        }

        // GET: api/contractoptiontypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContractOptionTypeDto>>> GetAll()
        {
            var items = await _repository.GetAllAsync();
            return Ok(items.Select(x => new ContractOptionTypeDto
            {
                Id = x.Id,
                Code = x.Code,
                Category = x.Category,
                Label = x.Label,
                Objective = x.Objective,
                Mechanism = x.Mechanism,
                DefaultCost = x.DefaultCost
            }));
        }

        // GET: api/contractoptiontypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ContractOptionTypeDto>> GetById(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();

            return new ContractOptionTypeDto
            {
                Id = item.Id,
                Code = item.Code,
                Category = item.Category,
                Label = item.Label,
                Objective = item.Objective,
                Mechanism = item.Mechanism,
                DefaultCost = item.DefaultCost
            };
        }

        // POST: api/contractoptiontypes
        [HttpPost]
        public async Task<ActionResult<ContractOptionTypeDto>> Create([FromBody] ContractOptionTypeDto dto)
        {
            var entity = new ContractOptionType
            {
                Code = dto.Code,
                Category = dto.Category,
                Label = dto.Label,
                Objective = dto.Objective,
                Mechanism = dto.Mechanism,
                DefaultCost = dto.DefaultCost
            };

            var created = await _repository.CreateAsync(entity);

            dto.Id = created.Id;
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, dto);
        }

        // PUT: api/contractoptiontypes/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ContractOptionTypeDto>> Update(int id, [FromBody] ContractOptionTypeDto dto)
        {
            var entity = new ContractOptionType
            {
                Id = id,
                Code = dto.Code,
                Category = dto.Category,
                Label = dto.Label,
                Objective = dto.Objective,
                Mechanism = dto.Mechanism,
                DefaultCost = dto.DefaultCost
            };

            var updated = await _repository.UpdateAsync(id, entity);
            if (updated == null) return NotFound();

            return dto;
        }

        // DELETE: api/contractoptiontypes/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _repository.DeleteAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }
    }
}
