
using api.Data;
using api.Dtos.Generic;
using api.Dtos.Insurer;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.Data.SqlClient;
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
        private readonly IInseeSireneService _inseeSireneService;
        public InsurerController(
            ApplicationDBContext context,
            IInsurerRepository InsurerRepository,
            IInseeSireneService inseeSireneService)
        {
            _insurerRepository = InsurerRepository;
            _context = context;
            _inseeSireneService = inseeSireneService;
        }

        [HttpGet]

        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var Insurers = await _insurerRepository.GetAllAsync(query);
            var InsurerDto = Insurers.Items.Select(p => p.ToInsurerDto()).ToList();
            return Ok(new PagedResult<InsurerDto>
            {
                Items = InsurerDto,
                TotalCount = Insurers.TotalCount,
                TotalPages = Insurers.TotalPages,
                HasNextPage = Insurers.HasNextPage,
                CurrentPage = Insurers.CurrentPage
            });
        }

        [HttpGet("{id:int}")]

        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var Insurer = await _insurerRepository.GetByIdAsync(id);
            if (Insurer == null) return NotFound();
            return Ok(Insurer.ToInsurerDto());
        }

        [HttpGet("sirene-search")]
        public async Task<IActionResult> SearchSirene(
            [FromQuery] string search,
            [FromQuery] int limit = 10,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(search) || search.Trim().Length < 3)
            {
                return Ok(new { items = Array.Empty<InsurerSireneSearchDto>() });
            }

            try
            {
                var items = await _inseeSireneService.SearchInsurersAsync(search, limit, cancellationToken);
                return Ok(new { items });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInsurerRequestDto InsurerDto)
        {
            var duplicateResult = await FindDuplicateInsurerAsync(InsurerDto);
            if (duplicateResult != null) return duplicateResult;

            try
            {
                var InsurerModel = InsurerDto.ToInsurerFromCreateDto();
                await _insurerRepository.CreateAsync(InsurerModel);
                return CreatedAtAction(nameof(GetById), new { Id = InsurerModel.Id }, InsurerModel.ToInsurerDto());
            }
            catch (DbUpdateException ex) when (IsUniqueInsurerConstraintViolation(ex))
            {
                return Conflict(new { message = GetUniqueInsurerConstraintMessage(ex) });
            }
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

        private async Task<IActionResult?> FindDuplicateInsurerAsync(CreateInsurerRequestDto insurerDto)
        {
            if (!string.IsNullOrWhiteSpace(insurerDto.Siren))
            {
                var siren = insurerDto.Siren.Trim();
                var existingBySiren = await _context.Insurers
                    .AsNoTracking()
                    .Where(insurer => insurer.Siren == siren)
                    .Select(insurer => new { insurer.Id, insurer.Name })
                    .FirstOrDefaultAsync();

                if (existingBySiren != null)
                {
                    return Conflict(new
                    {
                        message = $"Un assureur existe déjà avec le SIREN {siren} : {existingBySiren.Name}.",
                        field = "siren",
                        existingId = existingBySiren.Id,
                        existingName = existingBySiren.Name
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(insurerDto.Lei))
            {
                var lei = insurerDto.Lei.Trim().ToUpperInvariant();
                var existingByLei = await _context.Insurers
                    .AsNoTracking()
                    .Where(insurer => insurer.Lei == lei)
                    .Select(insurer => new { insurer.Id, insurer.Name })
                    .FirstOrDefaultAsync();

                if (existingByLei != null)
                {
                    return Conflict(new
                    {
                        message = $"Un assureur existe déjà avec le LEI {lei} : {existingByLei.Name}.",
                        field = "lei",
                        existingId = existingByLei.Id,
                        existingName = existingByLei.Name
                    });
                }
            }

            return null;
        }

        private static bool IsUniqueInsurerConstraintViolation(DbUpdateException ex)
        {
            if (ex.InnerException is not SqlException sqlException) return false;
            if (sqlException.Number is not (2601 or 2627)) return false;

            return sqlException.Message.Contains("UX_Insurers_Siren", StringComparison.OrdinalIgnoreCase)
                || sqlException.Message.Contains("UX_Insurers_Lei", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetUniqueInsurerConstraintMessage(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;

            if (message.Contains("UX_Insurers_Siren", StringComparison.OrdinalIgnoreCase))
            {
                return "Un assureur existe déjà avec ce SIREN.";
            }

            if (message.Contains("UX_Insurers_Lei", StringComparison.OrdinalIgnoreCase))
            {
                return "Un assureur existe déjà avec ce LEI.";
            }

            return "Un assureur existe déjà avec cet identifiant unique.";
        }

    }
}
