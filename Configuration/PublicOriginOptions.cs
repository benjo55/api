namespace api.Configuration
{
    public sealed class PublicOriginOptions
    {
        public const string SectionName = "PublicOrigins";

        public SiteExperience DefaultExperience { get; set; } = SiteExperience.Insurance;

        public UnknownHostPolicy UnknownHostPolicy { get; set; } = UnknownHostPolicy.Reject;

        public Dictionary<SiteExperience, PublicOriginExperienceOptions> Experiences { get; set; } = new()
        {
            [SiteExperience.Insurance] = new PublicOriginExperienceOptions
            {
                Origin = "http://localhost:5173",
                Domains = ["localhost", "127.0.0.1"]
            },
            [SiteExperience.Donation] = new PublicOriginExperienceOptions
            {
                Origin = "https://cerfa.top",
                Domains = ["cerfa.top", "www.cerfa.top"]
            },
            [SiteExperience.Urbanization] = new PublicOriginExperienceOptions
            {
                Origin = "https://urbanisation.world",
                Domains = ["urbanisation.world", "www.urbanisation.world"]
            }
        };
    }

    public sealed class PublicOriginExperienceOptions
    {
        public string Origin { get; set; } = string.Empty;

        public string[] Domains { get; set; } = [];
    }

    public enum UnknownHostPolicy
    {
        Reject,
        UseDefaultExperience
    }
}
