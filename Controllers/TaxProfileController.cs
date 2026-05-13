using api.Dtos.TaxProfile;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/taxProfiles")]
    public class TaxProfileController : ControllerBase
    {
        private readonly ITaxProfileRepository _repo;
        private readonly ITaxEngineService _engine;

        public TaxProfileController(ITaxProfileRepository repo, ITaxEngineService engine)
        {
            _repo = repo;
            _engine = engine;
        }

        /// <summary>Liste tous les profils fiscaux</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var profiles = await _repo.GetAllAsync();
            return Ok(profiles);
        }

        /// <summary>Récupère un profil par id</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var profile = await _repo.GetByIdAsync(id);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        /// <summary>Récupère le profil d'une famille de contrat</summary>
        [HttpGet("family/{family}")]
        public async Task<IActionResult> GetByFamily([FromRoute] ContractFamily family)
        {
            var profile = await _repo.GetByFamilyAsync(family);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        /// <summary>Crée un nouveau profil fiscal</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaxProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var model = dto.Adapt<TaxProfile>();
            var created = await _repo.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Met à jour un profil fiscal</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTaxProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updated = await _repo.UpdateAsync(id, dto);
                if (updated == null) return NotFound();
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        /// <summary>Supprime un profil fiscal</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                var deleted = await _repo.DeleteAsync(id);
                if (!deleted) return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lance une simulation fiscale complète pour un profil donné.
        /// Retourne la décomposition des taxes applicables (rachat, rente, décès).
        /// </summary>
        [HttpPost("simulate")]
        public async Task<IActionResult> Simulate([FromBody] TaxSimulationRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _engine.SimulateAsync(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Retourne les dernières simulations fiscales historisées.
        /// Permet audit, traçabilité et rejouabilité métier.
        /// </summary>
        [HttpGet("simulations")]
        public async Task<IActionResult> GetRecentSimulations([FromQuery] int take = 50)
        {
            var items = await _engine.GetRecentComputationsAsync(take);
            return Ok(items);
        }
    }
}
