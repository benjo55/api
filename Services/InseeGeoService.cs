using System.Net;
using System.Text.Json;
using api.Configuration;
using api.Dtos.Insee;
using api.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public sealed class InseeGeoService : IInseeGeoService
    {
        private const string ApiKeyHeader = "X-INSEE-Api-Key-Integration";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly InseeSireneOptions _options;
        private readonly ILogger<InseeGeoService> _logger;

        public InseeGeoService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IOptions<InseeSireneOptions> options,
            ILogger<InseeGeoService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<InseeCommuneDto>> SearchCommunesAsync(
            string search,
            int limit,
            CancellationToken cancellationToken)
        {
            var normalizedSearch = string.Join(
                " ",
                search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            if (normalizedSearch.Length < 2)
            {
                return [];
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException(
                    "La clé API INSEE est absente. Configurez Insee:ApiKey dans les User Secrets ou l'environnement.");
            }

            var safeLimit = Math.Clamp(limit, 1, 100);
            var cacheKey = $"insee-geo:communes:{normalizedSearch}:{safeLimit}";
            if (_cache.TryGetValue<IReadOnlyCollection<InseeCommuneDto>>(cacheKey, out var cached)
                && cached is not null)
            {
                return cached;
            }

            var client = _httpClientFactory.CreateClient("insee-geo");
            var requestPath = $"communes?filtreNom={Uri.EscapeDataString(normalizedSearch)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestPath);
            request.Headers.TryAddWithoutValidation(ApiKeyHeader, _options.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException("La clé API INSEE est refusée par le portail géographique.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("INSEE geo communes search failed with HTTP {StatusCode}", (int)response.StatusCode);
                throw new InvalidOperationException($"Recherche commune INSEE indisponible (HTTP {(int)response.StatusCode}).");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var source = json.RootElement.ValueKind == JsonValueKind.Array
                ? json.RootElement.EnumerateArray()
                : json.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array
                    ? items.EnumerateArray()
                    : [];

            var communes = source
                .Select(MapCommune)
                .Where(commune => !string.IsNullOrWhiteSpace(commune.Code) || !string.IsNullOrWhiteSpace(commune.Intitule))
                .OrderBy(commune => commune.Intitule)
                .Take(safeLimit)
                .ToList();

            _cache.Set(
                cacheKey,
                communes,
                TimeSpan.FromMinutes(Math.Max(1, _options.CacheDurationMinutes)));

            return communes;
        }

        private static InseeCommuneDto MapCommune(JsonElement item) => new()
        {
            Code = ReadString(item, "code") ?? "",
            Uri = ReadString(item, "uri") ?? "",
            Type = ReadString(item, "type") ?? "",
            DateCreation = ReadString(item, "dateCreation") ?? "",
            IntituleSansArticle = ReadString(item, "intituleSansArticle") ?? "",
            TypeArticle = ReadString(item, "typeArticle") ?? "",
            Intitule = ReadString(item, "intitule") ?? ReadString(item, "intituleSansArticle") ?? "",
        };

        private static string? ReadString(JsonElement source, string propertyName)
        {
            if (source.ValueKind != JsonValueKind.Object
                || !source.TryGetProperty(propertyName, out var value)
                || value.ValueKind == JsonValueKind.Null
                || value.ValueKind == JsonValueKind.Undefined)
            {
                return null;
            }

            return value.ValueKind == JsonValueKind.String
                ? EmptyToNull(value.GetString())
                : EmptyToNull(value.ToString());
        }

        private static string? EmptyToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
