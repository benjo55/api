using api.Dtos.EuroFund;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/euro-funds")]
    [Authorize]
    public sealed class EuroFundsController : ControllerBase
    {
        private readonly IEuroFundRevaluationService _service;
        private readonly IEuroFundValuationService _valuationService;
        private readonly ICurrentUserAccessService _access;

        public EuroFundsController(
            IEuroFundRevaluationService service,
            IEuroFundValuationService valuationService,
            ICurrentUserAccessService access)
        {
            _service = service;
            _valuationService = valuationService;
            _access = access;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            if (!(await _access.GetScopeAsync()).IsBackOffice) return Forbid();
            return Ok(await _service.GetEuroFundsAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            if (!(await _access.GetScopeAsync()).IsBackOffice) return Forbid();
            var result = await _service.GetEuroFundAsync(id, cancellationToken);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPut("{id:int}/configuration")]
        public async Task<IActionResult> UpsertConfiguration(int id, [FromBody] EuroFundConfigurationDto dto, CancellationToken cancellationToken)
        {
            if (!(await _access.GetScopeAsync()).IsBackOffice) return Forbid();
            return Ok(await _service.UpsertConfigurationAsync(id, dto, cancellationToken));
        }

        [HttpGet("{id:int}/financial-years")]
        public async Task<IActionResult> GetYears(int id, CancellationToken cancellationToken)
        {
            if (!(await _access.GetScopeAsync()).IsBackOffice) return Forbid();
            return Ok(await _service.GetFinancialYearsAsync(id, cancellationToken));
        }

        [HttpPost("{id:int}/financial-years")]
        public async Task<IActionResult> CreateYear(int id, [FromBody] EuroFundFinancialYearDto dto, CancellationToken cancellationToken)
        {
            if (!(await _access.GetScopeAsync()).IsBackOffice) return Forbid();
            return Ok(await _service.UpsertFinancialYearAsync(id, dto.Year, dto, cancellationToken));
        }

        [HttpPut("{id:int}/financial-years/{year:int}")]
        public async Task<IActionResult> UpdateYear(int id, int year, [FromBody] EuroFundFinancialYearDto dto, CancellationToken cancellationToken)
        {
            if (!(await _access.GetScopeAsync()).IsBackOffice) return Forbid();
            return Ok(await _service.UpsertFinancialYearAsync(id, year, dto, cancellationToken));
        }

        [HttpPost("{id:int}/financial-years/{year:int}/preview")]
        public async Task<IActionResult> Preview(int id, int year, [FromQuery] DateTime? asOf, CancellationToken cancellationToken)
        {
            if (!(await _access.GetScopeAsync()).IsBackOffice) return Forbid();
            return Ok(await _service.PreviewAsync(id, year, asOf, cancellationToken));
        }

        [HttpPost("{id:int}/financial-years/{year:int}/finalize")]
        public async Task<IActionResult> Finalize(int id, int year, CancellationToken cancellationToken)
        {
            if (!(await _access.GetScopeAsync()).IsBackOffice) return Forbid();
            return Ok(await _service.FinalizeAsync(id, year, cancellationToken));
        }

        [HttpPost("reference-rates")]
        public async Task<IActionResult> AddReferenceRate([FromBody] ReferenceRateDto dto, CancellationToken cancellationToken)
        {
            if (!(await _access.GetScopeAsync()).IsBackOffice) return Forbid();
            return Ok(await _service.AddReferenceRateAsync(dto, cancellationToken));
        }

        [HttpGet("{id:int}/contracts/{contractId:int}/valuation")]
        public async Task<IActionResult> GetContractValuation(int id, int contractId, [FromQuery] DateTime? valuationDate, CancellationToken cancellationToken)
        {
            if (!await _access.CanReadContractAsync(contractId)) return NotFound();
            return Ok(await _valuationService.GetValuationAsync(contractId, id, valuationDate ?? DateTime.UtcNow, cancellationToken));
        }
    }
}
