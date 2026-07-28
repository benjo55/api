using api.Dtos.Product;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    public class ProductCatalogController : ControllerBase
    {
        private readonly IProductCatalogRepository _repository;

        public ProductCatalogController(IProductCatalogRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("api/product-categories")]
        public async Task<IActionResult> GetCategories() => Ok(await _repository.GetCategoriesAsync());

        [HttpGet("api/product-categories/{id:int}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var item = await _repository.GetCategoryAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("api/product-categories")]
        public async Task<IActionResult> CreateCategory([FromBody] UpsertProductCategoryDto dto)
        {
            var item = await _repository.CreateCategoryAsync(dto);
            return CreatedAtAction(nameof(GetCategory), new { id = item.Id }, item);
        }

        [HttpPut("api/product-categories/{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpsertProductCategoryDto dto)
        {
            var item = await _repository.UpdateCategoryAsync(id, dto);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpGet("api/legal-natures")]
        public async Task<IActionResult> GetLegalNatures() => Ok(await _repository.GetLegalNaturesAsync());

        [HttpGet("api/legal-natures/{id:int}")]
        public async Task<IActionResult> GetLegalNature(int id)
        {
            var item = await _repository.GetLegalNatureAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("api/legal-natures")]
        public async Task<IActionResult> CreateLegalNature([FromBody] UpsertLegalNatureDto dto)
        {
            var item = await _repository.CreateLegalNatureAsync(dto);
            return CreatedAtAction(nameof(GetLegalNature), new { id = item.Id }, item);
        }

        [HttpPut("api/legal-natures/{id:int}")]
        public async Task<IActionResult> UpdateLegalNature(int id, [FromBody] UpsertLegalNatureDto dto)
        {
            var item = await _repository.UpdateLegalNatureAsync(id, dto);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpGet("api/product-envelopes")]
        public async Task<IActionResult> GetEnvelopes() => Ok(await _repository.GetEnvelopesAsync());

        [HttpGet("api/product-envelopes/{id:int}")]
        public async Task<IActionResult> GetEnvelope(int id)
        {
            var item = await _repository.GetEnvelopeAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("api/product-envelopes")]
        public async Task<IActionResult> CreateEnvelope([FromBody] UpsertProductEnvelopeDto dto)
        {
            var item = await _repository.CreateEnvelopeAsync(dto);
            return CreatedAtAction(nameof(GetEnvelope), new { id = item.Id }, item);
        }

        [HttpPut("api/product-envelopes/{id:int}")]
        public async Task<IActionResult> UpdateEnvelope(int id, [FromBody] UpsertProductEnvelopeDto dto)
        {
            var item = await _repository.UpdateEnvelopeAsync(id, dto);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpGet("api/products/{productId:int}/versions")]
        public async Task<IActionResult> GetVersionsByProduct(int productId) =>
            Ok(await _repository.GetVersionsByProductAsync(productId));

        [HttpPost("api/products/{productId:int}/versions")]
        public async Task<IActionResult> CreateVersion(int productId, [FromBody] UpsertProductVersionDto dto)
        {
            var item = await _repository.CreateVersionAsync(productId, dto);
            return item is null ? NotFound() : CreatedAtAction(nameof(GetVersion), new { id = item.Id }, item);
        }

        [HttpGet("api/product-versions/{id:int}")]
        public async Task<IActionResult> GetVersion(int id)
        {
            var item = await _repository.GetVersionAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPut("api/product-versions/{id:int}")]
        public async Task<IActionResult> UpdateVersion(int id, [FromBody] UpsertProductVersionDto dto)
        {
            try
            {
                var item = await _repository.UpdateVersionAsync(id, dto);
                return item is null ? NotFound() : Ok(item);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
