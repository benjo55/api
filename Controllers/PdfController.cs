using api.Dtos.Pdf;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/pdf")]
    [ApiController]
    public sealed class PdfController : ControllerBase
    {
        private readonly IPdfDocumentService _pdfDocumentService;
        private readonly IPdfBusinessDocumentService _pdfBusinessDocumentService;

        public PdfController(IPdfDocumentService pdfDocumentService, IPdfBusinessDocumentService pdfBusinessDocumentService)
        {
            _pdfDocumentService = pdfDocumentService;
            _pdfBusinessDocumentService = pdfBusinessDocumentService;
        }

        [HttpPost("generate")]
        [Obsolete("Use /api/documents/{documentType}/generate instead.")]
        public async Task<IActionResult> Generate([FromBody] GeneratePdfRequestDto request, CancellationToken cancellationToken)
        {
            AddDeprecatedEndpointHeader();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var result = await _pdfDocumentService.GenerateAsync(request, cancellationToken);
            return File(result.Content, "application/pdf", $"{result.FileName}.pdf");
        }

        [HttpPost("merge")]
        [Obsolete("Use /api/documents/{documentType}/generate instead.")]
        public async Task<IActionResult> Merge([FromBody] MergePdfRequestDto request, CancellationToken cancellationToken)
        {
            AddDeprecatedEndpointHeader();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (request.Documents.Count == 0)
            {
                return BadRequest("At least one document must be provided.");
            }

            var result = await _pdfDocumentService.MergeAsync(request, cancellationToken);
            return File(result.Content, "application/pdf", $"{result.FileName}.pdf");
        }

        [HttpPost("contract-sheet")]
        [Obsolete("Use /api/documents/contract-sheet/generate instead.")]
        public async Task<IActionResult> GenerateContractSheet([FromBody] GenerateContractSheetRequestDto request, CancellationToken cancellationToken)
        {
            AddDeprecatedEndpointHeader();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var result = await _pdfBusinessDocumentService.GenerateContractSheetAsync(request, cancellationToken);
                return File(result.Content, "application/pdf", $"{result.FileName}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("client-case-file")]
        [Obsolete("Use /api/documents/client-case-file/generate instead.")]
        public async Task<IActionResult> GenerateClientCaseFile([FromBody] GenerateClientCaseFileRequestDto request, CancellationToken cancellationToken)
        {
            AddDeprecatedEndpointHeader();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var result = await _pdfBusinessDocumentService.GenerateClientCaseFileAsync(request, cancellationToken);
                return File(result.Content, "application/pdf", $"{result.FileName}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private void AddDeprecatedEndpointHeader()
        {
            Response.Headers["Warning"] = "299 - \"Deprecated PDF endpoint; use /api/documents/{documentType}/generate\"";
        }
    }
}
