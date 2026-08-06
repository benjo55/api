using api.Configuration;
using api.Interfaces;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public sealed class PublicOriginResolver : IPublicOriginResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PublicOriginOptions _options;

        public PublicOriginResolver(
            IHttpContextAccessor httpContextAccessor,
            IOptions<PublicOriginOptions> options)
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
        }

        public ResolvedPublicOrigin ResolveCurrent()
        {
            var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
            return Resolve(host);
        }

        public ResolvedPublicOrigin Resolve(string? host)
        {
            var normalizedHost = NormalizeHost(host);
            foreach (var entry in _options.Experiences)
            {
                if (entry.Value.Domains.Any(domain =>
                    string.Equals(NormalizeHost(domain), normalizedHost, StringComparison.OrdinalIgnoreCase)))
                {
                    return new ResolvedPublicOrigin(
                        entry.Key,
                        NormalizeOrigin(entry.Value.Origin),
                        normalizedHost,
                        true,
                        _options.UnknownHostPolicy);
                }
            }

            var defaultExperience = _options.DefaultExperience;
            return new ResolvedPublicOrigin(
                defaultExperience,
                GetOrigin(defaultExperience),
                normalizedHost,
                false,
                _options.UnknownHostPolicy);
        }

        public string GetOrigin(SiteExperience experience)
        {
            if (!_options.Experiences.TryGetValue(experience, out var experienceOptions)
                || string.IsNullOrWhiteSpace(experienceOptions.Origin))
            {
                throw new InvalidOperationException($"PublicOrigins:Experiences:{experience}:Origin est obligatoire.");
            }

            return NormalizeOrigin(experienceOptions.Origin);
        }

        private static string NormalizeHost(string? value)
        {
            var host = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                host = new Uri(host).Host;
            }

            var slashIndex = host.IndexOf('/');
            if (slashIndex >= 0)
            {
                host = host[..slashIndex];
            }

            var colonIndex = host.IndexOf(':');
            if (colonIndex > 0)
            {
                host = host[..colonIndex];
            }

            return host.TrimEnd('.');
        }

        private static string NormalizeOrigin(string value) => value.Trim().TrimEnd('/');
    }
}

