
using api.Data;
using api.Dtos.Person;
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
    [ApiController]
    [Route("api/[controller]")]
    public abstract class ControllerBaseGeneric<TEntity, TCreateDto, TUpdateDto> : ControllerBase where TEntity : class
    {
        private readonly IRepository<TEntity> _repository;

        protected ControllerBaseGeneric(IRepository<TEntity> repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query) => Ok(await _repository.GetAllAsync(query));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? NotFound() : Ok(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TCreateDto createDto)
        {
            var entity = MapToEntity(createDto);
            var createdEntity = await _repository.CreateAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = createdEntity }, createdEntity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] TUpdateDto updateDto)
        {
            if (updateDto == null)
            {
                return BadRequest("Update data cannot be null");
            }
            var updatedEntity = await _repository.UpdateAsync(id, updateDto);
            return updatedEntity == null ? NotFound() : Ok(updatedEntity);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deletedEntity = await _repository.DeleteAsync(id);
            return deletedEntity == null ? NotFound() : NoContent();
        }

        protected abstract TEntity MapToEntity(TCreateDto createDto);
    }
}

