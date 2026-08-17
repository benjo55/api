using System.Security.Claims;
using api.Extensions;
using api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace api.Tests;

public sealed class SiteAccessPolicyTests
{
    [Fact]
    public async Task CerfaUser_CannotAccessLifeEndpoint()
    {
        var authorized = await AuthorizeAsync(SystemRoles.CerfaUser, AuthorizationPolicies.LifeAccess);

        Assert.False(authorized);
    }

    [Fact]
    public async Task LifeUser_CannotAccessCerfaEndpoint()
    {
        var authorized = await AuthorizeAsync(SystemRoles.LifeUser, AuthorizationPolicies.CerfaAccess);

        Assert.False(authorized);
    }

    [Fact]
    public async Task UrbanisationUser_CannotAccessCerfaEndpoint()
    {
        var authorized = await AuthorizeAsync(SystemRoles.UrbanisationUser, AuthorizationPolicies.CerfaAccess);

        Assert.False(authorized);
    }

    [Fact]
    public async Task Admin_CanAccessAllSites()
    {
        Assert.True(await AuthorizeAsync(SystemRoles.LegacyAdmin, AuthorizationPolicies.LifeAccess));
        Assert.True(await AuthorizeAsync(SystemRoles.LegacyAdmin, AuthorizationPolicies.CerfaAccess));
        Assert.True(await AuthorizeAsync(SystemRoles.LegacyAdmin, AuthorizationPolicies.UrbanisationAccess));
    }

    private static async Task<bool> AuthorizeAsync(string role, string policyName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiAuthentication(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests"
            })
            .Build());

        await using var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var policy = await policyProvider.GetPolicyAsync(policyName)
            ?? throw new InvalidOperationException($"Policy {policyName} introuvable.");
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("role", role)],
            authenticationType: "test",
            nameType: "username",
            roleType: "role"));

        var result = await authorizationService.AuthorizeAsync(user, resource: null, policy);
        return result.Succeeded;
    }
}
