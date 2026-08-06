using api.Configuration;

namespace api.Interfaces
{
    public interface IPublicOriginResolver
    {
        ResolvedPublicOrigin ResolveCurrent();

        ResolvedPublicOrigin Resolve(string? host);

        string GetOrigin(SiteExperience experience);
    }

    public sealed record ResolvedPublicOrigin(
        SiteExperience Experience,
        string Origin,
        string Host,
        bool IsKnownHost,
        UnknownHostPolicy UnknownHostPolicy);
}

