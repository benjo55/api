using System.ComponentModel.DataAnnotations;

namespace api.Configuration
{
    public sealed class HelloAssoOptions
    {
        public const string SectionName = "Payments:HelloAsso";

        public bool Enabled { get; set; } = false;

        [Required]
        public string Environment { get; set; } = "Sandbox";

        [Required]
        [Url]
        public string BaseUrl { get; set; } = "https://api.helloasso-sandbox.com";

        [Required]
        [Url]
        public string TokenBaseUrl { get; set; } = "https://api.helloasso-sandbox.com";

        [Required]
        [Url]
        public string ApiBaseUrl { get; set; } = "https://api.helloasso-sandbox.com";

        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        public Dictionary<string, HelloAssoCredentialOptions> Credentials { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public string OrganizationSlug { get; set; } = string.Empty;

        public string? WebhookSignatureKey { get; set; }
        public List<string> AllowedWebhookIpAddresses { get; set; } = new();

        [Required]
        public string ItemName { get; set; } = "Don a l'association";

        [Required]
        [Url]
        public string ReturnUrl { get; set; } = "http://localhost:5173/faire-un-don/retour";

        [Required]
        [Url]
        public string BackUrl { get; set; } = "http://localhost:5173/faire-un-don";

        [Required]
        [Url]
        public string ErrorUrl { get; set; } = "http://localhost:5173/faire-un-don/erreur";

        [Range(1, 120)]
        public int HttpTimeoutSeconds { get; set; } = 20;

        [Range(1, 10)]
        public int RetryCount { get; set; } = 3;

        public bool HasGlobalCredentials =>
            !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret);

        public bool HasCredential(string? credentialKey) =>
            !string.IsNullOrWhiteSpace(credentialKey)
            && Credentials.TryGetValue(credentialKey, out var credential)
            && credential.HasCredentials;

        public bool HasAnyCredentials =>
            HasGlobalCredentials || Credentials.Values.Any(x => x.HasCredentials);
    }

    public sealed class HelloAssoCredentialOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string? Environment { get; set; }
        public string? TokenBaseUrl { get; set; }
        public string? ApiBaseUrl { get; set; }

        public bool HasCredentials =>
            !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret);
    }
}
