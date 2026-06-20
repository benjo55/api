using api.Data;
using api.Dtos.Contract;
using api.Models.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [Route("api/contracts/{contractId:int}/support-fees")]
    [ApiController]
    public class ContractSupportFeesController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public ContractSupportFeesController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails(
            [FromRoute] int contractId,
            [FromQuery] ContractSupportFeeNature? feeNature,
            [FromQuery] int? compartmentId,
            [FromQuery] int? supportId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var query = _context.ContractSupportFeeApplications
                .AsNoTracking()
                .Include(x => x.Support)
                .Include(x => x.Compartment)
                .Where(x => x.ContractId == contractId);

            if (feeNature.HasValue)
                query = query.Where(x => x.FeeNature == feeNature.Value);

            if (compartmentId.HasValue)
                query = query.Where(x => x.CompartmentId == compartmentId.Value);

            if (supportId.HasValue)
                query = query.Where(x => x.SupportId == supportId.Value);

            if (from.HasValue)
                query = query.Where(x => x.EffectiveDate.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(x => x.EffectiveDate.Date <= to.Value.Date);

            var rows = await query
                .OrderByDescending(x => x.EffectiveDate)
                .ThenByDescending(x => x.Id)
                .Select(x => new ContractSupportFeeApplicationDto
                {
                    Id = x.Id,
                    ContractId = x.ContractId,
                    FeeOperationId = x.FeeOperationId,
                    SourceOperationId = x.SourceOperationId,
                    FeeNature = x.FeeNature,
                    CompartmentId = x.CompartmentId,
                    CompartmentLabel = x.Compartment.Label,
                    SupportId = x.SupportId,
                    SupportLabel = x.Support.Label,
                    SupportIsin = x.Support.ISIN,
                    ApplyOn = x.ApplyOn,
                    BaseAmount = x.BaseAmount,
                    FeeAmount = x.FeeAmount,
                    FeeShares = x.FeeShares,
                    NavUsed = x.NavUsed,
                    NavDateUsed = x.NavDateUsed,
                    PolicySource = x.PolicySource,
                    PolicyId = x.PolicyId,
                    EffectiveDate = x.EffectiveDate,
                    CreatedDate = x.CreatedDate
                })
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("aggregate")]
        public async Task<IActionResult> GetAggregate(
            [FromRoute] int contractId,
            [FromQuery] ContractSupportFeeNature? feeNature,
            [FromQuery] int? compartmentId,
            [FromQuery] int? supportId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var query = _context.ContractSupportFeeApplications
                .AsNoTracking()
                .Include(x => x.Support)
                .Include(x => x.Compartment)
                .Where(x => x.ContractId == contractId);

            if (feeNature.HasValue)
                query = query.Where(x => x.FeeNature == feeNature.Value);

            if (compartmentId.HasValue)
                query = query.Where(x => x.CompartmentId == compartmentId.Value);

            if (supportId.HasValue)
                query = query.Where(x => x.SupportId == supportId.Value);

            if (from.HasValue)
                query = query.Where(x => x.EffectiveDate.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(x => x.EffectiveDate.Date <= to.Value.Date);

            var rows = await query
                .GroupBy(x => new
                {
                    x.ContractId,
                    x.CompartmentId,
                    CompartmentLabel = x.Compartment.Label,
                    x.SupportId,
                    SupportLabel = x.Support.Label,
                    SupportIsin = x.Support.ISIN,
                    x.FeeNature
                })
                .Select(g => new ContractSupportFeeAggregateDto
                {
                    ContractId = g.Key.ContractId,
                    CompartmentId = g.Key.CompartmentId,
                    CompartmentLabel = g.Key.CompartmentLabel,
                    SupportId = g.Key.SupportId,
                    SupportLabel = g.Key.SupportLabel,
                    SupportIsin = g.Key.SupportIsin,
                    FeeNature = g.Key.FeeNature,
                    TotalFeeAmount = g.Sum(x => x.FeeAmount),
                    TotalFeeShares = g.Sum(x => x.FeeShares)
                })
                .OrderBy(x => x.CompartmentLabel)
                .ThenBy(x => x.SupportLabel)
                .ThenBy(x => x.FeeNature)
                .ToListAsync();

            return Ok(rows);
        }
    }
}