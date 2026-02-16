using api.Data;
using api.Dtos.BeneficiaryClause;
using api.Interfaces;
using api.Models;
using api.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using api.Helpers;

namespace api.Controllers
{
    [Route("api/beneficiaryClauses")]
    [ApiController]
    public class BeneficiaryClauseController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IBeneficiaryClauseRepository _beneficiaryClauseRepository;
        private readonly IBeneficiaryClausePersonRepository _beneficiaryClausePersonRepository;
        private readonly EntityHistoryService _entityHistoryService;
        private readonly AutoMapper.IMapper _mapper;

        public BeneficiaryClauseController(
            ApplicationDBContext context,
            IBeneficiaryClauseRepository beneficiaryClauseRepository,
            IBeneficiaryClausePersonRepository beneficiaryClausePersonRepository,
            EntityHistoryService entityHistoryService,
            AutoMapper.IMapper mapper
        )
        {
            _context = context;
            _beneficiaryClauseRepository = beneficiaryClauseRepository;
            _beneficiaryClausePersonRepository = beneficiaryClausePersonRepository;
            _entityHistoryService = entityHistoryService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            var clauses = await _beneficiaryClauseRepository.GetAllAsync(query);
            return Ok(clauses);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var clause = await _beneficiaryClauseRepository.GetByIdAsync(id);
            if (clause == null) return NotFound();
            return Ok(clause.ToBeneficiaryClauseDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBeneficiaryClauseRequestDto clauseDto)
        {
            var existingClause = await _context.BeneficiaryClauses
                .FirstOrDefaultAsync(c => c.ContractId == clauseDto.ContractId);

            if (existingClause != null)
                return BadRequest("Ce contrat possède déjà une clause bénéficiaire.");

            var clauseModel = clauseDto.ToBeneficiaryClauseFromCreateDto();
            var createdClause = await _beneficiaryClauseRepository.CreateAsync(clauseModel);

            if (clauseDto.Beneficiaries != null && clauseDto.Beneficiaries.Any())
            {
                foreach (var beneficiary in clauseDto.Beneficiaries)
                {
                    var beneficiaryClausePerson = new BeneficiaryClausePerson
                    {
                        ClauseId = createdClause.Id,
                        PersonId = beneficiary.PersonId,
                        RelationWithClause = beneficiary.RelationWithClause,
                        Percentage = beneficiary.Percentage,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };
                    await _beneficiaryClausePersonRepository.AssignBeneficiaryAsync(beneficiaryClausePerson);
                }
            }

            return CreatedAtAction(nameof(GetById), new { Id = createdClause.Id }, createdClause);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateBeneficiaryClauseRequestDto updateDto)
        {
            // 1. Charger la clause existante avec ses relations
            var clause = await _context.BeneficiaryClauses
                .Include(c => c.Beneficiaries)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clause == null)
                return NotFound();

            // 2. Historisation si nécessaire
            var original = new BeneficiaryClause();
            _mapper.Map(clause, original); // Clone original pour suivi

            // 3. Appliquer la mise à jour des champs simples
            updateDto.ToBeneficiaryClauseFromUpdateDto(clause);

            // 4. Supprimer les anciens bénéficiaires
            _context.BeneficiaryClausePersons.RemoveRange(clause.Beneficiaries);
            await _context.SaveChangesAsync(); // important avant réinsertion

            // 5. Réinsérer les nouveaux bénéficiaires
            foreach (var b in updateDto.Beneficiaries)
            {
                var item = new BeneficiaryClausePerson
                {
                    ClauseId = id,
                    PersonId = b.PersonId,
                    RelationWithClause = b.RelationWithClause?.Trim() ?? string.Empty,
                    Percentage = b.Percentage,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };
                await _beneficiaryClausePersonRepository.AssignBeneficiaryAsync(item);
            }

            // 6. Save et historiser
            await _context.SaveChangesAsync();
            await _entityHistoryService.TrackChangesAsync(original, clause, "Admin");

            // 7. ✅ Recharger avec relations pour DTO enrichi
            var updatedClauseWithRelations = await _context.BeneficiaryClauses
                .Include(c => c.Beneficiaries)
                    .ThenInclude(b => b.Person)
                .Include(c => c.Contract)
                .FirstOrDefaultAsync(c => c.Id == clause.Id);

            return Ok(updatedClauseWithRelations?.ToBeneficiaryClauseDto());
        }

        [HttpPost("{id:int}/lock")]
        public async Task<IActionResult> LockClause(int id)
        {
            var clause = await _beneficiaryClauseRepository.GetByIdAsync(id);
            if (clause == null) return NotFound();
            if (clause.Locked) return BadRequest("Déjà verrouillée.");

            var original = new BeneficiaryClause();
            _mapper.Map(clause, original);

            clause.Locked = true;
            clause.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Historisation diff/événement
            await _entityHistoryService.TrackChangesAsync(original, clause, "Admin");
            return Ok();
        }

        [HttpPost("{id:int}/unlock")]
        public async Task<IActionResult> UnlockClause(int id)
        {
            var clause = await _beneficiaryClauseRepository.GetByIdAsync(id);
            if (clause == null) return NotFound();
            if (!clause.Locked) return BadRequest("Déjà déverrouillée.");

            var original = new BeneficiaryClause();
            _mapper.Map(clause, original);

            clause.Locked = false;
            clause.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _entityHistoryService.TrackChangesAsync(original, clause, "Admin");
            return Ok();
        }

        [HttpPatch("{id:int}/locked")]
        public async Task<IActionResult> PatchLocked(int id, [FromBody] bool locked)
        {
            var updatedClause = await _beneficiaryClauseRepository.PatchLockedAsync(id, locked);

            if (updatedClause == null)
                return NotFound();

            return Ok(updatedClause);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var clause = await _beneficiaryClauseRepository.DeleteAsync(id);
            if (clause == null)
                return NotFound();

            return NoContent();
        }

    }
}
