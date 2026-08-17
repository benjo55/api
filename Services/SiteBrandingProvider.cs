using api.Configuration;
using api.Interfaces;
using api.Models;

namespace api.Services
{
    public sealed class SiteBrandingProvider : ISiteBrandingProvider
    {
        private readonly IPublicOriginResolver _originResolver;

        public SiteBrandingProvider(IPublicOriginResolver originResolver)
        {
            _originResolver = originResolver;
        }

        public SiteBranding Get(UserOrigin origin) =>
            origin switch
            {
                UserOrigin.Cerfa => Build(
                    UserOrigin.Cerfa,
                    SiteExperience.Donation,
                    "CERFA",
                    "#2563eb",
                    "support@cerfa.top"),
                UserOrigin.Urbanisation => Build(
                    UserOrigin.Urbanisation,
                    SiteExperience.Urbanization,
                    "Urbanisation",
                    "#0891b2",
                    "support@urbanisation.world"),
                _ => Build(
                    UserOrigin.Life,
                    SiteExperience.Insurance,
                    "Financial Life",
                    "#0ea5e9",
                    "support@financial-life.fr")
            };

        public SiteBranding Get(SiteExperience experience) =>
            Get(ToUserOrigin(experience));

        private SiteBranding Build(
            UserOrigin origin,
            SiteExperience experience,
            string displayName,
            string accentColor,
            string supportEmail)
        {
            var baseUrl = _originResolver.GetOrigin(experience);
            return new SiteBranding
            {
                Site = origin,
                DisplayName = displayName,
                BaseUrl = baseUrl,
                LogoUrl = $"{baseUrl}/favicon.svg",
                EmailFromName = displayName,
                SupportEmail = supportEmail,
                AccentColor = accentColor
            };
        }

        public static UserOrigin ToUserOrigin(SiteExperience experience) =>
            experience switch
            {
                SiteExperience.Donation => UserOrigin.Cerfa,
                SiteExperience.Urbanization => UserOrigin.Urbanisation,
                _ => UserOrigin.Life
            };
    }
}
