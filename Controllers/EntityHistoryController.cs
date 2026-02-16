using Microsoft.AspNetCore.Mvc;
using api.Interfaces;
using api.Repository; // ou ton namespace exact
using System.Threading.Tasks;

[ApiController]
[Route("api/entityHistory")]
public class EntityHistoryController : ControllerBase
{
    private readonly IEntityHistoryRepository _repo;

    public EntityHistoryController(IEntityHistoryRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> GetHistoryForEntity([FromQuery] string entityName, [FromQuery] int entityId)
    {
        var history = await _repo.GetHistoryForEntityAsync(entityName, entityId);
        return Ok(history);
    }
}
