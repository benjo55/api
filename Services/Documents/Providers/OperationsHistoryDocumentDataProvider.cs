using System.Globalization;
using System.Text.Json;
using api.Dtos.Documents;
using api.Dtos.Pdf;
using api.Exceptions;
using api.Interfaces.Documents;
using api.Services.Documents.Models;

namespace api.Services.Documents.Providers
{
    public sealed class OperationsHistoryDocumentDataProvider : IDocumentDataProvider
    {
        public Task<object> BuildModelAsync(
            DocumentDefinition definition,
            GenerateDocumentRequestDto request,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            var contractId = ReadContractId(request);
            var documentRequest = new GenerateOperationsHistoryRequestDto
            {
                ContractId = contractId,
                FileName = ReadStringParameter(request, "fileName")
                    ?? $"historique-operations-{contractId.ToString(CultureInfo.InvariantCulture)}",
                LogoBase64 = ReadStringParameter(request, "logoBase64"),
                LogoUrl = ReadStringParameter(request, "logoUrl")
            };

            return Task.FromResult<object>(new OperationsHistoryDocumentModel(documentRequest));
        }

        private static int ReadContractId(GenerateDocumentRequestDto request)
        {
            if (int.TryParse(request.SubjectId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var subjectId)
                && subjectId > 0)
            {
                return subjectId;
            }

            if (TryReadProperty(request, "contractId", out var value) &&
                value.TryGetInt32(out var contractId) &&
                contractId > 0)
            {
                return contractId;
            }

            throw new BusinessException("ContractIdRequired");
        }

        private static string? ReadStringParameter(GenerateDocumentRequestDto request, string propertyName)
        {
            if (!TryReadProperty(request, propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return value.GetString();
        }

        private static bool TryReadProperty(
            GenerateDocumentRequestDto request,
            string propertyName,
            out JsonElement value)
        {
            if (!request.Parameters.HasValue ||
                request.Parameters.Value.ValueKind != JsonValueKind.Object)
            {
                value = default;
                return false;
            }

            return request.Parameters.Value.TryGetProperty(propertyName, out value);
        }
    }
}
