using api.Dtos.LegalDocuments;
using api.Interfaces;
using api.Models.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [Route("api/legal-documents")]
    [ApiController]
    public sealed class LegalDocumentsController : ControllerBase
    {
        private readonly IDocumentStructureService _structureService;
        private readonly IDocumentVersioningService _versioningService;
        private readonly IDocumentWorkflowService _workflowService;
        private readonly IDocumentValidationService _validationService;
        private readonly IDocumentRenderService _renderService;
        private readonly IDocumentComparisonService _comparisonService;
        private readonly IDocumentAuditService _auditService;
        private readonly IDocumentBinaryStorage _storage;
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly IProductDocumentAssignmentService _productAssignmentService;
        private readonly IDocumentVariableResolver _variableResolver;
        private readonly api.Data.ApplicationDBContext _db;

        public LegalDocumentsController(
            IDocumentStructureService structureService,
            IDocumentVersioningService versioningService,
            IDocumentWorkflowService workflowService,
            IDocumentValidationService validationService,
            IDocumentRenderService renderService,
            IDocumentComparisonService comparisonService,
            IDocumentAuditService auditService,
            IDocumentBinaryStorage storage,
            IPdfGenerationService pdfGenerationService,
            IProductDocumentAssignmentService productAssignmentService,
            IDocumentVariableResolver variableResolver,
            api.Data.ApplicationDBContext db)
        {
            _structureService = structureService;
            _versioningService = versioningService;
            _workflowService = workflowService;
            _validationService = validationService;
            _renderService = renderService;
            _comparisonService = comparisonService;
            _auditService = auditService;
            _storage = storage;
            _pdfGenerationService = pdfGenerationService;
            _productAssignmentService = productAssignmentService;
            _variableResolver = variableResolver;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetDefinitions([FromQuery] bool? isLibrary, CancellationToken cancellationToken)
        {
            var definitions = await _structureService.GetDefinitionsAsync(isLibrary, cancellationToken);
            return Ok(definitions);
        }

        [HttpGet("{definitionId:int}")]
        public async Task<IActionResult> GetDefinition([FromRoute] int definitionId, CancellationToken cancellationToken)
        {
            var definition = await _structureService.GetDefinitionAsync(definitionId, cancellationToken);
            return definition is null ? NotFound() : Ok(definition);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDefinition([FromBody] CreateLegalDocumentDefinitionDto dto, CancellationToken cancellationToken)
        {
            var created = await _structureService.CreateDefinitionAsync(dto, User.Identity?.Name, cancellationToken);
            return CreatedAtAction(nameof(GetDefinition), new { definitionId = created.Id }, created);
        }

        [HttpGet("revisions/{revisionId:int}")]
        public async Task<IActionResult> GetRevision([FromRoute] int revisionId, CancellationToken cancellationToken)
        {
            var revision = await _structureService.GetRevisionAsync(revisionId, cancellationToken);
            return revision is null ? NotFound() : Ok(revision);
        }

        [HttpGet("published-revisions")]
        public async Task<IActionResult> GetPublishedRevisions([FromQuery] LegalDocumentType? type, CancellationToken cancellationToken)
        {
            var query = _db.LegalDocumentRevisions
                .AsNoTracking()
                .Include(x => x.LegalDocumentDefinition)
                .Where(x => x.Status == DocumentRevisionStatus.Published);

            if (type is not null)
            {
                query = query.Where(x => x.LegalDocumentDefinition.Type == type.Value);
            }

            var revisions = await query
                .OrderBy(x => x.LegalDocumentDefinition.Code)
                .ThenByDescending(x => x.MajorVersion)
                .ThenByDescending(x => x.MinorVersion)
                .Select(x => new PublishedLegalDocumentRevisionDto(
                    x.Id,
                    x.LegalDocumentDefinitionId,
                    x.LegalDocumentDefinition.Code,
                    x.LegalDocumentDefinition.Name,
                    x.LegalDocumentDefinition.Type,
                    x.MajorVersion,
                    x.MinorVersion,
                    x.PublishedAt))
                .ToListAsync(cancellationToken);

            return Ok(revisions);
        }

        [HttpPost("{definitionId:int}/versions")]
        public async Task<IActionResult> CreateVersion([FromRoute] int definitionId, [FromBody] CreateDocumentVersionDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var created = await _versioningService.CreateVersionAsync(definitionId, dto, User.Identity?.Name, cancellationToken);
                return Ok(created);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "The document was modified by another user." });
            }
        }

        [HttpPost("revisions/{revisionId:int}/nodes")]
        public async Task<IActionResult> AddNode([FromRoute] int revisionId, [FromBody] CreateLegalDocumentNodeDto dto, CancellationToken cancellationToken)
        {
            var created = await _structureService.AddNodeAsync(revisionId, dto, User.Identity?.Name, cancellationToken);
            return Ok(created);
        }

        [HttpPut("nodes/{nodeId:int}")]
        public async Task<IActionResult> UpdateNode([FromRoute] int nodeId, [FromBody] UpdateLegalDocumentNodeDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _structureService.UpdateNodeAsync(nodeId, dto, User.Identity?.Name, cancellationToken);
                return Ok(updated);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "The node was modified by another user." });
            }
        }

        [HttpPost("nodes/{nodeId:int}/move")]
        public async Task<IActionResult> MoveNode([FromRoute] int nodeId, [FromBody] MoveLegalDocumentNodeDto dto, CancellationToken cancellationToken)
        {
            try
            {
                await _structureService.MoveNodeAsync(nodeId, dto, User.Identity?.Name, cancellationToken);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "The node was modified by another user." });
            }
        }

        [HttpPost("nodes/{nodeId:int}/duplicate")]
        public async Task<IActionResult> DuplicateNode([FromRoute] int nodeId, CancellationToken cancellationToken)
        {
            var duplicate = await _structureService.DuplicateSubtreeAsync(nodeId, User.Identity?.Name, cancellationToken);
            return Ok(duplicate);
        }

        [HttpGet("reusable-nodes")]
        public async Task<IActionResult> GetReusableNodes(
            [FromQuery] int excludeRevisionId,
            [FromQuery] DocumentNodeType? type,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            var nodes = await _structureService.GetReusableNodesAsync(excludeRevisionId, type, search, cancellationToken);
            return Ok(nodes);
        }

        [HttpPost("revisions/{revisionId:int}/nodes/import")]
        public async Task<IActionResult> ImportNode(
            [FromRoute] int revisionId,
            [FromBody] ImportDocumentNodeDto dto,
            CancellationToken cancellationToken)
        {
            var imported = await _structureService.ImportSubtreeAsync(revisionId, dto, User.Identity?.Name, cancellationToken);
            return Ok(imported);
        }

        [HttpDelete("nodes/{nodeId:int}")]
        public async Task<IActionResult> DeleteNode([FromRoute] int nodeId, [FromQuery] string rowVersion, CancellationToken cancellationToken)
        {
            try
            {
                await _structureService.DeleteNodeAsync(nodeId, rowVersion, User.Identity?.Name, cancellationToken);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "The node was modified by another user." });
            }
        }

        [HttpPost("revisions/{revisionId:int}/submit")]
        public async Task<IActionResult> Submit([FromRoute] int revisionId, [FromBody] WorkflowTransitionDto dto, CancellationToken cancellationToken)
        {
            var revision = await _workflowService.SubmitForReviewAsync(revisionId, dto, User.Identity?.Name, cancellationToken);
            return Ok(revision);
        }

        [HttpPost("revisions/{revisionId:int}/validate")]
        public async Task<IActionResult> Validate([FromRoute] int revisionId, [FromBody] WorkflowTransitionDto dto, CancellationToken cancellationToken)
        {
            var result = await _workflowService.ValidateAsync(revisionId, dto, User.Identity?.Name, cancellationToken);
            return result.IsValid ? Ok(result) : BadRequest(result);
        }

        [HttpPost("revisions/{revisionId:int}/publish")]
        public async Task<IActionResult> Publish([FromRoute] int revisionId, [FromBody] WorkflowTransitionDto dto, CancellationToken cancellationToken)
        {
            var revision = await _workflowService.PublishAsync(revisionId, dto, User.Identity?.Name, cancellationToken);
            return Ok(revision);
        }

        [HttpGet("revisions/{revisionId:int}/validation")]
        public async Task<IActionResult> ValidateReadOnly([FromRoute] int revisionId, CancellationToken cancellationToken)
        {
            var result = await _validationService.ValidateRevisionAsync(revisionId, includePdfGeneration: false, cancellationToken);
            return Ok(result);
        }

        [HttpPost("revisions/{revisionId:int}/preview")]
        public async Task<IActionResult> GeneratePreview([FromRoute] int revisionId, [FromBody] DocumentPreviewRequestDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var preview = await _renderService.GeneratePreviewAsync(revisionId, dto.RevisionStamp, User.Identity?.Name, cancellationToken);
                return Ok(preview);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Problem(
                    ex.Message,
                    title: "Génération PDF impossible",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (IOException ex)
            {
                return Problem(
                    $"Le stockage des PDF n'est pas accessible: {ex.Message}",
                    title: "Stockage PDF inaccessible",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Problem(
                    $"Le compte applicatif n'a pas les droits requis sur le stockage PDF: {ex.Message}",
                    title: "Droits de stockage PDF insuffisants",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("artifacts/{artifactId:int}")]
        public async Task<IActionResult> DownloadArtifact([FromRoute] int artifactId, CancellationToken cancellationToken)
        {
            try
            {
                var artifact = await _db.DocumentArtifacts.FirstOrDefaultAsync(x => x.Id == artifactId, cancellationToken);
                if (artifact is null)
                {
                    return NotFound();
                }

                try
                {
                    var content = await _storage.ReadAsync(artifact.StorageKey, cancellationToken);
                    return File(content, artifact.ContentType, artifact.FileName);
                }
                catch
                {
                    if (artifact.LegalDocumentRevisionId is null || artifact.Type != DocumentArtifactType.PreviewPdf)
                    {
                        return NotFound(new { message = "Le PDF demandé n'existe plus sur le stockage." });
                    }

                    try
                    {
                        var model = await _renderService.BuildRenderModelAsync(artifact.LegalDocumentRevisionId.Value, cancellationToken);
                        var html = _renderService.RenderCanonicalHtml(model);
                        var content = await _pdfGenerationService.GeneratePdfAsync(html, model.Layout.PageFormat, cancellationToken);

                        var saved = await _storage.SaveAsync(content, ".pdf", cancellationToken);
                        artifact.StorageKey = saved.StorageKey;
                        artifact.Hash = saved.Hash;
                        artifact.Size = saved.Size;
                        artifact.ContentType = "application/pdf";
                        artifact.GeneratedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync(cancellationToken);

                        return File(content, "application/pdf", artifact.FileName);
                    }
                    catch (Exception regenerationEx)
                    {
                        return Problem(
                            $"Le PDF n'est plus lisible et sa régénération a échoué: {regenerationEx.Message}",
                            statusCode: StatusCodes.Status500InternalServerError);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("revisions/{revisionId:int}/history")]
        public async Task<IActionResult> GetHistory([FromRoute] int revisionId, CancellationToken cancellationToken)
        {
            var events = await _auditService.GetHistoryAsync(revisionId, cancellationToken);
            return Ok(events);
        }

        [HttpGet("revisions/compare")]
        public async Task<IActionResult> Compare([FromQuery] int leftRevisionId, [FromQuery] int rightRevisionId, CancellationToken cancellationToken)
        {
            var comparison = await _comparisonService.CompareAsync(leftRevisionId, rightRevisionId, cancellationToken);
            return Ok(comparison);
        }

        [HttpGet("variables")]
        public IActionResult GetVariables()
        {
            return Ok(_variableResolver.GetVariableDefinitions());
        }

        [HttpGet("products/{productId:int}/documents")]
        public async Task<IActionResult> GetProductDocuments([FromRoute] int productId, CancellationToken cancellationToken)
        {
            var assignments = await _productAssignmentService.GetProductAssignmentsAsync(productId, cancellationToken);
            return Ok(assignments);
        }

        [HttpPost("product-assignments")]
        public async Task<IActionResult> AssignProductDocument([FromBody] CreateProductDocumentAssignmentDto dto, CancellationToken cancellationToken)
        {
            var assignment = await _productAssignmentService.AssignAsync(dto, User.Identity?.Name, cancellationToken);
            return Ok(assignment);
        }

        [HttpDelete("product-assignments/{assignmentId:int}")]
        public async Task<IActionResult> DeleteProductDocumentAssignment([FromRoute] int assignmentId, [FromQuery] string rowVersion, CancellationToken cancellationToken)
        {
            try
            {
                await _productAssignmentService.DeleteAsync(assignmentId, rowVersion, User.Identity?.Name, cancellationToken);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "The product document assignment was modified by another user." });
            }
        }
    }
}
