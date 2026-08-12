using System.Globalization;
using System.Text.Json;
using api.Dtos.Documents;
using api.Exceptions;
using api.Interfaces.Documents;
using api.Services.Documents.Models;

namespace api.Services.Documents.Providers
{
    public sealed class BoostSimulationDocumentDataProvider : IDocumentDataProvider
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public Task<object> BuildModelAsync(
            DocumentDefinition definition,
            GenerateDocumentRequestDto request,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            if (!request.Parameters.HasValue || request.Parameters.Value.ValueKind != JsonValueKind.Object)
            {
                throw new BusinessException("BoostSimulationParametersRequired");
            }

            var root = request.Parameters.Value;
            var collecte = ReadRequired<BoostCollecteModel>(root, "collecte", "BoostCollecteRequired");
            var operations = ReadRequired<List<BoostOperationModel>>(root, "operations", "BoostOperationsRequired");
            var fileName = ReadString(root, "fileName")
                ?? $"simulation-boost-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            return Task.FromResult<object>(new BoostSimulationDocumentModel(
                collecte,
                operations.OrderBy(operation => operation.DateOperation).ToList(),
                fileName));
        }

        private static T ReadRequired<T>(JsonElement root, string propertyName, string errorCode)
        {
            if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                throw new BusinessException(errorCode);
            }

            return value.Deserialize<T>(SerializerOptions) ?? throw new BusinessException(errorCode);
        }

        private static string? ReadString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            var text = value.GetString();
            return string.IsNullOrWhiteSpace(text)
                ? null
                : text.Trim().ToLower(CultureInfo.InvariantCulture);
        }
    }
}
