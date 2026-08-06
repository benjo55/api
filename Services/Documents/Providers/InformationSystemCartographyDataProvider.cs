using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using api.Data;
using api.Dtos.Documents;
using api.Interfaces.Documents;
using api.Services.Cmdb;
using api.Services.Documents.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Documents.Providers
{
    public sealed class InformationSystemCartographyDataProvider : IDocumentDataProvider
    {
        private readonly ApplicationDBContext _db;

        public InformationSystemCartographyDataProvider(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<object> BuildModelAsync(
            DocumentDefinition definition,
            GenerateDocumentRequestDto request,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            var employerEntity = request.SubjectId?.Trim();
            if (string.IsNullOrWhiteSpace(employerEntity))
            {
                throw new InvalidOperationException("L'entité employeur CMDB est obligatoire.");
            }

            var configurationItems = (await _db.ConfigurationItems
                    .AsNoTracking()
                    .Include(x => x.ApplicationProfile)
                    .Where(x => x.IsCurrent && !x.IsPlaceholder)
                    .OrderBy(x => x.Name)
                    .ToListAsync(cancellationToken))
                .Where(x => string.Equals(
                    CmdbEmployerResolver.Resolve(x.EntityPath, x.ResponsibleEmployer),
                    employerEntity,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (configurationItems.Count == 0)
            {
                throw new KeyNotFoundException("Aucun CI actif n'est rattaché à cette entité.");
            }

            var applicationIds = configurationItems
                .Where(x => string.Equals(x.Category, "Application Métier", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Id)
                .ToHashSet();

            var flows = applicationIds.Count == 0
                ? []
                : await _db.IntegrationFlows
                    .AsNoTracking()
                    .Where(x => x.Status != "Retired" &&
                        (applicationIds.Contains(x.SourceCiId) || applicationIds.Contains(x.TargetCiId)))
                    .OrderBy(x => x.SourceCi.Name)
                    .ThenBy(x => x.TargetCi.Name)
                    .ThenBy(x => x.Name)
                    .Select(x => new CartographyFlowModel(
                        x.SourceCi.Name,
                        x.TargetCi.Name,
                        x.Name,
                        x.ExchangePattern.Name,
                        x.ExchangePattern.InteractionMode,
                        x.Technology != null ? x.Technology.Name : null))
                    .ToListAsync(cancellationToken);

            var includeDomainSections = ReadBooleanParameter(request, "includeDomainSections", true);
            var domainSections = includeDomainSections
                ? await _db.CartographyDomainDocuments
                    .AsNoTracking()
                    .Where(x => x.EmployerEntity == employerEntity)
                    .SelectMany(x => x.Sections)
                    .OrderBy(x => x.SortOrder)
                    .Select(x => new CartographyDomainSectionModel(
                        x.Title,
                        x.HeadingLevel,
                        x.SortOrder,
                        HtmlToText(x.ContentHtml, x.PlainText)))
                    .ToListAsync(cancellationToken)
                : [];

            return new InformationSystemCartographyDocumentModel(
                employerEntity,
                ReadDateParameter(request, "asOfDate") ?? DateTime.UtcNow.Date,
                ReadStringParameter(request, "classification") ?? "Interne",
                domainSections,
                configurationItems
                    .Where(x => applicationIds.Contains(x.Id))
                    .Select(x => new CartographyApplicationModel(
                        x.Id,
                        x.ExternalCiNumber,
                        x.Name,
                        x.ApplicationDomain,
                        x.OwnerName,
                        x.ApplicationProfile?.ApplicationCriticality,
                        x.ApplicationProfile?.ShortDescription ?? x.Label,
                        x.ApplicationProfile?.HostingMode))
                    .ToList(),
                configurationItems
                    .Select(x => new CartographyConfigurationItemModel(
                        x.Id,
                        x.ExternalCiNumber,
                        x.Name,
                        x.Category,
                        x.Model,
                        x.Status,
                        x.OwnerName ?? x.ResponsibleEmployer))
                    .ToList(),
                flows);
        }

        private static string? ReadStringParameter(GenerateDocumentRequestDto request, string propertyName)
        {
            if (!request.Parameters.HasValue ||
                request.Parameters.Value.ValueKind != System.Text.Json.JsonValueKind.Object ||
                !request.Parameters.Value.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            return value.GetString();
        }

        private static DateTime? ReadDateParameter(GenerateDocumentRequestDto request, string propertyName)
        {
            var value = ReadStringParameter(request, propertyName);
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
                ? parsed.Date
                : null;
        }

        private static bool ReadBooleanParameter(GenerateDocumentRequestDto request, string propertyName, bool fallback)
        {
            if (!request.Parameters.HasValue ||
                request.Parameters.Value.ValueKind != System.Text.Json.JsonValueKind.Object ||
                !request.Parameters.Value.TryGetProperty(propertyName, out var value))
            {
                return fallback;
            }

            return value.ValueKind == System.Text.Json.JsonValueKind.True ||
                (value.ValueKind != System.Text.Json.JsonValueKind.False && fallback);
        }

        private static string HtmlToText(string? html, string? fallback)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return fallback?.Trim() ?? string.Empty;
            }

            var prepared = html.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
            prepared = Regex.Replace(prepared, "</(p|div|h1|h2|h3|li|tr)>", "\n", RegexOptions.IgnoreCase);
            prepared = Regex.Replace(prepared, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
            prepared = Regex.Replace(prepared, "<li[^>]*>", "- ", RegexOptions.IgnoreCase);
            prepared = Regex.Replace(prepared, "<[^>]+>", string.Empty, RegexOptions.IgnoreCase);
            return WebUtility.HtmlDecode(prepared).Trim();
        }
    }
}
