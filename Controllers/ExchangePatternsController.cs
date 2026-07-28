using api.Data;
using api.Dtos.Cmdb;
using api.Dtos.Generic;
using api.Helpers;
using api.Models.Cmdb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/exchangePatterns")]
public sealed class ExchangePatternsController : ControllerBase
{
    private readonly ApplicationDBContext _db;

    public ExchangePatternsController(ApplicationDBContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ExchangePatternDto>>> GetAll(
        [FromQuery] QueryObject query,
        CancellationToken cancellationToken)
    {
        var source = _db.ExchangePatterns.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x =>
                x.Code.Contains(search) ||
                x.Name.Contains(search) ||
                x.Family.Contains(search) ||
                (x.Description != null && x.Description.Contains(search)) ||
                (x.TypicalUses != null && x.TypicalUses.Contains(search)));
        }

        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var pageNumber = Math.Max(query.PageNumber, 1);
        var totalCount = await source.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var items = await Project(source.OrderBy(x => x.Family).ThenBy(x => x.Name))
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return Ok(new PagedResult<ExchangePatternDto>
        {
            Items = items,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            CurrentPage = pageNumber,
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExchangePatternDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await Project(_db.ExchangePatterns.AsNoTracking().Where(x => x.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ExchangePatternDto>> Create(
        ExchangePatternWriteDto dto,
        CancellationToken cancellationToken)
    {
        if (await _db.ExchangePatterns.AnyAsync(x => x.Code == dto.Code, cancellationToken))
        {
            return Conflict("Ce code de pattern existe déjà.");
        }
        var entity = new ExchangePattern();
        Apply(dto, entity);
        _db.ExchangePatterns.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            await Project(_db.ExchangePatterns.AsNoTracking().Where(x => x.Id == entity.Id))
                .SingleAsync(cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExchangePatternDto>> Update(
        int id,
        ExchangePatternWriteDto dto,
        CancellationToken cancellationToken)
    {
        var entity = await _db.ExchangePatterns.FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();
        if (entity.Locked) return Conflict("Ce pattern est verrouillé.");
        if (await _db.ExchangePatterns.AnyAsync(x => x.Code == dto.Code && x.Id != id, cancellationToken))
        {
            return Conflict("Ce code de pattern existe déjà.");
        }
        Apply(dto, entity);
        entity.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(await Project(_db.ExchangePatterns.AsNoTracking().Where(x => x.Id == id))
            .SingleAsync(cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _db.ExchangePatterns.FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();
        if (entity.IsSystem || entity.Locked) return Conflict("Un pattern système ou verrouillé ne peut pas être supprimé.");
        if (await _db.IntegrationFlows.AnyAsync(x => x.ExchangePatternId == id, cancellationToken))
        {
            return Conflict("Ce pattern est utilisé par un flux.");
        }
        _db.ExchangePatterns.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/locked")]
    public async Task<ActionResult<ExchangePatternDto>> PatchLocked(
        int id,
        [FromBody] bool locked,
        CancellationToken cancellationToken)
    {
        var entity = await _db.ExchangePatterns.FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();
        entity.Locked = locked;
        entity.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(await Project(_db.ExchangePatterns.AsNoTracking().Where(x => x.Id == id))
            .SingleAsync(cancellationToken));
    }

    private static void Apply(ExchangePatternWriteDto dto, ExchangePattern entity)
    {
        entity.Code = dto.Code.Trim().ToUpperInvariant();
        entity.Name = dto.Name.Trim();
        entity.Family = dto.Family.Trim();
        entity.InteractionMode = dto.InteractionMode;
        entity.TriggerMode = dto.TriggerMode;
        entity.DefaultTechnologyId = dto.DefaultTechnologyId;
        entity.Description = dto.Description?.Trim();
        entity.TypicalUses = dto.TypicalUses?.Trim();
        entity.IsActive = dto.IsActive;
    }

    private static IQueryable<ExchangePatternDto> Project(IQueryable<ExchangePattern> source) =>
        source.Select(x => new ExchangePatternDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Family = x.Family,
            InteractionMode = x.InteractionMode,
            TriggerMode = x.TriggerMode,
            DefaultTechnologyId = x.DefaultTechnologyId,
            DefaultTechnologyName = x.DefaultTechnology != null ? x.DefaultTechnology.Name : null,
            Description = x.Description,
            TypicalUses = x.TypicalUses,
            IsActive = x.IsActive,
            IsSystem = x.IsSystem,
            Locked = x.Locked,
            CreatedDate = x.CreatedDate,
            UpdatedDate = x.UpdatedDate,
        });
}
