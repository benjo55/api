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
    }
}
