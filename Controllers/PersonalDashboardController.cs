using api.Dtos.PersonalDashboard;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/personal-dashboard")]
    [Authorize]
    public sealed class PersonalDashboardController : ControllerBase
    {
        private readonly INewsFeedService _newsFeedService;
        private readonly IFinancialMarketService _financialMarketService;

        public PersonalDashboardController(
            INewsFeedService newsFeedService,
            IFinancialMarketService financialMarketService)
        {
            _newsFeedService = newsFeedService;
            _financialMarketService = financialMarketService;
        }

        [HttpGet("news")]
        [ProducesResponseType(typeof(NewsFeedDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNews(
            [FromQuery] NewsCategory category = NewsCategory.Top,
            [FromQuery] int limit = 6,
            CancellationToken cancellationToken = default)
        {
            if (limit is < 1 or > 12)
            {
                return BadRequest(new { message = "La limite doit être comprise entre 1 et 12." });
            }

            return Ok(await _newsFeedService.GetNewsFeedAsync(category, limit, cancellationToken));
        }

        [HttpGet("financial-markets")]
        [ProducesResponseType(typeof(FinancialMarketFeedDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFinancialMarkets(CancellationToken cancellationToken)
        {
            return Ok(await _financialMarketService.GetMarketFeedAsync(cancellationToken));
        }
    }
}
