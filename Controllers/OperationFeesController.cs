using api.Data;
using api.Dtos.Operation;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [ApiController]
    [Route("api/operation-fees")]
    public class OperationFeesController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IOperationFeePolicyResolver _resolver;
        private readonly IFeeEngine _feeEngine;

        public OperationFeesController(ApplicationDBContext context, IOperationFeePolicyResolver resolver, IFeeEngine feeEngine)
        {
            _context = context;
            _resolver = resolver;
            _feeEngine = feeEngine;
        }

        [HttpPost("simulate")]
        public async Task<IActionResult> Simulate([FromBody] SimulateOperationFeesRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var contract = await _context.Contracts
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == request.ContractId);

            if (contract == null) return NotFound(new { message = "Contract not found" });

            // Load supports involved
            var supportIds = request.Allocations.Select(a => a.SupportId).Distinct().ToList();
            var supports = await _context.FinancialSupports
                .Where(s => supportIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            var groups = request.Allocations
                .GroupBy(a => new { a.SupportId, a.CompartmentId })
                .ToList();

            var results = new List<SimulatedFeeResult>();

            foreach (var g in groups)
            {
                var supportId = g.Key.SupportId;
                var compartmentId = g.Key.CompartmentId;

                supports.TryGetValue(supportId, out var support);

                var sourceAmount = request.Allocations
                    .Where(x => x.Flow == api.Models.OperationFlow.Source && x.SupportId == supportId && x.CompartmentId == compartmentId)
                    .Sum(x => x.Amount ?? 0m);

                var targetAmount = request.Allocations
                    .Where(x => x.Flow == api.Models.OperationFlow.Target && x.SupportId == supportId && x.CompartmentId == compartmentId)
                    .Sum(x => x.Amount ?? 0m);

                var calculated = _feeEngine.CalculateOperationFees(new OperationFeeCalculationRequest
                {
                    ContractId = contract.Id,
                    ProductId = contract.ProductId,
                    FinancialSupportId = supportId,
                    CompartmentId = compartmentId,
                    SupportType = support?.SupportType,
                    OperationType = request.OperationType,
                    OperationDate = request.OperationDate,
                    SourceAmount = sourceAmount,
                    TargetAmount = targetAmount
                });

                if (calculated.Any())
                {
                    foreach (var line in calculated)
                    {
                        results.Add(new SimulatedFeeResult
                        {
                            SupportId = supportId,
                            CompartmentId = compartmentId,
                            BaseAmount = line.BaseAmount,
                            FeeAmount = line.FeeAmount,
                            Mode = line.AmountMode.ToString(),
                            Rate = line.Rate,
                            FixedAmount = line.FixedAmount,
                            ApplyOn = line.ApplyOn.ToString()
                        });
                    }

                    continue;
                }

                var resolvedFees = _resolver.ResolveOperationFees(contract, support, request.OperationType, request.OperationDate);
                if (resolvedFees == null || !resolvedFees.Any()) continue;

                foreach (var rf in resolvedFees)
                {
                    var baseAmount = rf.ApplyOn == api.Models.Enum.FeeApplyOn.Source
                        ? sourceAmount
                        : targetAmount;

                    if (baseAmount <= 0m) continue;

                    var feeAmount = rf.Mode == api.Models.Enum.FeeAmountMode.Percentage
                        ? Math.Round(baseAmount * rf.Rate / 100m, 7)
                        : rf.FixedAmount;

                    if (feeAmount <= 0m) continue;

                    results.Add(new SimulatedFeeResult
                    {
                        SupportId = supportId,
                        CompartmentId = compartmentId,
                        BaseAmount = baseAmount,
                        FeeAmount = feeAmount,
                        Mode = rf.Mode.ToString(),
                        Rate = rf.Rate,
                        FixedAmount = rf.FixedAmount,
                        ApplyOn = rf.ApplyOn.ToString()
                    });
                }
            }

            return Ok(results);
        }
    }
}
