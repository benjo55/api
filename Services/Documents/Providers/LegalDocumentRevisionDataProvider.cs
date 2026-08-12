using System.Text.Json;
using api.Dtos.Documents;
using api.Exceptions;
using api.Interfaces;
using api.Interfaces.Documents;
using api.Services.Documents.Models;

namespace api.Services.Documents.Providers
{
    public sealed class LegalDocumentRevisionDataProvider : IDocumentDataProvider
    {
        private readonly IDocumentRenderService _renderService;

        public LegalDocumentRevisionDataProvider(IDocumentRenderService renderService)
        {
            _renderService = renderService;
        }

        public async Task<object> BuildModelAsync(
            DocumentDefinition definition,
            GenerateDocumentRequestDto request,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            var revisionId = ReadRevisionId(request)
                ?? throw new BusinessException("LegalDocumentRevisionIdRequired");
            var revisionStamp = ReadParameterString(request, "revisionStamp");
            var renderModel = await _renderService.BuildRenderModelAsync(revisionId, cancellationToken);

            return new LegalDocumentRevisionDocumentModel(renderModel, revisionStamp);
        }

        private static int? ReadRevisionId(GenerateDocumentRequestDto request)
        {
            if (int.TryParse(request.SubjectId, out var subjectRevisionId))
            {
                return subjectRevisionId;
            }

            if (!request.Parameters.HasValue ||
                request.Parameters.Value.ValueKind != JsonValueKind.Object ||
                !request.Parameters.Value.TryGetProperty("revisionId", out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numericRevisionId))
            {
                return numericRevisionId;
            }

            return value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), out var stringRevisionId)
                    ? stringRevisionId
                    : null;
        }

        private static string? ReadParameterString(GenerateDocumentRequestDto request, string propertyName)
        {
            if (!request.Parameters.HasValue ||
                request.Parameters.Value.ValueKind != JsonValueKind.Object ||
                !request.Parameters.Value.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }
    }
}
