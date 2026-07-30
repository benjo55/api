using System.Security.Claims;
using api.Dtos.Subscription;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/subscriptions/drafts")]
    [Authorize]
    public sealed class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionDraftService _service;

        public SubscriptionsController(ISubscriptionDraftService service)
        {
            _service = service;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            return Ok(await _service.GetCurrentAsync(userId.Value, cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            var draft = await _service.CreateAsync(userId.Value, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = draft.Id }, draft);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            var draft = await _service.GetByIdAsync(userId.Value, id, cancellationToken);
            return draft == null ? NotFound(new { message = "Brouillon de souscription introuvable." }) : Ok(draft);
        }

        [HttpPut("{id:int}/{stepKey}")]
        public async Task<IActionResult> SaveStep(int id, string stepKey, [FromBody] SaveSubscriptionStepRequestDto request, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            return await HandleAsync(() => _service.SaveStepAsync(userId.Value, id, stepKey, request.Data, cancellationToken));
        }

        [HttpPost("{id:int}/investor-profile/compute")]
        public async Task<IActionResult> ComputeInvestorProfile(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            return await HandleAsync(() => _service.ComputeInvestorProfileAsync(userId.Value, id, cancellationToken));
        }

        [HttpPost("{id:int}/recommendation")]
        public async Task<IActionResult> GenerateRecommendation(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            return await HandleAsync(() => _service.GenerateRecommendationAsync(userId.Value, id, cancellationToken));
        }

        [HttpPost("{id:int}/recommendation/accept")]
        public async Task<IActionResult> AcceptRecommendation(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            return await HandleAsync(() => _service.AcceptRecommendationAsync(userId.Value, id, cancellationToken));
        }

        [HttpPost("{id:int}/recommendation/override")]
        public async Task<IActionResult> OverrideRecommendation(int id, [FromBody] RecommendationOverrideRequestDto request, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            return await HandleAsync(() => _service.OverrideRecommendationAsync(userId.Value, id, request.Reason, cancellationToken));
        }

        [HttpGet("{id:int}/summary")]
        public Task<IActionResult> GetSummary(int id, CancellationToken cancellationToken) =>
            GetById(id, cancellationToken);

        [HttpPost("{id:int}/submit")]
        public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            return await HandleAsync(() => _service.SubmitAsync(userId.Value, id, cancellationToken));
        }

        private async Task<IActionResult> HandleAsync(Func<Task<SubscriptionDraftDto>> action)
        {
            try
            {
                return Ok(await action());
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private int? CurrentUserId()
        {
            var rawUserId = User.FindFirst("userId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(rawUserId, out var userId) ? userId : null;
        }
    }
}
