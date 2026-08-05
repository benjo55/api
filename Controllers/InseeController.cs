using api.Dtos.Insee;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/insee")]
    [ApiController]
    public sealed class InseeController : ControllerBase
    {
        private readonly IInseeGeoService _inseeGeoService;

        public InseeController(IInseeGeoService inseeGeoService)
        {
            _inseeGeoService = inseeGeoService;
        }

        [HttpGet("communes")]
        public async Task<IActionResult> SearchCommunes(
            [FromQuery] string search,
            [FromQuery] int limit = 20,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(search) || search.Trim().Length < 2)
            {
                return Ok(new { items = Array.Empty<InseeCommuneDto>() });
            }

            try
            {
                var items = await _inseeGeoService.SearchCommunesAsync(search, limit, cancellationToken);
                return Ok(new { items });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
