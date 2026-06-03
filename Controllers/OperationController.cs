using api.Dtos.Contract;
using api.Dtos.Operation;
using api.Dtos.FinancialSupport;
using api.Helpers;
using api.Interfaces;
using api.Models;
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
        var dtoList = operations.Items.Select(MapEntityToDto).ToList();

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
        return Ok(MapEntityToDto(op));
    }

    [HttpGet("contract/{contractId}")]
    public async Task<ActionResult<IEnumerable<OperationDto>>> GetByContract(int contractId)
    {
        var ops = await _repository.GetByContractAsync(contractId);
        return Ok(ops.Select(MapEntityToDto));
    }

    [HttpPost]
    public async Task<ActionResult<OperationCreateResponseDto>> Create([FromBody] OperationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (TryGetManualScheduleDates(dto, out var manualDates, out var paymentDetails))
        {
            var manualResult = await CreateManualScheduledPayments(dto, manualDates, paymentDetails!);
            return manualResult;
        }

        if (TryGetManualScheduledWithdrawals(dto, out var withdrawalDates, out var withdrawalDetails))
        {
            var manualResult = await CreateManualScheduledWithdrawals(dto, withdrawalDates, withdrawalDetails!);
            return manualResult;
        }

        if (TryGetManualScheduledArbitrages(dto, out var arbitrageDates, out var arbitrageDetails))
        {
            var manualResult = await CreateManualScheduledArbitrages(dto, arbitrageDates, arbitrageDetails!);
            return manualResult;
        }

        var entity = MapDtoToEntity(dto);
        var created = await _repository.AddAsync(entity);

        if (created == null || created.Id <= 0)
            return StatusCode(500, "Erreur interne : l’opération n’a pas pu être créée.");

        var contract = await _contractRepository.GetByIdAsync(created.ContractId);
        if (contract == null)
            return NotFound($"Contrat {created.ContractId} introuvable.");

        var response = BuildCreateResponse(created, contract);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OperationDto>> Update(int id, [FromBody] OperationDto dto)
    {
        if (id != dto.Id) return BadRequest();

        if (TryGetManualScheduleDates(dto, out var manualDates, out var paymentDetails))
        {
            var manualResult = await UpdateManualScheduledPayments(id, dto, manualDates, paymentDetails!);
            return manualResult;
        }

        if (TryGetManualScheduledWithdrawals(dto, out var withdrawalDates, out var withdrawalDetails))
        {
            var manualResult = await UpdateManualScheduledWithdrawals(id, dto, withdrawalDates, withdrawalDetails!);
            return manualResult;
        }

        if (TryGetManualScheduledArbitrages(dto, out var arbitrageDates, out var arbitrageDetails))
        {
            var manualResult = await UpdateManualScheduledArbitrages(id, dto, arbitrageDates, arbitrageDetails!);
            return manualResult;
        }

        var entity = MapDtoToEntity(dto);
        var updated = await _repository.UpdateAsync(entity);

        // Rechargement complet pour sérialiser des allocations/supports cohérents.
        var reloaded = await _repository.GetByIdAsync(updated.Id);
        return Ok(MapEntityToDto(reloaded ?? updated));
    }

    private static bool TryGetManualScheduleDates(
        OperationDto dto,
        out List<DateTime> manualDates,
        out PaymentDetailsDto? paymentDetails)
    {
        paymentDetails = dto.Details as PaymentDetailsDto;
        manualDates = new List<DateTime>();

        if (dto.Type != OperationType.ScheduledPayment ||
            paymentDetails == null ||
            !string.Equals(paymentDetails.PlanningMode, "manual", StringComparison.OrdinalIgnoreCase) ||
            paymentDetails.FixedDates == null ||
            paymentDetails.FixedDates.Count == 0)
        {
            return false;
        }

        manualDates = paymentDetails.FixedDates
            .Select(d => d)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        return manualDates.Count > 0;
    }

    private static bool TryGetManualScheduledWithdrawals(
        OperationDto dto,
        out List<DateTime> manualDates,
        out WithdrawalDetailsDto? withdrawalDetails)
    {
        withdrawalDetails = dto.Details as WithdrawalDetailsDto;
        manualDates = new List<DateTime>();

        if (dto.Type != OperationType.ScheduledWithdrawal ||
            withdrawalDetails == null ||
            !string.Equals(withdrawalDetails.PlanningMode, "manual", StringComparison.OrdinalIgnoreCase) ||
            withdrawalDetails.FixedDates == null ||
            withdrawalDetails.FixedDates.Count == 0)
        {
            return false;
        }

        manualDates = withdrawalDetails.FixedDates
            .Select(d => d)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        return manualDates.Count > 0;
    }

    private static bool TryGetManualScheduledArbitrages(
        OperationDto dto,
        out List<DateTime> manualDates,
        out ArbitrageDetailsDto? arbitrageDetails)
    {
        arbitrageDetails = dto.Details as ArbitrageDetailsDto;
        manualDates = new List<DateTime>();

        if (dto.Type != OperationType.ScheduledArbitrage ||
            arbitrageDetails == null ||
            !string.Equals(arbitrageDetails.PlanningMode, "manual", StringComparison.OrdinalIgnoreCase) ||
            arbitrageDetails.FixedDates == null ||
            arbitrageDetails.FixedDates.Count == 0)
        {
            return false;
        }

        manualDates = arbitrageDetails.FixedDates
            .Select(d => d)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        return manualDates.Count > 0;
    }

    private async Task<ActionResult<OperationCreateResponseDto>> CreateManualScheduledPayments(
        OperationDto dto,
        List<DateTime> manualDates,
        PaymentDetailsDto paymentDetails)
    {
        var groupId = string.IsNullOrWhiteSpace(paymentDetails.ScheduleGroupId)
            ? Guid.NewGuid().ToString("N")
            : paymentDetails.ScheduleGroupId!;

        var firstDate = manualDates[0];
        paymentDetails.ScheduleGroupId = groupId;
        paymentDetails.Frequency = "manual";
        paymentDetails.StartDate = firstDate;
        dto.OperationDate = firstDate;

        var createdMain = await _repository.AddAsync(MapDtoToEntity(dto));

        foreach (var date in manualDates.Skip(1))
        {
            var clone = CloneScheduledPaymentDto(dto, date, groupId);
            await _repository.AddAsync(MapDtoToEntity(clone));
        }

        var contract = await _contractRepository.GetByIdAsync(createdMain.ContractId);
        if (contract == null)
            return NotFound($"Contrat {createdMain.ContractId} introuvable.");

        var response = BuildCreateResponse(createdMain, contract);
        return CreatedAtAction(nameof(Get), new { id = createdMain.Id }, response);
    }

    private async Task<ActionResult<OperationCreateResponseDto>> CreateManualScheduledWithdrawals(
        OperationDto dto,
        List<DateTime> manualDates,
        WithdrawalDetailsDto withdrawalDetails)
    {
        var groupId = string.IsNullOrWhiteSpace(withdrawalDetails.ScheduleGroupId)
            ? Guid.NewGuid().ToString("N")
            : withdrawalDetails.ScheduleGroupId!;

        var firstDate = manualDates[0];
        withdrawalDetails.Mode = "scheduled";
        withdrawalDetails.Frequency = "manual";
        withdrawalDetails.StartDate = firstDate;
        withdrawalDetails.ScheduleGroupId = groupId;
        dto.OperationDate = firstDate;

        var createdMain = await _repository.AddAsync(MapDtoToEntity(dto));

        foreach (var date in manualDates.Skip(1))
        {
            var clone = CloneScheduledWithdrawalDto(dto, date, groupId);
            await _repository.AddAsync(MapDtoToEntity(clone));
        }

        var contract = await _contractRepository.GetByIdAsync(createdMain.ContractId);
        if (contract == null)
            return NotFound($"Contrat {createdMain.ContractId} introuvable.");

        var response = BuildCreateResponse(createdMain, contract);
        return CreatedAtAction(nameof(Get), new { id = createdMain.Id }, response);
    }

    private async Task<ActionResult<OperationCreateResponseDto>> CreateManualScheduledArbitrages(
        OperationDto dto,
        List<DateTime> manualDates,
        ArbitrageDetailsDto arbitrageDetails)
    {
        var groupId = string.IsNullOrWhiteSpace(arbitrageDetails.ScheduleGroupId)
            ? Guid.NewGuid().ToString("N")
            : arbitrageDetails.ScheduleGroupId!;

        var firstDate = manualDates[0];
        arbitrageDetails.Frequency = "manual";
        arbitrageDetails.StartDate = firstDate;
        arbitrageDetails.ScheduleGroupId = groupId;
        dto.OperationDate = firstDate;

        var createdMain = await _repository.AddAsync(MapDtoToEntity(dto));

        foreach (var date in manualDates.Skip(1))
        {
            var clone = CloneScheduledArbitrageDto(dto, date, groupId);
            await _repository.AddAsync(MapDtoToEntity(clone));
        }

        var contract = await _contractRepository.GetByIdAsync(createdMain.ContractId);
        if (contract == null)
            return NotFound($"Contrat {createdMain.ContractId} introuvable.");

        var response = BuildCreateResponse(createdMain, contract);
        return CreatedAtAction(nameof(Get), new { id = createdMain.Id }, response);
    }

    private async Task<ActionResult<OperationDto>> UpdateManualScheduledPayments(
        int id,
        OperationDto dto,
        List<DateTime> manualDates,
        PaymentDetailsDto paymentDetails)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var groupId = existing.PaymentDetail?.ScheduleGroupId;
        if (string.IsNullOrWhiteSpace(groupId))
        {
            groupId = string.IsNullOrWhiteSpace(paymentDetails.ScheduleGroupId)
                ? Guid.NewGuid().ToString("N")
                : paymentDetails.ScheduleGroupId;
        }

        var firstDate = manualDates[0];
        paymentDetails.ScheduleGroupId = groupId;
        paymentDetails.Frequency = "manual";
        paymentDetails.StartDate = firstDate;
        dto.OperationDate = firstDate;

        var updatedMain = await _repository.UpdateAsync(MapDtoToEntity(dto));

        var contractOps = await _repository.GetByContractAsync(dto.ContractId);
        var idsToDelete = contractOps
            .Where(o => o.Id != updatedMain.Id &&
                        o.Type == OperationType.ScheduledPayment &&
                        o.Status == OperationStatus.Pending &&
                        string.Equals(o.PaymentDetail?.ScheduleGroupId, groupId, StringComparison.Ordinal))
            .Select(o => o.Id)
            .ToList();

        foreach (var operationId in idsToDelete)
        {
            await _repository.DeleteAsync(operationId);
        }

        foreach (var date in manualDates.Skip(1))
        {
            var clone = CloneScheduledPaymentDto(dto, date, groupId!);
            await _repository.AddAsync(MapDtoToEntity(clone));
        }

        var reloaded = await _repository.GetByIdAsync(updatedMain.Id);
        return Ok(MapEntityToDto(reloaded ?? updatedMain));
    }

    private async Task<ActionResult<OperationDto>> UpdateManualScheduledWithdrawals(
        int id,
        OperationDto dto,
        List<DateTime> manualDates,
        WithdrawalDetailsDto withdrawalDetails)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var groupId = existing.WithdrawalDetail?.ScheduleGroupId;
        if (string.IsNullOrWhiteSpace(groupId))
        {
            groupId = string.IsNullOrWhiteSpace(withdrawalDetails.ScheduleGroupId)
                ? Guid.NewGuid().ToString("N")
                : withdrawalDetails.ScheduleGroupId;
        }

        var firstDate = manualDates[0];
        withdrawalDetails.Mode = "scheduled";
        withdrawalDetails.Frequency = "manual";
        withdrawalDetails.StartDate = firstDate;
        withdrawalDetails.ScheduleGroupId = groupId;
        dto.OperationDate = firstDate;

        var updatedMain = await _repository.UpdateAsync(MapDtoToEntity(dto));

        var contractOps = await _repository.GetByContractAsync(dto.ContractId);
        var idsToDelete = contractOps
            .Where(o => o.Id != updatedMain.Id &&
                        o.Type == OperationType.ScheduledWithdrawal &&
                        o.Status == OperationStatus.Pending &&
                        string.Equals(o.WithdrawalDetail?.ScheduleGroupId, groupId, StringComparison.Ordinal))
            .Select(o => o.Id)
            .ToList();

        foreach (var operationId in idsToDelete)
        {
            await _repository.DeleteAsync(operationId);
        }

        foreach (var date in manualDates.Skip(1))
        {
            var clone = CloneScheduledWithdrawalDto(dto, date, groupId!);
            await _repository.AddAsync(MapDtoToEntity(clone));
        }

        var reloaded = await _repository.GetByIdAsync(updatedMain.Id);
        return Ok(MapEntityToDto(reloaded ?? updatedMain));
    }

    private async Task<ActionResult<OperationDto>> UpdateManualScheduledArbitrages(
        int id,
        OperationDto dto,
        List<DateTime> manualDates,
        ArbitrageDetailsDto arbitrageDetails)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var groupId = existing.ArbitrageDetail?.ScheduleGroupId;
        if (string.IsNullOrWhiteSpace(groupId))
        {
            groupId = string.IsNullOrWhiteSpace(arbitrageDetails.ScheduleGroupId)
                ? Guid.NewGuid().ToString("N")
                : arbitrageDetails.ScheduleGroupId;
        }

        var firstDate = manualDates[0];
        arbitrageDetails.Frequency = "manual";
        arbitrageDetails.StartDate = firstDate;
        arbitrageDetails.ScheduleGroupId = groupId;
        dto.OperationDate = firstDate;

        var updatedMain = await _repository.UpdateAsync(MapDtoToEntity(dto));

        var contractOps = await _repository.GetByContractAsync(dto.ContractId);
        var idsToDelete = contractOps
            .Where(o => o.Id != updatedMain.Id &&
                        o.Type == OperationType.ScheduledArbitrage &&
                        o.Status == OperationStatus.Pending &&
                        string.Equals(o.ArbitrageDetail?.ScheduleGroupId, groupId, StringComparison.Ordinal))
            .Select(o => o.Id)
            .ToList();

        foreach (var operationId in idsToDelete)
        {
            await _repository.DeleteAsync(operationId);
        }

        foreach (var date in manualDates.Skip(1))
        {
            var clone = CloneScheduledArbitrageDto(dto, date, groupId!);
            await _repository.AddAsync(MapDtoToEntity(clone));
        }

        var reloaded = await _repository.GetByIdAsync(updatedMain.Id);
        return Ok(MapEntityToDto(reloaded ?? updatedMain));
    }

    private static OperationDto CloneScheduledPaymentDto(OperationDto source, DateTime operationDate, string scheduleGroupId)
    {
        var sourcePayment = source.Details as PaymentDetailsDto;

        return new OperationDto
        {
            Id = 0,
            ContractId = source.ContractId,
            ContractNumber = source.ContractNumber,
            Type = source.Type,
            Status = OperationStatus.Pending,
            OperationDate = operationDate,
            ExecutionDate = null,
            Amount = source.Amount,
            Currency = source.Currency,
            Details = sourcePayment == null
                ? source.Details
                : new PaymentDetailsDto
                {
                    Mode = sourcePayment.Mode,
                    PlanningMode = "manual",
                    FixedDates = null,
                    SourceOfFunds = sourcePayment.SourceOfFunds,
                    BankReference = sourcePayment.BankReference,
                    CheckNumber = sourcePayment.CheckNumber,
                    IssuerName = sourcePayment.IssuerName,
                    DepositDate = sourcePayment.DepositDate,
                    MandateId = sourcePayment.MandateId,
                    IbanMasked = sourcePayment.IbanMasked,
                    CreditorId = sourcePayment.CreditorId,
                    SequenceType = sourcePayment.SequenceType,
                    Frequency = "manual",
                    StartDate = operationDate,
                    ScheduleStatus = sourcePayment.ScheduleStatus,
                    ScheduleGroupId = scheduleGroupId,
                    SuspendedAt = sourcePayment.SuspendedAt,
                    StoppedAt = sourcePayment.StoppedAt,
                },
            AdvanceDetail = source.AdvanceDetail,
            Allocations = (source.Allocations ?? new List<OperationSupportAllocationDto>())
                .Select(a => new OperationSupportAllocationDto
                {
                    SupportId = a.SupportId,
                    Amount = a.Amount,
                    Percentage = a.Percentage,
                    Shares = null,
                    NavAtOperation = null,
                    NavDateAtOperation = null,
                    CompartmentId = a.CompartmentId,
                    Flow = a.Flow,
                    Support = null,
                })
                .ToList(),
        };
    }

    private static OperationDto CloneScheduledWithdrawalDto(OperationDto source, DateTime operationDate, string scheduleGroupId)
    {
        var sourceWithdrawal = source.Details as WithdrawalDetailsDto;

        return new OperationDto
        {
            Id = 0,
            ContractId = source.ContractId,
            ContractNumber = source.ContractNumber,
            Type = source.Type,
            Status = OperationStatus.Pending,
            OperationDate = operationDate,
            ExecutionDate = null,
            Amount = source.Amount,
            Currency = source.Currency,
            Details = sourceWithdrawal == null
                ? source.Details
                : new WithdrawalDetailsDto
                {
                    Mode = "scheduled",
                    PlanningMode = "manual",
                    FixedDates = null,
                    GrossAmount = sourceWithdrawal.GrossAmount,
                    TaxOption = sourceWithdrawal.TaxOption,
                    Reason = sourceWithdrawal.Reason,
                    AllocationMode = sourceWithdrawal.AllocationMode,
                    Frequency = "manual",
                    StartDate = operationDate,
                    ScheduleGroupId = scheduleGroupId,
                    EndDate = sourceWithdrawal.EndDate,
                    RevaluationRate = sourceWithdrawal.RevaluationRate,
                },
            AdvanceDetail = source.AdvanceDetail,
            Allocations = (source.Allocations ?? new List<OperationSupportAllocationDto>())
                .Select(a => new OperationSupportAllocationDto
                {
                    SupportId = a.SupportId,
                    Amount = a.Amount,
                    Percentage = a.Percentage,
                    Shares = null,
                    NavAtOperation = null,
                    NavDateAtOperation = null,
                    CompartmentId = a.CompartmentId,
                    Flow = a.Flow,
                    Support = null,
                })
                .ToList(),
        };
    }

    private static OperationDto CloneScheduledArbitrageDto(OperationDto source, DateTime operationDate, string scheduleGroupId)
    {
        var sourceArbitrage = source.Details as ArbitrageDetailsDto;

        return new OperationDto
        {
            Id = 0,
            ContractId = source.ContractId,
            ContractNumber = source.ContractNumber,
            Type = source.Type,
            Status = OperationStatus.Pending,
            OperationDate = operationDate,
            ExecutionDate = null,
            Amount = source.Amount,
            Currency = source.Currency,
            Details = sourceArbitrage == null
                ? source.Details
                : new ArbitrageDetailsDto
                {
                    Mode = sourceArbitrage.Mode,
                    PlanningMode = "manual",
                    FixedDates = null,
                    Frequency = "manual",
                    StartDate = operationDate,
                    ScheduleGroupId = scheduleGroupId,
                    Motive = sourceArbitrage.Motive,
                    RebalancePolicy = sourceArbitrage.RebalancePolicy,
                },
            AdvanceDetail = source.AdvanceDetail,
            Allocations = (source.Allocations ?? new List<OperationSupportAllocationDto>())
                .Select(a => new OperationSupportAllocationDto
                {
                    SupportId = a.SupportId,
                    Amount = a.Amount,
                    Percentage = a.Percentage,
                    Shares = null,
                    NavAtOperation = null,
                    NavDateAtOperation = null,
                    CompartmentId = a.CompartmentId,
                    Flow = a.Flow,
                    Support = null,
                })
                .ToList(),
        };
    }

    private static OperationCreateResponseDto BuildCreateResponse(Operation created, Contract contract)
    {
        return new OperationCreateResponseDto
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

        var reloaded = await _repository.GetByIdAsync(updated.Id);
        return Ok(MapEntityToDto(reloaded ?? updated));
    }

    [HttpPost("{id}/schedule/resume")]
    public async Task<IActionResult> ResumeSchedule(int id)
    {
        var updated = await _repository.ResumeScheduleAsync(id);
        if (updated == null)
            return NotFound();

        var reloaded = await _repository.GetByIdAsync(updated.Id);
        return Ok(MapEntityToDto(reloaded ?? updated));
    }

    [HttpPost("{id}/schedule/stop")]
    public async Task<IActionResult> StopSchedule(int id)
    {
        var updated = await _repository.StopScheduleAsync(id);
        if (updated == null)
            return NotFound();

        var reloaded = await _repository.GetByIdAsync(updated.Id);
        return Ok(MapEntityToDto(reloaded ?? updated));
    }

    private static OperationDto MapEntityToDto(Operation op)
    {
        var dto = new OperationDto
        {
            Id = op.Id,
            ContractId = op.ContractId,
            ContractNumber = op.Contract?.ContractNumber,
            Type = op.Type,
            Status = op.Status,
            OperationDate = op.OperationDate,
            ExecutionDate = op.ExecutionDate,
            Amount = op.Amount,
            Currency = string.IsNullOrWhiteSpace(op.Currency) ? "EUR" : op.Currency,
            Details = OperationDetailsMapper.ToDto(op),
            AdvanceDetail = op.AdvanceDetail is null
                ? null
                : new AdvanceDetailDto
                {
                    Amount = op.AdvanceDetail.Amount,
                    InterestRate = op.AdvanceDetail.InterestRate,
                    MaturityDate = op.AdvanceDetail.MaturityDate,
                },
            Allocations = (op.Allocations ?? new List<OperationSupportAllocation>())
                .Select(a => new OperationSupportAllocationDto
                {
                    SupportId = a.SupportId,
                    Amount = a.Amount,
                    Percentage = a.Percentage,
                    Shares = a.Shares,
                    NavAtOperation = a.NavAtOperation,
                    NavDateAtOperation = a.NavDateAtOperation,
                    CompartmentId = a.CompartmentId,
                    Flow = a.Flow,
                    Support = a.Support is null
                        ? null
                        : new FinancialSupportLightDto
                        {
                            Id = a.Support.Id,
                            Isin = a.Support.ISIN ?? string.Empty,
                            Label = a.Support.Label ?? string.Empty,
                            LastValuationAmount = a.Support.LastValuationAmount,
                        },
                })
                .ToList(),
        };

        return dto;
    }

}
