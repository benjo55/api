using api.Data;
using api.Dtos.Cmdb;
using api.Dtos.Generic;
using api.Helpers;
using api.Models.Cmdb;
using api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/integrationFlows")]
[Authorize(Policy = AuthorizationPolicies.UrbanisationAccess)]
public sealed class IntegrationFlowsController : ControllerBase
{
    private readonly ApplicationDBContext _db;

    public IntegrationFlowsController(ApplicationDBContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<IntegrationFlowDto>>> GetAll(
        [FromQuery] QueryObject query,
        CancellationToken cancellationToken)
    {
        var source = _db.IntegrationFlows.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x =>
                x.Code.Contains(search) || x.Name.Contains(search) ||
                x.SourceCi.Name.Contains(search) || x.TargetCi.Name.Contains(search));
        }

        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var pageNumber = Math.Max(query.PageNumber, 1);
        var totalCount = await source.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var items = await Project(source.OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate))
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return Ok(new PagedResult<IntegrationFlowDto>
        {
            Items = items,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            CurrentPage = pageNumber,
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IntegrationFlowDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await Project(_db.IntegrationFlows.AsNoTracking().Where(x => x.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<IntegrationFlowDto>> Create(
        IntegrationFlowWriteDto dto,
        CancellationToken cancellationToken)
    {
        var validation = await Validate(dto, null, false, cancellationToken);
        if (validation is not null) return validation;

        dto.Code = await GenerateUniqueCode(
            dto.SourceCiId,
            dto.TargetCiId,
            cancellationToken);
        var entity = new IntegrationFlow();
        Apply(dto, entity);
        _db.IntegrationFlows.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            await Project(_db.IntegrationFlows.AsNoTracking().Where(x => x.Id == entity.Id))
                .SingleAsync(cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<IntegrationFlowDto>> Update(
        int id,
        IntegrationFlowWriteDto dto,
        CancellationToken cancellationToken)
    {
        var entity = await _db.IntegrationFlows.FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();
        if (entity.Locked) return Conflict("Ce flux est verrouillé.");
        var validation = await Validate(dto, id, true, cancellationToken);
        if (validation is not null) return validation;

        Apply(dto, entity);
        entity.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(await Project(_db.IntegrationFlows.AsNoTracking().Where(x => x.Id == id))
            .SingleAsync(cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _db.IntegrationFlows.FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();
        if (entity.Locked) return Conflict("Ce flux est verrouillé.");
        _db.IntegrationFlows.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/locked")]
    public async Task<ActionResult<IntegrationFlowDto>> PatchLocked(
        int id,
        [FromBody] bool locked,
        CancellationToken cancellationToken)
    {
        var entity = await _db.IntegrationFlows.FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();
        entity.Locked = locked;
        entity.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(await Project(_db.IntegrationFlows.AsNoTracking().Where(x => x.Id == id))
            .SingleAsync(cancellationToken));
    }

    private async Task<ActionResult?> Validate(
        IntegrationFlowWriteDto dto,
        int? currentId,
        bool codeRequired,
        CancellationToken cancellationToken)
    {
        if (dto.SourceCiId == dto.TargetCiId) return BadRequest("La source et la cible doivent être différentes.");
        if (string.IsNullOrWhiteSpace(dto.Name) ||
            (codeRequired && string.IsNullOrWhiteSpace(dto.Code)))
            return BadRequest(codeRequired
                ? "Le code et le nom sont obligatoires."
                : "Le nom est obligatoire.");
        if (codeRequired &&
            await _db.IntegrationFlows.AnyAsync(
                x => x.Code == dto.Code && x.Id != currentId,
                cancellationToken))
            return Conflict("Ce code de flux existe déjà.");
        var ciCount = await _db.ConfigurationItems.CountAsync(
            x => x.Id == dto.SourceCiId || x.Id == dto.TargetCiId, cancellationToken);
        if (ciCount != 2) return BadRequest("La source ou la cible est inconnue.");
        if (!await _db.ExchangePatterns.AnyAsync(x => x.Id == dto.ExchangePatternId && x.IsActive, cancellationToken))
            return BadRequest("Le pattern d'échange est inconnu ou inactif.");
        return null;
    }

    private async Task<string> GenerateUniqueCode(
        int sourceCiId,
        int targetCiId,
        CancellationToken cancellationToken)
    {
        var endpoints = await _db.ConfigurationItems.AsNoTracking()
            .Where(x => x.Id == sourceCiId || x.Id == targetCiId)
            .Select(x => new { x.Id, x.ExternalCiNumber })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var sourceNumber = endpoints[sourceCiId].ExternalCiNumber;
        var targetNumber = endpoints[targetCiId].ExternalCiNumber;
        var baseCode = BuildFlowCode(sourceNumber, targetNumber);

        var usedCodeValues = await _db.IntegrationFlows.AsNoTracking()
            .Where(x => x.Code == baseCode || x.Code.StartsWith(baseCode + "_"))
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);
        var usedCodes = usedCodeValues.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!usedCodes.Contains(baseCode)) return baseCode;

        for (var sequence = 2; sequence < 10_000; sequence++)
        {
            var candidate = $"{baseCode}_{sequence:00}";
            if (!usedCodes.Contains(candidate)) return candidate;
        }

        throw new InvalidOperationException(
            "Impossible de générer un code unique pour ce couple de CI.");
    }

    internal static string BuildFlowCode(string sourceCiNumber, string targetCiNumber)
    {
        static string Normalize(string value) =>
            string.Concat(value.Trim().ToUpperInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '_'))
                .Trim('_');

        var source = Normalize(sourceCiNumber);
        var target = Normalize(targetCiNumber);
        return $"FLUX_{source}_{target}"[..Math.Min(74, $"FLUX_{source}_{target}".Length)];
    }

    private static void Apply(IntegrationFlowWriteDto dto, IntegrationFlow entity)
    {
        entity.Code = dto.Code.Trim().ToUpperInvariant();
        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.SourceCiId = dto.SourceCiId;
        entity.TargetCiId = dto.TargetCiId;
        entity.BrokerCiId = dto.BrokerCiId;
        entity.ExchangePatternId = dto.ExchangePatternId;
        entity.TechnologyId = dto.TechnologyId;
        entity.FlowGroupCode = dto.FlowGroupCode?.Trim();
        entity.Status = dto.Status;
        entity.Criticality = dto.Criticality;
        entity.TransportProtocol = dto.TransportProtocol?.Trim();
        entity.ChannelName = dto.ChannelName?.Trim();
        entity.EndpointReference = dto.EndpointReference?.Trim();
        entity.AverageMessagesPerDay = dto.AverageMessagesPerDay;
        entity.PeakMessagesPerMinute = dto.PeakMessagesPerMinute;
        entity.AveragePayloadKb = dto.AveragePayloadKb;
        entity.ExpectedLatencyMs = dto.ExpectedLatencyMs;
        entity.DataClassification = dto.DataClassification;
        entity.ContainsPersonalData = dto.ContainsPersonalData;
        entity.IsEncryptedInTransit = dto.IsEncryptedInTransit;
        entity.ValidFromDate = dto.ValidFromDate;
        entity.ValidToDate = dto.ValidToDate;
    }

    private static IQueryable<IntegrationFlowDto> Project(IQueryable<IntegrationFlow> source) =>
        source.Select(x => new IntegrationFlowDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            SourceCiId = x.SourceCiId,
            SourceCiName = x.SourceCi.Name,
            SourceCiNumber = x.SourceCi.ExternalCiNumber,
            TargetCiId = x.TargetCiId,
            TargetCiName = x.TargetCi.Name,
            TargetCiNumber = x.TargetCi.ExternalCiNumber,
            BrokerCiId = x.BrokerCiId,
            BrokerCiName = x.BrokerCi != null ? x.BrokerCi.Name : null,
            ExchangePatternId = x.ExchangePatternId,
            ExchangePatternName = x.ExchangePattern.Name,
            PatternFamily = x.ExchangePattern.Family,
            TechnologyId = x.TechnologyId,
            TechnologyName = x.Technology != null ? x.Technology.Name : null,
            FlowGroupCode = x.FlowGroupCode,
            Status = x.Status,
            Criticality = x.Criticality,
            TransportProtocol = x.TransportProtocol,
            ChannelName = x.ChannelName,
            EndpointReference = x.EndpointReference,
            AverageMessagesPerDay = x.AverageMessagesPerDay,
            PeakMessagesPerMinute = x.PeakMessagesPerMinute,
            AveragePayloadKb = x.AveragePayloadKb,
            ExpectedLatencyMs = x.ExpectedLatencyMs,
            DataClassification = x.DataClassification,
            ContainsPersonalData = x.ContainsPersonalData,
            IsEncryptedInTransit = x.IsEncryptedInTransit,
            ValidFromDate = x.ValidFromDate,
            ValidToDate = x.ValidToDate,
            Locked = x.Locked,
            CreatedDate = x.CreatedDate,
            UpdatedDate = x.UpdatedDate,
        });
}
