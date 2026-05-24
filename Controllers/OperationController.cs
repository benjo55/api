using api.Dtos.Contract;
using api.Dtos.Operation;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Mapster;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/operations")]
public class OperationController : ControllerBase

{
    private readonly IOperationRepository _repository;
    private readonly IContractRepository _contractRepository;
    private readonly IOperationEngineService _operationEngineService;

    public OperationController(IOperationRepository repository, IContractRepository contractRepository, IOperationEngineService operationEngineService)
    {
        _repository = repository;
        _contractRepository = contractRepository;
        _operationEngineService = operationEngineService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var operations = await _repository.GetAllAsync(query);
        var dtoList = operations.Items.Adapt<IEnumerable<OperationDto>>();

        return Ok(new
        {
            operations.TotalCount,
            operations.TotalPages,
            operations.HasNextPage,
            operations.CurrentPage,
            Items = dtoList
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OperationDto>> Get(int id)
    {
        var op = await _repository.GetByIdAsync(id);
        if (op == null) return NotFound();
        return Ok(op.Adapt<OperationDto>());
    }

    [HttpGet("contract/{contractId}")]
    public async Task<ActionResult<IEnumerable<OperationDto>>> GetByContract(int contractId)
    {
        var ops = await _repository.GetByContractAsync(contractId);
        return Ok(ops.Adapt<IEnumerable<OperationDto>>());
    }

    [HttpPost]
    public async Task<ActionResult<OperationCreateResponseDto>> Create([FromBody] OperationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var entity = MapDtoToEntity(dto);
        var created = await _repository.AddAsync(entity);

        if (created == null || created.Id <= 0)
            return StatusCode(500, "Erreur interne : l’opération n’a pas pu être créée.");

        var contract = await _contractRepository.GetByIdAsync(created.ContractId);
        if (contract == null)
            return NotFound($"Contrat {created.ContractId} introuvable.");

        // 🧮 Préparation de la réponse simplifiée
        var response = new OperationCreateResponseDto
        {
            Id = created.Id,
            ContractId = created.ContractId,
            OperationType = created.Type.ToString(),
            Amount = created.Amount ?? 0m,
            Status = created.Status.ToString(),
            Allocations = created.Allocations.Select(a => new SimpleAllocationDto
            {
                SupportId = a.SupportId,
                Amount = a.Amount ?? 0m,
                Percentage = a.Percentage ?? 0m
            }).ToList(),
            CurrentValue = contract.CurrentValue,
            TotalPayments = contract.TotalPayments,
            TotalWithdrawals = contract.TotalWithdrawals
        };

        return CreatedAtAction(nameof(Get), new { id = created.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OperationDto>> Update(int id, [FromBody] OperationDto dto)
    {
        if (id != dto.Id) return BadRequest();

        var entity = MapDtoToEntity(dto);
        var updated = await _repository.UpdateAsync(entity);
        return Ok(updated.Adapt<OperationDto>());
    }

    private static Operation MapDtoToEntity(OperationDto dto)
    {
        var allocations = (dto.Allocations ?? new List<OperationSupportAllocationDto>())
            .Select(a => new OperationSupportAllocation
            {
                SupportId = a.SupportId,
                Amount = a.Amount,
                Percentage = a.Percentage,
                Shares = a.Shares,
                NavAtOperation = a.NavAtOperation,
                NavDateAtOperation = a.NavDateAtOperation,
                CompartmentId = a.CompartmentId,
                Flow = a.Flow,
            })
            .ToList();

        return new Operation
        {
            Id = dto.Id,
            ContractId = dto.ContractId,
            Type = dto.Type,
            Status = dto.Status,
            OperationDate = dto.OperationDate,
            ExecutionDate = dto.ExecutionDate,
            Amount = dto.Amount,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EUR" : dto.Currency,
            PaymentDetail = OperationDetailsMapper.ToPaymentModel(dto.Details),
            WithdrawalDetail = OperationDetailsMapper.ToWithdrawalModel(dto.Details),
            ArbitrageDetail = OperationDetailsMapper.ToArbitrageModel(dto.Details),
            AdvanceDetail = dto.AdvanceDetail is null
                ? null
                : new AdvanceDetail
                {
                    Amount = dto.AdvanceDetail.Amount,
                    InterestRate = dto.AdvanceDetail.InterestRate,
                    MaturityDate = dto.AdvanceDetail.MaturityDate,
                },
            Allocations = allocations,
        };
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _repository.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("update-valuations")]
    public async Task<IActionResult> UpdateValuations()
    {
        await _operationEngineService.UpdateValuationsAsync();
        return Ok(new { message = "UpdateValuationsAsync exécuté avec succès" });
    }

    [HttpPost("process-pending")]
    public async Task<IActionResult> ProcessPending()
    {
        await _operationEngineService.ProcessPendingOperationsAsync();
        return Ok(new { message = "ProcessPendingOperationsAsync exécuté avec succès" });
    }

    [HttpPost("{id}/schedule/suspend")]
    public async Task<IActionResult> SuspendSchedule(int id)
    {
        var updated = await _repository.SuspendScheduleAsync(id);
        if (updated == null)
            return NotFound();

        return Ok(updated.Adapt<OperationDto>());
    }

    [HttpPost("{id}/schedule/resume")]
    public async Task<IActionResult> ResumeSchedule(int id)
    {
        var updated = await _repository.ResumeScheduleAsync(id);
        if (updated == null)
            return NotFound();

        return Ok(updated.Adapt<OperationDto>());
    }

    [HttpPost("{id}/schedule/stop")]
    public async Task<IActionResult> StopSchedule(int id)
    {
        var updated = await _repository.StopScheduleAsync(id);
        if (updated == null)
            return NotFound();

        return Ok(updated.Adapt<OperationDto>());
    }

}
