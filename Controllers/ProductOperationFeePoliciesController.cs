using api.Data;
using api.Dtos.Product;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace api.Controllers
{
    [ApiController]
    [Route("api/products/{productId}/operation-fees")]
    public class ProductOperationFeePoliciesController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public ProductOperationFeePoliciesController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetForProduct(int productId)
        {
            var policies = await _context.ProductOperationFeePolicies
                .Where(p => p.ProductId == productId)
                .ToListAsync();

            var dtos = policies.Select(p => new ProductOperationFeePolicyDto
            {
                Id = p.Id,
                ProductId = p.ProductId,
                FeeType = p.FeeType,
                Mode = p.Mode,
                Rate = p.Rate,
                FixedAmount = p.FixedAmount,
                ApplyOn = p.ApplyOn,
                IsEnabled = p.IsEnabled,
                EffectiveDate = p.EffectiveDate,
                EndDate = p.EndDate,
                CreatedDate = p.CreatedDate,
                UpdatedDate = p.UpdatedDate,
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int productId, int id)
        {
            var policy = await _context.ProductOperationFeePolicies.FindAsync(id);
            if (policy == null || policy.ProductId != productId) return NotFound();

            var dto = new ProductOperationFeePolicyDto
            {
                Id = policy.Id,
                ProductId = policy.ProductId,
                FeeType = policy.FeeType,
                Mode = policy.Mode,
                Rate = policy.Rate,
                FixedAmount = policy.FixedAmount,
                ApplyOn = policy.ApplyOn,
                IsEnabled = policy.IsEnabled,
                EffectiveDate = policy.EffectiveDate,
                EndDate = policy.EndDate,
                CreatedDate = policy.CreatedDate,
                UpdatedDate = policy.UpdatedDate,
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int productId, [FromBody] CreateProductOperationFeePolicyDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (productId != model.ProductId) model.ProductId = productId;

            // Respect unique constraint: ProductId + FeeType + ApplyOn
            var exists = await _context.ProductOperationFeePolicies.AnyAsync(p => p.ProductId == model.ProductId && p.FeeType == model.FeeType && p.ApplyOn == model.ApplyOn);
            if (exists) return Conflict(new { message = "A policy with same product/feeType/applyOn already exists." });

            var entity = new ProductOperationFeePolicy
            {
                ProductId = model.ProductId,
                FeeType = model.FeeType,
                Mode = model.Mode,
                Rate = model.Rate,
                FixedAmount = model.FixedAmount,
                ApplyOn = model.ApplyOn,
                IsEnabled = model.IsEnabled,
                EffectiveDate = model.EffectiveDate,
                EndDate = model.EndDate,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
            };

            _context.ProductOperationFeePolicies.Add(entity);
            await _context.SaveChangesAsync();

            var dto = new ProductOperationFeePolicyDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                FeeType = entity.FeeType,
                Mode = entity.Mode,
                Rate = entity.Rate,
                FixedAmount = entity.FixedAmount,
                ApplyOn = entity.ApplyOn,
                IsEnabled = entity.IsEnabled,
                EffectiveDate = entity.EffectiveDate,
                EndDate = entity.EndDate,
                CreatedDate = entity.CreatedDate,
                UpdatedDate = entity.UpdatedDate,
            };

            return CreatedAtAction(nameof(Get), new { productId = dto.ProductId, id = dto.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int productId, int id, [FromBody] UpdateProductOperationFeePolicyDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != model.Id || model.ProductId != productId) return BadRequest();

            var entity = await _context.ProductOperationFeePolicies.FirstOrDefaultAsync(p => p.Id == id && p.ProductId == productId);
            if (entity == null) return NotFound();

            // Check unique constraint if keys changed
            var conflict = await _context.ProductOperationFeePolicies.AnyAsync(p => p.Id != id && p.ProductId == model.ProductId && p.FeeType == model.FeeType && p.ApplyOn == model.ApplyOn);
            if (conflict) return Conflict(new { message = "Another policy with same product/feeType/applyOn exists." });

            entity.FeeType = model.FeeType;
            entity.Mode = model.Mode;
            entity.Rate = model.Rate;
            entity.FixedAmount = model.FixedAmount;
            entity.ApplyOn = model.ApplyOn;
            entity.IsEnabled = model.IsEnabled;
            entity.EffectiveDate = model.EffectiveDate;
            entity.EndDate = model.EndDate;
            entity.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int productId, int id)
        {
            var policy = await _context.ProductOperationFeePolicies.FindAsync(id);
            if (policy == null || policy.ProductId != productId) return NotFound();
            _context.ProductOperationFeePolicies.Remove(policy);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
