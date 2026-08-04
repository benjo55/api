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
        private readonly ISubscriptionDocumentService _documentService;
        private readonly ISubscriptionMfaService _mfaService;
        private readonly ISubscriptionPaymentPreparationService _paymentService;
        private readonly ISubscriptionSignatureService _signatureService;

        public SubscriptionsController(
            ISubscriptionDraftService service,
            ISubscriptionDocumentService documentService,
            ISubscriptionMfaService mfaService,
            ISubscriptionPaymentPreparationService paymentService,
            ISubscriptionSignatureService signatureService)
        {
            _service = service;
            _documentService = documentService;
            _mfaService = mfaService;
            _paymentService = paymentService;
            _signatureService = signatureService;
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

        [HttpGet("{id:int}/documents")]
        public async Task<IActionResult> GetDocuments(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            try
            {
                return Ok(await _documentService.GetDossierAsync(userId.Value, id, cancellationToken));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/documents/generate")]
        public async Task<IActionResult> GenerateDocuments(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            try
            {
                return Ok(await _documentService.GenerateDossierAsync(userId.Value, id, User.Identity?.Name, cancellationToken));
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

        [HttpGet("{id:int}/documents/{artifactId:int}/download")]
        public async Task<IActionResult> DownloadDocument(int id, int artifactId, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            try
            {
                var file = await _documentService.GetDocumentFileAsync(userId.Value, id, artifactId, cancellationToken);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/payment/prepare")]
        public async Task<IActionResult> PreparePayment(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            try
            {
                return Ok(await _paymentService.PrepareAsync(userId.Value, id, cancellationToken));
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

        [HttpPost("{id:int}/mfa/challenge")]
        public async Task<IActionResult> CreateMfaChallenge(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            try
            {
                return Ok(await _mfaService.CreateChallengeAsync(userId.Value, id, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/mfa/totp/setup")]
        public async Task<IActionResult> CreateTotpSetup(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            try
            {
                return Ok(await _mfaService.CreateTotpSetupAsync(userId.Value, id, cancellationToken));
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

        [HttpPost("{id:int}/mfa/verify")]
        public async Task<IActionResult> VerifyMfa(int id, [FromBody] SubscriptionMfaVerifyRequestDto request, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            try
            {
                var result = await _mfaService.VerifyAsync(userId.Value, id, request.Code, cancellationToken);
                return result.Verified ? Ok(result) : BadRequest(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/signature/envelope")]
        public async Task<IActionResult> PrepareSignatureEnvelope(int id, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });
            try
            {
                return Ok(await _signatureService.PrepareEnvelopeAsync(userId.Value, id, User.Identity?.Name, cancellationToken));
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
