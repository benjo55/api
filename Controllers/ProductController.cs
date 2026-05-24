using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Interfaces;
using api.Dtos.Product;
using Microsoft.AspNetCore.Mvc;
using api.Helpers;
using api.Mappers;

namespace api.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepository _productRepository;

        public ProductController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var products = await _productRepository.GetAllAsync(query);
            return Ok(products);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductRequestDto productDto)
        {
            var productModel = productDto.ToProductFromCreateDto();
            await _productRepository.CreateAsync(productModel);
            return CreatedAtAction(nameof(GetById), new { Id = productModel.Id }, productModel);

        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateProductRequestDto productDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var updatedProduct = await _productRepository.UpdateAsync(id, productDto);
            if (updatedProduct == null) return NotFound();
            return Ok(updatedProduct);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var deletedProduct = await _productRepository.DeleteAsync(id);
            if (deletedProduct == null) return NotFound();
            return Ok(deletedProduct);
        }

        [HttpGet("{id:int}/contracts/count")]
        public async Task<IActionResult> GetContractCountByProductId([FromRoute] int id)
        {
            var contractCount = await _productRepository.CountContractsByProductIdAsync(id);
            return Ok(new { ProductId = id, ContractCount = contractCount });
        }

        [HttpPatch("{id:int}/locked")]
        public async Task<IActionResult> PatchLocked(int id, [FromBody] bool locked)
        {
            var updatedProduct = await _productRepository.PatchLockedAsync(id, locked);
            if (updatedProduct == null)
                return NotFound();

            return Ok(updatedProduct);
        }

        [HttpGet("{id:int}/tax")]
        public async Task<IActionResult> GetTaxView([FromRoute] int id, [FromQuery] DateTime? asOfDate = null)
        {
            var taxView = await _productRepository.GetTaxViewByProductIdAsync(id, asOfDate);
            if (taxView == null) return NotFound();
            return Ok(taxView);
        }

        [HttpGet("{id:int}/features")]
        public async Task<IActionResult> GetFeatures([FromRoute] int id, [FromQuery] DateTime? asOfDate = null)
        {
            var features = await _productRepository.GetFeaturesByProductIdAsync(id, asOfDate);
            return Ok(features);
        }

        [HttpPost("{id:int}/features")]
        public async Task<IActionResult> AddFeature([FromRoute] int id, [FromBody] CreateProductFeatureDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _productRepository.AddFeatureAsync(id, dto);
            if (created == null) return NotFound();
            return Ok(created);
        }

        [HttpPut("{id:int}/features/{featureId:int}")]
        public async Task<IActionResult> UpdateFeature([FromRoute] int id, [FromRoute] int featureId, [FromBody] UpdateProductFeatureDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var updated = await _productRepository.UpdateFeatureAsync(id, featureId, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpGet("{id:int}/tax-overrides")]
        public async Task<IActionResult> GetTaxOverrides([FromRoute] int id, [FromQuery] DateTime? asOfDate = null)
        {
            var overrides = await _productRepository.GetTaxOverridesByProductIdAsync(id, asOfDate);
            return Ok(overrides);
        }

        [HttpPost("{id:int}/tax-overrides")]
        public async Task<IActionResult> AddTaxOverride([FromRoute] int id, [FromBody] CreateProductTaxOverrideDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _productRepository.AddTaxOverrideAsync(id, dto);
            if (created == null) return NotFound();
            return Ok(created);
        }

        [HttpPut("{id:int}/tax-overrides/{taxOverrideId:int}")]
        public async Task<IActionResult> UpdateTaxOverride([FromRoute] int id, [FromRoute] int taxOverrideId, [FromBody] UpdateProductTaxOverrideDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var updated = await _productRepository.UpdateTaxOverrideAsync(id, taxOverrideId, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpGet("types")]
        public async Task<IActionResult> GetProductTypes()
        {
            var productTypes = await _productRepository.GetProductTypesAsync();
            return Ok(productTypes);
        }
    }
}
