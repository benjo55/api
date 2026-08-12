using System.Globalization;
using System.Text.Json;
using api.Dtos.Documents;
using api.Dtos.Pdf;
using api.Exceptions;
using api.Interfaces.Documents;
using api.Services.Documents.Models;

namespace api.Services.Documents.Providers
{
    public sealed class ClientCaseFileDocumentDataProvider : IDocumentDataProvider
    {
        public Task<object> BuildModelAsync(
            DocumentDefinition definition,
            GenerateDocumentRequestDto request,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            var contractId = ReadContractId(request);
            var documentRequest = new GenerateClientCaseFileRequestDto
            {
                ContractId = contractId,
                FileName = ReadStringParameter(request, "fileName")
                    ?? $"dossier-client-{contractId.ToString(CultureInfo.InvariantCulture)}",
                IncludeContractSheet = ReadBooleanParameter(request, "includeContractSheet", true),
                IncludeSituationStatement = ReadBooleanParameter(request, "includeSituationStatement", true),
                IncludeOperationsHistory = ReadBooleanParameter(request, "includeOperationsHistory", true),
                IncludeAssetAllocationReport = ReadBooleanParameter(request, "includeAssetAllocationReport", true),
                LogoBase64 = ReadStringParameter(request, "logoBase64"),
                LogoUrl = ReadStringParameter(request, "logoUrl"),
                QrCodeContent = ReadStringParameter(request, "qrCodeContent"),
                AdditionalDocuments = ReadAdditionalDocuments(request)
            };

            return Task.FromResult<object>(new ClientCaseFileDocumentModel(documentRequest));
        }

        private static int ReadContractId(GenerateDocumentRequestDto request)
        {
            if (int.TryParse(request.SubjectId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var subjectId)
                && subjectId > 0)
            {
                return subjectId;
            }

            var parameterId = ReadIntParameter(request, "contractId");
            if (parameterId is > 0)
            {
                return parameterId.Value;
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

        private static int? ReadIntParameter(GenerateDocumentRequestDto request, string propertyName)
        {
            if (!TryReadProperty(request, propertyName, out var value))
            {
                return null;
            }

            return value.TryGetInt32(out var parsed) ? parsed : null;
        }

        private static bool ReadBooleanParameter(GenerateDocumentRequestDto request, string propertyName, bool fallback)
        {
            if (!TryReadProperty(request, propertyName, out var value))
            {
                return fallback;
            }

            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => fallback
            };
        }

        private static List<MergePdfPartDto> ReadAdditionalDocuments(GenerateDocumentRequestDto request)
        {
            if (!TryReadProperty(request, "additionalDocuments", out var value) ||
                value.ValueKind != JsonValueKind.Array)
            {
                return new List<MergePdfPartDto>();
            }

            var documents = new List<MergePdfPartDto>();
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("base64Content", out var content) ||
                    content.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(content.GetString()))
                {
                    continue;
                }

                documents.Add(new MergePdfPartDto
                {
                    FileName = item.TryGetProperty("fileName", out var fileName)
                        ? fileName.GetString()
                        : null,
                    Base64Content = content.GetString() ?? string.Empty
                });
            }

            return documents;
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
