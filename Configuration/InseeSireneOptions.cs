using System.ComponentModel.DataAnnotations;

namespace api.Configuration
{
    public sealed class InseeSireneOptions
    {
        public const string SectionName = "Insee";

        public string ApiKey { get; init; } = "";

        [Required]
        public string BaseUrl { get; init; } = "https://api.insee.fr/api-sirene/3.11/";

        [Required]
        public string GeoBaseUrl { get; init; } = "https://api.insee.fr/metadonnees/V1/geo/";

        [Range(1, 60)]
        public int TimeoutSeconds { get; init; } = 10;

        [Range(1, 1440)]
        public int CacheDurationMinutes { get; init; } = 720;

        [Range(1, 25)]
        public int MaxSearchResults { get; init; } = 10;
    }
}
