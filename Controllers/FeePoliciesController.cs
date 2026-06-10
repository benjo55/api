using api.Data;
using api.Dtos.FeePolicy;
using api.Models;
using api.Models.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [ApiController]
    [Route("api/fee-policies")]
    public class FeePoliciesController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public FeePoliciesController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? productId, [FromQuery] int? contractId, [FromQuery] FeeCategory? category)
        {
            var query = _context.FeePolicies.AsNoTracking().AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(x => x.ProductId == productId.Value);
            }

            if (contractId.HasValue)
            {
                query = query.Where(x => x.ContractId == contractId.Value);
            }

            if (category.HasValue)
            {
                query = query.Where(x => x.Category == category.Value);
            }

            var policies = await query
                .OrderByDescending(x => x.IsOverride)
                .ThenByDescending(x => x.Scope)
                .ThenBy(x => x.Priority)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return Ok(policies.Select(ToDto));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var policy = await _context.FeePolicies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (policy == null)
            {
                return NotFound();
            }

            return Ok(ToDto(policy));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertFeePolicyDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = new FeePolicy();
            ApplyUpsert(entity, dto);
            entity.CreatedDate = DateTime.UtcNow;
            entity.UpdatedDate = DateTime.UtcNow;

            var error = ValidateScope(entity);
            if (error != null)
            {
                return BadRequest(new { message = error });
            }

            _context.FeePolicies.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpsertFeePolicyDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await _context.FeePolicies.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound();
            }

            ApplyUpsert(entity, dto);
            entity.UpdatedDate = DateTime.UtcNow;

            var error = ValidateScope(entity);
            if (error != null)
            {
                return BadRequest(new { message = error });
            }

            await _context.SaveChangesAsync();
            return Ok(ToDto(entity));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.FeePolicies.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.FeePolicies.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static string? ValidateScope(FeePolicy policy)
        {
            return policy.Scope switch
            {
                FeeScope.Product when policy.ProductId == null => "ProductId is required for Product scope.",
                FeeScope.Contract when policy.ContractId == null => "ContractId is required for Contract scope.",
                FeeScope.Compartment when policy.CompartmentId == null => "CompartmentId is required for Compartment scope.",
                FeeScope.FinancialSupport when policy.FinancialSupportId == null => "FinancialSupportId is required for FinancialSupport scope.",
                _ => null
            };
        }

        private static void ApplyUpsert(FeePolicy entity, UpsertFeePolicyDto dto)
        {
            entity.ProductId = dto.ProductId;
            entity.ContractId = dto.ContractId;
            entity.CompartmentId = dto.CompartmentId;
            entity.FinancialSupportId = dto.FinancialSupportId;
            entity.SupportType = dto.SupportType;
            entity.Category = dto.Category;
            entity.FeeType = dto.FeeType;
            entity.Scope = dto.Scope;
            entity.AmountMode = dto.AmountMode;
            entity.ApplyOn = dto.ApplyOn;
            entity.Rate = dto.Rate;
            entity.FixedAmount = dto.FixedAmount;
            entity.MinAmount = dto.MinAmount;
            entity.MaxAmount = dto.MaxAmount;
            entity.Priority = dto.Priority;
            entity.IsOverride = dto.IsOverride;
            entity.IsEnabled = dto.IsEnabled;
            entity.Frequency = dto.Frequency;
            entity.RateBase = dto.RateBase;
            entity.ProrataMethod = dto.ProrataMethod;
            entity.PostingMode = dto.PostingMode;
            entity.EffectiveDate = dto.EffectiveDate;
            entity.EndDate = dto.EndDate;
        }

        private static FeePolicyDto ToDto(FeePolicy entity)
        {
            return new FeePolicyDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                ContractId = entity.ContractId,
                CompartmentId = entity.CompartmentId,
                FinancialSupportId = entity.FinancialSupportId,
                SupportType = entity.SupportType,
                Category = entity.Category,
                FeeType = entity.FeeType,
                Scope = entity.Scope,
                AmountMode = entity.AmountMode,
                ApplyOn = entity.ApplyOn,
                Rate = entity.Rate,
                FixedAmount = entity.FixedAmount,
                MinAmount = entity.MinAmount,
                MaxAmount = entity.MaxAmount,
                Priority = entity.Priority,
                IsOverride = entity.IsOverride,
                IsEnabled = entity.IsEnabled,
                Frequency = entity.Frequency,
                RateBase = entity.RateBase,
                ProrataMethod = entity.ProrataMethod,
                PostingMode = entity.PostingMode,
                EffectiveDate = entity.EffectiveDate,
                EndDate = entity.EndDate,
                CreatedDate = entity.CreatedDate,
                UpdatedDate = entity.UpdatedDate
            };
        }
    }
}
