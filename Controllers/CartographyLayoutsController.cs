using api.Data;
using api.Dtos.Cmdb;
using api.Models.Cmdb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/cartography/layout")]
public sealed class CartographyLayoutsController : ControllerBase
{
    private static readonly string[] AllowedScopeTypes =
        ["EmployerEntity", "RootCi"];
    private readonly ApplicationDBContext _db;

    public CartographyLayoutsController(ApplicationDBContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<CartographyLayoutDto>> Get(
        [FromQuery] string scopeType,
        [FromQuery] string scopeKey,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateScope(scopeType, scopeKey);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var userName = CurrentUserName();
        var nodes = await _db.CartographyNodeLayouts.AsNoTracking()
            .Where(x => x.ScopeType == scopeType &&
                x.ScopeKey == scopeKey &&
                x.UserName == userName)
            .OrderBy(x => x.ConfigurationItemId)
            .Select(x => new CartographyNodePositionDto
            {
                ConfigurationItemId = x.ConfigurationItemId,
                X = x.PositionX,
                Y = x.PositionY,
            })
            .ToListAsync(cancellationToken);

        return Ok(new CartographyLayoutDto
        {
            ScopeType = scopeType,
            ScopeKey = scopeKey,
            Nodes = nodes,
        });
    }

    [HttpPut]
    public async Task<ActionResult<CartographyLayoutDto>> Save(
        CartographyLayoutDto dto,
        CancellationToken cancellationToken = default)
    {
        dto.ScopeType = dto.ScopeType.Trim();
        dto.ScopeKey = dto.ScopeKey.Trim();
        var validation = ValidateScope(dto.ScopeType, dto.ScopeKey);
        if (validation is not null)
        {
            return BadRequest(validation);
        }
        if (dto.Nodes.Count > 2000)
        {
            return BadRequest("Une disposition ne peut pas dépasser 2 000 CI.");
        }
        if (dto.Nodes.Any(x =>
                x.ConfigurationItemId <= 0 ||
                !double.IsFinite(x.X) ||
                !double.IsFinite(x.Y) ||
                Math.Abs(x.X) > 1_000_000 ||
                Math.Abs(x.Y) > 1_000_000))
        {
            return BadRequest("Une ou plusieurs positions sont invalides.");
        }

        var positions = dto.Nodes
            .GroupBy(x => x.ConfigurationItemId)
            .Select(x => x.Last())
            .ToList();
        var configurationItemIds = positions
            .Select(x => x.ConfigurationItemId)
            .ToHashSet();
        var validConfigurationItemIds = await _db.ConfigurationItems
            .AsNoTracking()
            .Where(x => configurationItemIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToHashSetAsync(cancellationToken);
        if (validConfigurationItemIds.Count != configurationItemIds.Count)
        {
            return BadRequest("Une ou plusieurs positions référencent un CI inconnu.");
        }

        var userName = CurrentUserName();
        var existing = await _db.CartographyNodeLayouts
            .Where(x => x.ScopeType == dto.ScopeType &&
                x.ScopeKey == dto.ScopeKey &&
                x.UserName == userName &&
                configurationItemIds.Contains(x.ConfigurationItemId))
            .ToDictionaryAsync(x => x.ConfigurationItemId, cancellationToken);
        var updatedDate = DateTime.UtcNow;

        foreach (var position in positions)
        {
            if (!existing.TryGetValue(position.ConfigurationItemId, out var layout))
            {
                layout = new CartographyNodeLayout
                {
                    ScopeType = dto.ScopeType,
                    ScopeKey = dto.ScopeKey,
                    UserName = userName,
                    ConfigurationItemId = position.ConfigurationItemId,
                };
                _db.CartographyNodeLayouts.Add(layout);
            }

            layout.PositionX = position.X;
            layout.PositionY = position.Y;
            layout.UpdatedDate = updatedDate;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await Get(dto.ScopeType, dto.ScopeKey, cancellationToken);
    }

    [HttpDelete]
    public async Task<IActionResult> Reset(
        [FromQuery] string scopeType,
        [FromQuery] string scopeKey,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateScope(scopeType, scopeKey);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var userName = CurrentUserName();
        var layouts = await _db.CartographyNodeLayouts
            .Where(x => x.ScopeType == scopeType &&
                x.ScopeKey == scopeKey &&
                x.UserName == userName)
            .ToListAsync(cancellationToken);
        _db.CartographyNodeLayouts.RemoveRange(layouts);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private string CurrentUserName() =>
        User.Identity?.Name?.Trim() is { Length: > 0 } userName
            ? userName[..Math.Min(userName.Length, 150)]
            : "anonymous";

    private static string? ValidateScope(string scopeType, string scopeKey)
    {
        if (!AllowedScopeTypes.Contains(scopeType, StringComparer.Ordinal))
        {
            return "Le type de portée doit être EmployerEntity ou RootCi.";
        }
        if (string.IsNullOrWhiteSpace(scopeKey) || scopeKey.Length > 250)
        {
            return "La clé de portée est obligatoire et limitée à 250 caractères.";
        }
        return null;
    }
}
