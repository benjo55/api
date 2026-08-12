using api.Dtos.Documents;
using api.Exceptions;
using api.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/documents")]
    public sealed class DocumentsController : ControllerBase
    {
        private readonly IDocumentGenerationService _generationService;
        private readonly IDocumentDefinitionRegistry _registry;

        public DocumentsController(
            IDocumentGenerationService generationService,
            IDocumentDefinitionRegistry registry)
        {
            _generationService = generationService;
            _registry = registry;
        }

        [HttpGet("types")]
        public IActionResult GetTypes() =>
            Ok(_registry.List().Select(x => new
            {
                x.Key,
                x.DisplayName,
                x.TemplateVersion,
                x.DefaultPageSize,
                x.DefaultOrientation,
                RenderEngine = x.RenderEngine.ToString(),
                RenderOptions = x.EffectiveRenderOptions,
                x.SupportsPreview,
                x.SupportsDownload,
                x.SupportsArchive,
                x.SupportsEmail
            }));

        [HttpPost("{documentType}/generate")]
        public async Task<IActionResult> Generate(
            [FromRoute] string documentType,
            [FromBody] GenerateDocumentRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _generationService.GenerateAsync(
                    documentType,
                    request,
                    User,
                    cancellationToken);

                Response.Headers["X-Document-Type"] = result.DocumentType;
                Response.Headers["X-Document-Template-Version"] = result.TemplateVersion;
                Response.Headers["X-Document-Generated-At"] = result.GeneratedAt.ToString("O");
                if (!string.IsNullOrWhiteSpace(result.Hash))
                {
                    Response.Headers["X-Document-Hash"] = result.Hash;
                }

                if (result.Metadata.TryGetValue("correlationId", out var correlationId))
                {
                    Response.Headers["X-Document-Correlation-Id"] = correlationId;
                }

                if (result.Metadata.TryGetValue("renderEngine", out var renderEngine))
                {
                    Response.Headers["X-Document-Render-Engine"] = renderEngine;
                }

                return File(result.Content, result.ContentType, result.FileName);
            }
            catch (KeyNotFoundException ex) when (ex.Message == "DocumentTypeNotFound")
            {
                return NotFound(new { message = "Type de document inconnu." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (BusinessException ex) when (ex.Message == "DocumentDeliveryModeNotSupported")
            {
                return BadRequest(new { message = "Mode de génération non supporté pour ce document." });
            }
            catch (BusinessException ex)
            {
                return UnprocessableEntity(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
