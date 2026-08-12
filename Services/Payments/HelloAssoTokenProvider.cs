using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Configuration;
using api.Interfaces;
using Microsoft.Extensions.Options;

namespace api.Services.Payments
{
    public sealed class HelloAssoTokenProvider : IHelloAssoTokenProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HelloAssoOptions _options;
        private readonly ILogger<HelloAssoTokenProvider> _logger;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        private readonly Dictionary<string, TokenCacheEntry> _tokens = new(StringComparer.OrdinalIgnoreCase);

        public HelloAssoTokenProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<HelloAssoOptions> options,
            ILogger<HelloAssoTokenProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken, string? credentialKey = null)
        {
            var cacheKey = NormalizeCredentialKey(credentialKey);
            if (_tokens.TryGetValue(cacheKey, out var cached)
                && !string.IsNullOrWhiteSpace(cached.AccessToken)
                && DateTimeOffset.UtcNow < cached.ExpiresAtUtc)
            {
                return cached.AccessToken;
            }

            return await RefreshAccessTokenAsync(cancellationToken, credentialKey);
        }

        public async Task<string> RefreshAccessTokenAsync(CancellationToken cancellationToken, string? credentialKey = null)
        {
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                var cacheKey = NormalizeCredentialKey(credentialKey);
                if (_tokens.TryGetValue(cacheKey, out var cached)
                    && !string.IsNullOrWhiteSpace(cached.AccessToken)
                    && DateTimeOffset.UtcNow < cached.ExpiresAtUtc)
                {
                    return cached.AccessToken;
                }

                var client = _httpClientFactory.CreateClient("helloasso-auth");
                var credential = ResolveCredentials(credentialKey);
                var maxRetries = Math.Max(1, _options.RetryCount);
                for (var attempt = 1; attempt <= maxRetries; attempt++)
                {
                    _tokens.TryGetValue(cacheKey, out var tokenCache);
                    var formValues = !string.IsNullOrWhiteSpace(tokenCache?.RefreshToken)
                        ? new Dictionary<string, string>
                        {
                            ["grant_type"] = "refresh_token",
                            ["refresh_token"] = tokenCache.RefreshToken!,
                            ["client_id"] = credential.ClientId,
                            ["client_secret"] = credential.ClientSecret,
                        }
                        : new Dictionary<string, string>
                        {
                            ["grant_type"] = "client_credentials",
                            ["client_id"] = credential.ClientId,
                            ["client_secret"] = credential.ClientSecret,
                        };

                    var tokenPath = string.IsNullOrWhiteSpace(credential.TokenBaseUrl)
                        ? "/oauth2/token"
                        : $"{credential.TokenBaseUrl.TrimEnd('/')}/oauth2/token";

                    using var request = new HttpRequestMessage(HttpMethod.Post, tokenPath)
                    {
                        Content = new FormUrlEncodedContent(formValues)
                    };

                    using var response = await client.SendAsync(request, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                        var token = await JsonSerializer.DeserializeAsync<HelloAssoTokenResponse>(stream, cancellationToken: cancellationToken)
                            ?? throw new InvalidOperationException("Token OAuth HelloAsso invalide.");

                        if (string.IsNullOrWhiteSpace(token.AccessToken))
                        {
                            throw new InvalidOperationException("Access token HelloAsso manquant.");
                        }

                        var safeExpiresIn = Math.Max(30, token.ExpiresIn - 60);
                        _tokens[cacheKey] = new TokenCacheEntry(
                            token.AccessToken,
                            string.IsNullOrWhiteSpace(token.RefreshToken)
                                ? tokenCache?.RefreshToken
                                : token.RefreshToken,
                            DateTimeOffset.UtcNow.AddSeconds(safeExpiresIn));
                        return token.AccessToken;
                    }

                    if (!string.IsNullOrWhiteSpace(tokenCache?.RefreshToken) && response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
                    {
                        _tokens.Remove(cacheKey);
                        continue;
                    }

                    if (!HttpRetryHelper.IsTransient(response.StatusCode) || attempt == maxRetries)
                    {
                        throw new InvalidOperationException($"Echec OAuth HelloAsso (HTTP {(int)response.StatusCode}).");
                    }

                    await HttpRetryHelper.DelayWithBackoffAsync(attempt, response.Headers.RetryAfter, cancellationToken);
                }

                throw new InvalidOperationException("Echec OAuth HelloAsso.");
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private HelloAssoCredentialOptions ResolveCredentials(string? credentialKey)
        {
            var normalizedCredentialKey = NormalizeCredentialKey(credentialKey);
            if (normalizedCredentialKey != "__global__"
                && _options.Credentials.TryGetValue(normalizedCredentialKey, out var credential)
                && credential.HasCredentials)
            {
                return new HelloAssoCredentialOptions
                {
                    ClientId = credential.ClientId,
                    ClientSecret = credential.ClientSecret,
                    Environment = credential.Environment ?? _options.Environment,
                    TokenBaseUrl = credential.TokenBaseUrl ?? _options.TokenBaseUrl,
                    ApiBaseUrl = credential.ApiBaseUrl ?? _options.ApiBaseUrl,
                };
            }

            if (_options.HasGlobalCredentials)
            {
                return new HelloAssoCredentialOptions
                {
                    ClientId = _options.ClientId,
                    ClientSecret = _options.ClientSecret,
                    Environment = _options.Environment,
                    TokenBaseUrl = _options.TokenBaseUrl,
                    ApiBaseUrl = _options.ApiBaseUrl,
                };
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(credentialKey)
                    ? "Credentials HelloAsso globaux absents."
                    : $"Credentials HelloAsso absents pour l'alias '{credentialKey}'.");
        }

        private static string NormalizeCredentialKey(string? credentialKey) =>
            string.IsNullOrWhiteSpace(credentialKey) ? "__global__" : credentialKey.Trim();

        private sealed record TokenCacheEntry(
            string AccessToken,
            string? RefreshToken,
            DateTimeOffset ExpiresAtUtc);

        private sealed class HelloAssoTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }
        }
    }
}
