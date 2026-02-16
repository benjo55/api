using api.Dtos.Contract;
using api.Dtos.Operation;
using api.Helpers;
using api.Interfaces;
using api.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/operations")]
public class OperationController : ControllerBase

{
    private readonly IOperationRepository _repository;
    private readonly IContractRepository _contractRepository;
    private readonly IMapper _mapper;
    private readonly IOperationEngineService _operationEngineService;

    public OperationController(IOperationRepository repository, IContractRepository contractRepository, IMapper mapper, IOperationEngineService operationEngineService)
    {
        _repository = repository;
        _contractRepository = contractRepository;
        _mapper = mapper;
        _operationEngineService = operationEngineService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var operations = await _repository.GetAllAsync(query);
        var dtoList = _mapper.Map<IEnumerable<OperationDto>>(operations.Items);

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
        return Ok(_mapper.Map<OperationDto>(op));
    }

    [HttpGet("contract/{contractId}")]
    public async Task<ActionResult<IEnumerable<OperationDto>>> GetByContract(int contractId)
    {
        var ops = await _repository.GetByContractAsync(contractId);
        return Ok(_mapper.Map<IEnumerable<OperationDto>>(ops));
    }

    [HttpGet("contract/{contractId}/compartment/{compartmentId}")]
    public async Task<ActionResult<IEnumerable<OperationDto>>> GetByCompartment(int contractId, int compartmentId)
    {
        var ops = await _repository.GetByCompartmentAsync(contractId, compartmentId);
        return Ok(_mapper.Map<IEnumerable<OperationDto>>(ops));
    }

    [HttpPost]
    public async Task<ActionResult<OperationCreateResponseDto>> Create([FromBody] OperationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var entity = _mapper.Map<Operation>(dto);
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

        var entity = _mapper.Map<Operation>(dto);
        var updated = await _repository.UpdateAsync(entity);
        return Ok(_mapper.Map<OperationDto>(updated));
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

}
