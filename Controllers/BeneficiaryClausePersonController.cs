using api.Data;
using api.Dtos.BeneficiaryClause;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace api.Controllers
{
    [ApiController]
    [Route("api/beneficiaryClausePersons")]
    public class BeneficiaryClausePersonController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IBeneficiaryClausePersonRepository _repository;

        public BeneficiaryClausePersonController(ApplicationDBContext context, IBeneficiaryClausePersonRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {

            Console.WriteLine("🔍 Reçu : PersonId = " + query.PersonId);
            Console.WriteLine("🔍 Reçu : filters = " + string.Join(", ", query.Filters));

            var relations = await _repository.GetAllAsync(query);
            return Ok(relations);
        }

        [HttpGet("raw")]
        public async Task<List<BeneficiaryClausePerson>> GetAllRawAsync(QueryObject query)
        {
            var beneficiaries = _context.BeneficiaryClausePersons
                .Include(bcp => bcp.Person)
                .Include(bcp => bcp.BeneficiaryClause)
                .AsQueryable();

            if (query.PersonId.HasValue)
            {
                beneficiaries = beneficiaries.Where(b => b.PersonId == query.PersonId.Value);
            }

            if (query.Filters.ContainsKey("clauseType") && query.Filters["clauseType"] == "Nominative")
            {
                beneficiaries = beneficiaries.Where(b => b.BeneficiaryClause!.ClauseType == "Nominative");
            }

            return await beneficiaries.ToListAsync();
        }


        [HttpPost("assign")]
        public async Task<IActionResult> AssignBeneficiary([FromBody] BeneficiaryClausePerson entity)
        {
            var success = await _repository.AssignBeneficiaryAsync(entity);
            if (!success) return BadRequest("Échec de l'assignation.");
            return Ok();
        }

        [HttpPost("assignMultiple")]
        public async Task<IActionResult> AssignMultipleBeneficiaries([FromBody] List<BeneficiaryClausePerson> beneficiaries)
        {
            foreach (var beneficiary in beneficiaries)
            {
                await _repository.AssignBeneficiaryAsync(beneficiary);
            }
            return Ok();
        }

        [HttpDelete("remove/{clauseId:int}/{personId:int}")]
        public async Task<IActionResult> RemoveBeneficiary(int clauseId, int personId)
        {
            var success = await _repository.RemoveBeneficiaryAsync(clauseId, personId);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet("byPerson/{personId:int}")]
        public async Task<ActionResult<List<BeneficiaryClausePerson>>> GetByPersonId(int personId)
        {
            var clauses = await _repository.GetByPersonIdAsync(personId);
            return Ok(clauses);
        }
    }
}
