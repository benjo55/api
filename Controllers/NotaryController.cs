using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Notary;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [Route("api/notaries")]
    [ApiController]
    public class NotaryController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly INotaryRepository _notaryRepository;
        public NotaryController(ApplicationDBContext context, INotaryRepository notaryRepository)
        {
            _notaryRepository = notaryRepository;
            _context = context;
        }

        [HttpGet]

        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var notaries = await _notaryRepository.GetAllAsync(query);
            var notaryDto = notaries.Select(s => s.ToNotaryDto());
            return Ok(notaries);
        }

        [HttpGet("{id:int}")]

        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var notary = await _notaryRepository.GetByIdAsync(id);
            if (notary == null) return NotFound();
            return Ok(notary.ToNotaryDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotaryRequestDto notaryDto)
        {
            var notaryModel = notaryDto.ToNotaryFromCreateDto();
            await _notaryRepository.CreateAsync(notaryModel);
            return CreatedAtAction(nameof(GetById), new { Id = notaryModel.Id }, notaryModel);
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateNotaryRequestDto updatenotaryDto)

        {
            var notaryModel = await _notaryRepository.UpdateAsync(id, updatenotaryDto);
            if (notaryModel == null) return NotFound();
            return Ok(notaryModel.ToNotaryDto());
        }

        [HttpDelete]
        [Route("{id:int}")]

        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var notaryModel = await _notaryRepository.DeleteAsync(id);
            if (notaryModel == null) return NotFound();
            return NoContent();
        }
    }
}