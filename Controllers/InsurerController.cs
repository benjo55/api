
using System.Data.SqlClient;
using api.Data;
using api.Dtos.Insurer;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace api.Controllers
{
    [Route("api/insurers")]
    [ApiController]
    // [Microsoft.AspNetCore.Cors.DisableCors]
    // [Microsoft.AspNetCore.Cors.EnableCors("AllowAllHeaders")]

    public class InsurerController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IInsurerRepository _insurerRepository;
        public InsurerController(ApplicationDBContext context, IInsurerRepository InsurerRepository)
        {
            _insurerRepository = InsurerRepository;
            _context = context;
        }

        [HttpGet]

        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var Insurers = await _insurerRepository.GetAllAsync(query);
            var InsurerDto = Insurers.Items.Select(p => p.ToInsurerDto());
            return Ok(Insurers);
        }

        [HttpGet("{id:int}")]

        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var Insurer = await _insurerRepository.GetByIdAsync(id);
            if (Insurer == null) return NotFound();
            return Ok(Insurer.ToInsurerDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInsurerRequestDto InsurerDto)
        {
            var InsurerModel = InsurerDto.ToInsurerFromCreateDto();
            await _insurerRepository.CreateAsync(InsurerModel);
            return CreatedAtAction(nameof(GetById), new { Id = InsurerModel.Id }, InsurerModel);
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateInsurerRequestDto updateInsurerDto)

        {
            var InsurerModel = await _insurerRepository.UpdateAsync(id, updateInsurerDto);
            if (InsurerModel == null) return NotFound();
            return Ok(InsurerModel.ToInsurerDto());
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                var InsurerModel = await _insurerRepository.DeleteAsync(id);
                if (InsurerModel == null) return NotFound("Assureur non trouvé.");
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("⚠️ Erreur de suppression (FK) : " + ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Erreur Serveur : " + ex.Message);
                return StatusCode(500, new { message = "Une erreur interne est survenue.", details = ex.InnerException?.Message ?? ex.Message });
            }
        }
        [HttpPatch("{id:int}/locked")]
        public async Task<IActionResult> PatchLocked(int id, [FromBody] bool locked)
        {
            var insurer = await _insurerRepository.PatchLockedAsync(id, locked);
            if (insurer == null) return NotFound();
            return Ok(insurer.ToInsurerDto());
        }

    }
}