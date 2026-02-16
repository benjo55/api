using System;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Compartment;
using api.Interfaces;
using api.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/compartments")]
    public class CompartmentController : ControllerBase
    {
        private readonly ICompartmentRepository _repo;

        public CompartmentController(ICompartmentRepository repo)
        {
            _repo = repo;
        }

        // ==========================================================
        // 🔹 GET : par contrat
        // ==========================================================
        [HttpGet("byContract/{contractId:int}")]
        public async Task<IActionResult> GetByContract(int contractId)
        {
            var items = (await _repo.GetByContractAsync(contractId))
                .Select(c => c.ToCompartmentDto());
            return Ok(items);
        }

        // ==========================================================
        // 🔹 GET : par id
        // ==========================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _repo.GetByIdAsync(id);
            if (model == null)
                return NotFound(new { message = $"Compartiment {id} introuvable." });

            return Ok(model.ToCompartmentDto());
        }

        // ==========================================================
        // 🔹 POST : création
        // ==========================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompartmentRequestDto dto)
        {
            try
            {
                var model = await _repo.CreateAsync(dto.ToModel(), dto);
                return CreatedAtAction(nameof(GetById), new { id = model.Id }, model.ToCompartmentDto());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Erreur lors de la création : {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne du serveur.", details = ex.Message });
            }
        }

        // ==========================================================
        // 🔹 PUT : mise à jour
        // ==========================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCompartmentRequestDto dto)
        {
            try
            {
                var model = await _repo.UpdateAsync(id, dto);
                if (model == null)
                    return NotFound(new { message = $"Compartiment {id} introuvable." });

                return Ok(model.ToCompartmentDto());
            }
            catch (InvalidOperationException ex)
            {
                // ⚠️ Cas typique : tentative de modification du compartiment global
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Erreur Update Compartment : {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne du serveur.", details = ex.Message });
            }
        }

        // ==========================================================
        // 🔹 DELETE : suppression
        // ==========================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _repo.DeleteAsync(id);
                return ok
                    ? NoContent()
                    : NotFound(new { message = $"Compartiment {id} introuvable ou déjà supprimé." });
            }
            catch (InvalidOperationException ex)
            {
                // ⚠️ Cas typique : suppression du compartiment global interdite
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Erreur suppression compartiment : {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne du serveur.", details = ex.Message });
            }
        }

        // ==========================================================
        // 🔹 PATCH : renommage rapide
        // ==========================================================
        [HttpPatch("{id:int}/label")]
        public async Task<IActionResult> PatchLabel(int id, [FromBody] string newLabel)
        {
            try
            {
                var updated = await _repo.PatchLabelAsync(id, newLabel);
                if (updated == null)
                    return NotFound(new { message = $"Compartiment {id} introuvable." });

                if (updated.IsDefault)
                    return BadRequest(new { message = "Impossible de renommer le compartiment global." });

                return Ok(updated.ToCompartmentDto());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Erreur PatchLabel : {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne du serveur.", details = ex.Message });
            }
        }
    }
}
