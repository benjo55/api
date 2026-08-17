using api.Configuration;
using api.Models;

namespace api.Interfaces
{
    public interface ISiteBrandingProvider
    {
        SiteBranding Get(UserOrigin origin);

        SiteBranding Get(SiteExperience experience);
    }

    public sealed class SiteBranding
    {
        public UserOrigin Site { get; init; }

        public string DisplayName { get; init; } = string.Empty;

        public string BaseUrl { get; init; } = string.Empty;

        public string LogoUrl { get; init; } = string.Empty;

        public string EmailFromName { get; init; } = string.Empty;

        public string SupportEmail { get; init; } = string.Empty;

        public string AccentColor { get; init; } = "#0ea5e9";
    }
}
