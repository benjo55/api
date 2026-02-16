using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using api.Interfaces;
using api.Data;
using api.Dtos.Brand;
using api.Mappers;
using Microsoft.AspNetCore.Mvc;
using api.Helpers;

namespace api.Controllers
{
    [Route("api/brands")]
    [ApiController]
    public class BrandController : ControllerBase
    {
        private readonly IBrandRepository _brandRepository;

        public BrandController(IBrandRepository brandRepository)
        {
            _brandRepository = brandRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var brands = await _brandRepository.GetAllAsync(query);
            var brandDto = brands.Items.Select(s => s.ToBrandDto());
            return Ok(brands);
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {

            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null) return NotFound();
            return Ok(brand.ToBrandDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromRoute] int personId, CreateBrandRequestDto brandDto)
        {
            var brandModel = brandDto.ToBrandFromCreateDto();
            await _brandRepository.CreateAsync(brandModel);
            return CreatedAtAction(nameof(GetById), new { id = brandModel.Id }, brandModel);
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateBrandRequestDto updateBrandDto)
        {
            var brandModel = await _brandRepository.UpdateAsync(id, updateBrandDto);
            if (brandModel == null) return NotFound("Marque non trouvée");
            return Ok(brandModel.ToBrandDto());
        }
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var brandModel = await _brandRepository.DeleteAsync(id);
            if (brandModel == null) return NotFound("La marque n'existe pas");
            return Ok(brandModel);
        }

        [HttpPatch("{id:int}/locked")]
        public async Task<IActionResult> PatchLocked(int id, [FromBody] bool locked)
        {
            var updatedBrand = await _brandRepository.PatchLockedAsync(id, locked);
            if (updatedBrand == null)
                return NotFound();

            return Ok(updatedBrand);
        }
    }
}