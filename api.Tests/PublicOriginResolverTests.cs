using api.Configuration;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace api.Tests;

public sealed class PublicOriginResolverTests
{
    [Theory]
    [InlineData("cerfa.top", SiteExperience.Donation, "https://cerfa.top", true)]
    [InlineData("www.cerfa.top", SiteExperience.Donation, "https://cerfa.top", true)]
    [InlineData("euroboost.top", SiteExperience.Insurance, "https://euroboost.top", true)]
    [InlineData("api.euroboost.top", SiteExperience.Insurance, "https://euroboost.top", true)]
    [InlineData("urbanisation.world", SiteExperience.Urbanization, "https://urbanisation.world", true)]
    [InlineData("www.urbanisation.world", SiteExperience.Urbanization, "https://urbanisation.world", true)]
    [InlineData("unknown.example", SiteExperience.Insurance, "https://euroboost.top", false)]
    public void Resolve_maps_known_domains_and_marks_unknown_hosts(
        string host,
        SiteExperience expectedExperience,
        string expectedOrigin,
        bool expectedKnownHost)
    {
        var resolver = CreateResolver();

        var resolved = resolver.Resolve(host);

        Assert.Equal(expectedExperience, resolved.Experience);
        Assert.Equal(expectedOrigin, resolved.Origin);
        Assert.Equal(expectedKnownHost, resolved.IsKnownHost);
    }

    [Fact]
    public void ResolveCurrent_uses_request_host()
    {
        var contextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Host = new HostString("cerfa.top")
                }
            }
        };
        var resolver = new PublicOriginResolver(contextAccessor, Options.Create(CreateOptions()));

        var resolved = resolver.ResolveCurrent();

        Assert.Equal(SiteExperience.Donation, resolved.Experience);
        Assert.Equal("https://cerfa.top", resolved.Origin);
    }

    private static PublicOriginResolver CreateResolver() =>
        new(new HttpContextAccessor(), Options.Create(CreateOptions()));

    private static PublicOriginOptions CreateOptions() =>
        new()
        {
            DefaultExperience = SiteExperience.Insurance,
            UnknownHostPolicy = UnknownHostPolicy.Reject,
            Experiences = new()
            {
                [SiteExperience.Insurance] = new PublicOriginExperienceOptions
                {
                    Origin = "https://euroboost.top",
                    Domains = ["euroboost.top", "www.euroboost.top", "api.euroboost.top"]
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
            }
        };
}
