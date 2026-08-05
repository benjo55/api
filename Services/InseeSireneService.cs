using System.Globalization;
using System.Net;
using System.Text.Json;
using api.Configuration;
using api.Dtos.Insurer;
using api.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public sealed class InseeSireneService : IInseeSireneService
    {
        private const string ApiKeyHeader = "X-INSEE-Api-Key-Integration";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly InseeSireneOptions _options;
        private readonly ILogger<InseeSireneService> _logger;

        public InseeSireneService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IOptions<InseeSireneOptions> options,
            ILogger<InseeSireneService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<InsurerSireneSearchDto>> SearchInsurersAsync(
            string search,
            int limit,
            CancellationToken cancellationToken)
        {
            var normalizedSearch = NormalizeSearch(search);
            if (normalizedSearch.Length < 3)
            {
                return [];
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException(
                    "La clé API INSEE est absente. Configurez Insee:ApiKey dans les User Secrets ou l'environnement.");
            }

            var safeLimit = Math.Clamp(limit, 1, _options.MaxSearchResults);
            var cacheKey = $"insee-sirene:insurer-search:{normalizedSearch}:{safeLimit}";
            if (_cache.TryGetValue<IReadOnlyCollection<InsurerSireneSearchDto>>(cacheKey, out var cached)
                && cached is not null)
            {
                return cached;
            }

            var query = BuildQuery(normalizedSearch);
            var requestPath = $"siret?q={Uri.EscapeDataString(query)}&nombre={safeLimit}&masquerValeursNulles=true";
            var results = await FetchEtablissementsAsync(requestPath, cancellationToken);

            _cache.Set(
                cacheKey,
                results,
                TimeSpan.FromMinutes(Math.Max(1, _options.CacheDurationMinutes)));

            return results;
        }

        private async Task<IReadOnlyCollection<InsurerSireneSearchDto>> FetchEtablissementsAsync(
            string requestPath,
            CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("insee-sirene");
            using var request = new HttpRequestMessage(HttpMethod.Get, requestPath);
            request.Headers.TryAddWithoutValidation(ApiKeyHeader, _options.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException("La clé API INSEE est refusée par le portail Sirene.");
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new InvalidOperationException("Limite d'appels INSEE atteinte. Réessayez dans quelques instants.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sirene search failed with HTTP {StatusCode}", (int)response.StatusCode);
                throw new InvalidOperationException($"Recherche INSEE indisponible (HTTP {(int)response.StatusCode}).");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!json.RootElement.TryGetProperty("etablissements", out var etablissements)
                || etablissements.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return etablissements
                .EnumerateArray()
                .Select(MapEtablissement)
                .Where(item => !string.IsNullOrWhiteSpace(item.Siren) && !string.IsNullOrWhiteSpace(item.LegalName))
                .GroupBy(item => item.Siren, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        private static string BuildQuery(string search)
        {
            var digits = new string(search.Where(char.IsDigit).ToArray());
            if (digits.Length == 14)
            {
                return $"siret:{digits}";
            }

            if (digits.Length == 9)
            {
                return $"sirenUniteLegale:{digits} AND etablissementSiege:true";
            }

            var escaped = EscapeLuceneValue(search);
            var denominationQuery = search.Contains(' ', StringComparison.Ordinal)
                ? $"denominationUniteLegale:\"{escaped}\"~2"
                : $"denominationUniteLegale:{escaped}*";

            return $"{denominationQuery} AND etablissementSiege:true";
        }

        private static InsurerSireneSearchDto MapEtablissement(JsonElement etablissement)
        {
            var uniteLegale = etablissement.TryGetProperty("uniteLegale", out var unit)
                ? unit
                : default;
            var unitPeriod = CurrentPeriod(uniteLegale, "periodesUniteLegale");
            var establishmentPeriod = CurrentPeriod(etablissement, "periodesEtablissement");
            var siren = ReadString(etablissement, "siren") ?? ReadString(uniteLegale, "siren") ?? "";
            var siret = ReadString(etablissement, "siret");
            var legalName = ReadString(unitPeriod, "denominationUniteLegale")
                ?? ReadString(uniteLegale, "denominationUniteLegale")
                ?? ReadString(etablissement, "denominationUniteLegale")
                ?? "";
            var tradeName = ReadString(establishmentPeriod, "denominationUsuelleEtablissement")
                ?? ReadString(establishmentPeriod, "enseigne1Etablissement")
                ?? ReadString(uniteLegale, "sigleUniteLegale");
            var active = string.Equals(
                ReadString(unitPeriod, "etatAdministratifUniteLegale")
                ?? ReadString(uniteLegale, "etatAdministratifUniteLegale"),
                "A",
                StringComparison.OrdinalIgnoreCase);
            var now = DateTimeOffset.UtcNow;
            var address = BuildAddress(etablissement);
            var latitude = ReadString(etablissement, "latitude");
            var longitude = ReadString(etablissement, "longitude");

            return new InsurerSireneSearchDto
            {
                Siren = siren,
                HeadquartersSiret = siret,
                LegalName = legalName,
                TradeName = tradeName,
                Acronym = ReadString(uniteLegale, "sigleUniteLegale"),
                LegalForm = ReadString(unitPeriod, "categorieJuridiqueUniteLegale")
                    ?? ReadString(uniteLegale, "categorieJuridiqueUniteLegale"),
                IncorporationDate = ReadString(uniteLegale, "dateCreationUniteLegale"),
                ApeNafCode = ReadString(unitPeriod, "activitePrincipaleUniteLegale")
                    ?? ReadString(establishmentPeriod, "activitePrincipaleEtablissement"),
                HeadquartersAddress = address,
                HeadQuarters = BuildAddressGeoJson(address, latitude, longitude, ReadString(etablissement, "codeCommuneEtablissement")),
                PostalCode = ReadString(etablissement, "codePostalEtablissement"),
                City = ReadString(etablissement, "libelleCommuneEtablissement"),
                Latitude = latitude,
                Longitude = longitude,
                VatNumber = BuildFrenchVatNumber(siren),
                IsActive = active ? "En activité" : "Inactif",
                SourceUrl = $"https://annuaire-entreprises.data.gouv.fr/entreprise/{siren}",
                SourceReference = siren,
                RetrievedAt = now.ToString("O", CultureInfo.InvariantCulture),
                LastVerifiedAt = now.ToString("O", CultureInfo.InvariantCulture),
                DataQualityNotes = "Identité juridique récupérée depuis l'API Sirene INSEE. Le statut réglementaire ACPR/EIOPA reste à vérifier."
            };
        }

        private static JsonElement CurrentPeriod(JsonElement source, string propertyName)
        {
            if (source.ValueKind != JsonValueKind.Object
                || !source.TryGetProperty(propertyName, out var periods)
                || periods.ValueKind != JsonValueKind.Array)
            {
                return default;
            }

            foreach (var period in periods.EnumerateArray())
            {
                if (!period.TryGetProperty("dateFin", out var dateFin)
                    || dateFin.ValueKind == JsonValueKind.Null
                    || string.IsNullOrWhiteSpace(dateFin.GetString()))
                {
                    return period;
                }
            }

            return periods.EnumerateArray().FirstOrDefault();
        }

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

        private static string? BuildAddress(JsonElement etablissement)
        {
            var parts = new[]
            {
                ReadString(etablissement, "numeroVoieEtablissement"),
                ReadString(etablissement, "indiceRepetitionEtablissement"),
                ReadString(etablissement, "typeVoieEtablissement"),
                ReadString(etablissement, "libelleVoieEtablissement"),
                ReadString(etablissement, "complementAdresseEtablissement"),
                ReadString(etablissement, "codePostalEtablissement"),
                ReadString(etablissement, "libelleCommuneEtablissement")
            };

            return EmptyToNull(string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part))));
        }

        private static string? BuildAddressGeoJson(string? address, string? latitude, string? longitude, string? cityCode)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            var lat = 0d;
            var lon = 0d;
            var hasCoordinates = double.TryParse(latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out lat)
                && double.TryParse(longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out lon);

            var feature = new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Point",
                    coordinates = hasCoordinates ? new[] { lon, lat } : Array.Empty<double>()
                },
                properties = new
                {
                    label = address,
                    citycode = cityCode
                }
            };

            return JsonSerializer.Serialize(feature, JsonOptions);
        }

        private static string? BuildFrenchVatNumber(string siren)
        {
            if (siren.Length != 9 || !long.TryParse(siren, out var sirenNumber))
            {
                return null;
            }

            var key = (12 + 3 * (sirenNumber % 97)) % 97;
            return $"FR{key:00}{siren}";
        }

        private static string NormalizeSearch(string value) =>
            string.Join(" ", (value ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        private static string EscapeLuceneValue(string value) =>
            value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal)
                .Replace(":", "\\:", StringComparison.Ordinal);

        private static string? EmptyToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
