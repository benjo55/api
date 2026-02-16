using api.Data;
using api.Dtos.Person;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using api.Rules;

namespace api.Controllers
{
    [Route("api/persons")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IPersonRepository _personRepository;
        private readonly IValidationService<Person> _validationService;

        public PersonController(
            ApplicationDBContext context,
            IPersonRepository personRepository,
            IValidationService<Person> validationService)
        {
            _personRepository = personRepository;
            _context = context;
            _validationService = validationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var persons = await _personRepository.GetAllAsync(query);
            return Ok(persons);
        }

        [HttpGet("light")]
        public async Task<IActionResult> GetLight([FromQuery] QueryObject query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var persons = await _personRepository.GetListLightAsync(query);
            return Ok(persons);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var person = await _personRepository.GetByIdAsync(id);
            if (person == null) return NotFound();
            return Ok(person.ToPersonDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePersonRequestDto personDto)
        {
            var personModel = personDto.ToPersonFromCreateDto();

            var errors = _validationService.Validate(personModel);
            if (errors.Any())
                return BadRequest(new { errors });

            await _personRepository.CreateAsync(personModel);
            return CreatedAtAction(nameof(GetById), new { Id = personModel.Id }, personModel);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdatePersonRequestDto updatePersonDto)
        {
            var updatedPerson = updatePersonDto.ToPersonFromUpdateDto();

            var errors = _validationService.Validate(updatedPerson);
            if (errors.Any())
                return BadRequest(new { errors });

            var personModel = await _personRepository.UpdateAsync(id, updatePersonDto);
            if (personModel == null) return NotFound();
            return Ok(personModel.ToPersonDto());
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                var personModel = await _personRepository.GetByIdAsync(id);
                if (personModel == null)
                    return NotFound();

                var isBeneficiary = await _personRepository.IsPersonBeneficiary(id);

                if (isBeneficiary)
                {
                    return BadRequest(new
                    {
                        message = "Impossible de supprimer cette personne car elle est bénéficiaire nominative sur au moins un contrat."
                    });
                }

                var hasContracts = await _personRepository.HasContracts(id);
                if (hasContracts)
                {
                    return BadRequest(new
                    {
                        message = "Impossible de supprimer cette personne car elle est titulaire d’au moins un contrat."
                    });
                }

                var deletedPerson = await _personRepository.DeleteAsync(id);
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
                return StatusCode(500, new
                {
                    message = "Une erreur interne est survenue.",
                    details = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpGet("typeahead")]
        public async Task<IActionResult> GetTypeahead([FromQuery] string search)
        {
            var results = await _personRepository.GetTypeaheadAsync(search);
            return Ok(new { items = results });
        }

        [HttpPatch("{id:int}/locked")]
        public async Task<IActionResult> PatchLocked(int id, [FromBody] bool locked)
        {
            var updatedPerson = await _personRepository.PatchLockedAsync(id, locked);
            if (updatedPerson == null)
                return NotFound();

            return Ok(updatedPerson);
        }
    }
}
