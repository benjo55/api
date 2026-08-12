using System.Security.Claims;
using System.Text.RegularExpressions;
using api.Dtos.Documents;
using api.Exceptions;
using api.Interfaces.Documents;

namespace api.Services.Documents.Core
{
    public sealed class DocumentGenerationService : IDocumentGenerationService
    {
        private static readonly Regex FileNameUnsafeChars = new(@"[^\p{L}\p{N}_\-\.]+", RegexOptions.Compiled);
        private readonly IDocumentDefinitionRegistry _registry;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DocumentGenerationService> _logger;

        public DocumentGenerationService(
            IDocumentDefinitionRegistry registry,
            IServiceProvider serviceProvider,
            ILogger<DocumentGenerationService> logger)
        {
            _registry = registry;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<GeneratedDocumentResult> GenerateAsync(
            string documentType,
            GenerateDocumentRequestDto request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var definition = _registry.Find(documentType)
                ?? throw new KeyNotFoundException("DocumentTypeNotFound");

            EnsureDeliveryModeAllowed(definition, request.DeliveryMode);
            EnsurePermission(definition, user);

            var generatedAt = DateTimeOffset.UtcNow;
            var context = new DocumentGenerationContext(
                user,
                TryReadUserId(user),
                user.Identity?.Name ?? user.FindFirst("username")?.Value,
                ReadParameterString(request, "locale") ?? "fr-FR",
                ReadParameterString(request, "timeZone") ?? "Europe/Paris",
                generatedAt,
                request.DeliveryMode,
                Guid.NewGuid().ToString("N"),
                ReadParameterString(request, "asOfDate"));

            _logger.LogInformation(
                "Document generation started. CorrelationId={CorrelationId}, DocumentType={DocumentType}, SubjectId={SubjectId}, DeliveryMode={DeliveryMode}, RenderEngine={RenderEngine}, User={User}",
                context.CorrelationId,
                definition.Key,
                request.SubjectId,
                request.DeliveryMode,
                definition.RenderEngine,
                context.UserName ?? "anonymous");

            var provider = (IDocumentDataProvider)_serviceProvider.GetRequiredService(definition.DataProviderType);
            var renderer = (IDocumentRenderer)_serviceProvider.GetRequiredService(definition.RendererType);

            var model = await provider.BuildModelAsync(definition, request, context, cancellationToken);
            var rendered = await renderer.RenderAsync(model, definition, context, cancellationToken);
            var fileName = SanitizeFileName(rendered.FileName ?? BuildFileName(definition, request, context));
            var hash = await ComputeHashAsync(rendered.Content, cancellationToken);
            var metadata = BuildMetadata(definition, context, rendered.Metadata);

            _logger.LogInformation(
                "Document generation completed. CorrelationId={CorrelationId}, DocumentType={DocumentType}, RenderEngine={RenderEngine}, FileName={FileName}, ContentLength={ContentLength}, Hash={Hash}",
                context.CorrelationId,
                definition.Key,
                definition.RenderEngine,
                fileName,
                rendered.Content.CanSeek ? rendered.Content.Length : null,
                hash);

            return new GeneratedDocumentResult(
                rendered.Content,
                rendered.ContentType,
                fileName,
                rendered.Content.CanSeek ? rendered.Content.Length : null,
                definition.Key,
                definition.TemplateVersion,
                generatedAt,
                hash,
                metadata);
        }

        private static void EnsureDeliveryModeAllowed(DocumentDefinition definition, DocumentDeliveryMode deliveryMode)
        {
            var allowed = deliveryMode switch
            {
                DocumentDeliveryMode.Preview => definition.SupportsPreview,
                DocumentDeliveryMode.Download => definition.SupportsDownload,
                DocumentDeliveryMode.Archive => definition.SupportsArchive,
                DocumentDeliveryMode.Email => definition.SupportsEmail,
                _ => false
            };

            if (!allowed)
            {
                throw new BusinessException("DocumentDeliveryModeNotSupported");
            }
        }

        private static void EnsurePermission(DocumentDefinition definition, ClaimsPrincipal user)
        {
            if (string.IsNullOrWhiteSpace(definition.RequiredPermission))
            {
                return;
            }

            if (!user.HasClaim("permission", definition.RequiredPermission))
            {
                throw new UnauthorizedAccessException("DocumentForbidden");
            }
        }

        private static int? TryReadUserId(ClaimsPrincipal user)
        {
            var raw = user.FindFirst("userId")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var userId) ? userId : null;
        }

        private static string BuildFileName(
            DocumentDefinition definition,
            GenerateDocumentRequestDto request,
            DocumentGenerationContext context)
        {
            var subject = string.IsNullOrWhiteSpace(request.SubjectId) ? "global" : request.SubjectId.Trim();
            var date = context.GeneratedAt.ToString("yyyyMMdd");
            return definition.DefaultFileNamePattern
                .Replace("{subjectId}", subject, StringComparison.OrdinalIgnoreCase)
                .Replace("{date}", date, StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyDictionary<string, string> BuildMetadata(
            DocumentDefinition definition,
            DocumentGenerationContext context,
            IReadOnlyDictionary<string, string> renderedMetadata)
        {
            var options = definition.EffectiveRenderOptions;
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["documentType"] = definition.Key,
                ["displayName"] = definition.DisplayName,
                ["templateVersion"] = definition.TemplateVersion,
                ["renderEngine"] = definition.RenderEngine.ToString(),
                ["pageSize"] = options.PageSize,
                ["orientation"] = options.Orientation,
                ["deliveryMode"] = context.DeliveryMode.ToString(),
                ["correlationId"] = context.CorrelationId,
                ["generatedAt"] = context.GeneratedAt.ToString("O")
            };

            foreach (var item in renderedMetadata)
            {
                metadata[item.Key] = item.Value;
            }

            return metadata;
        }

        private static string SanitizeFileName(string fileName)
        {
            var normalized = FileNameUnsafeChars.Replace(fileName.Trim(), "_").Trim('_');
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = "document.pdf";
            }

            return normalized.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? normalized
                : $"{normalized}.pdf";
        }

        private static string? ReadParameterString(GenerateDocumentRequestDto request, string propertyName)
        {
            if (!request.Parameters.HasValue || request.Parameters.Value.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return null;
            }

            return request.Parameters.Value.TryGetProperty(propertyName, out var value)
                ? value.GetString()
                : null;
        }

        private static async Task<string> ComputeHashAsync(Stream content, CancellationToken cancellationToken)
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            var hash = await System.Security.Cryptography.SHA256.HashDataAsync(content, cancellationToken);

            if (content.CanSeek)
            {
                content.Position = 0;
            }

            return Convert.ToHexString(hash);
        }
    }
}
