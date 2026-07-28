using api.Controllers;
using api.Data;
using api.Dtos.Cmdb;
using api.Models.Cmdb;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace api.Tests;

public sealed class CartographyLayoutTests
{
    [Fact]
    public async Task Layout_IsPersistedPerUserAndCanBeReset()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase($"cartography-layout-{Guid.NewGuid()}")
            .Options;
        await using var db = new ApplicationDBContext(options);
        db.ConfigurationItems.Add(new ConfigurationItem
        {
            Id = 42,
            ExternalCiNumber = "CMDB42",
            Name = "SUNSHINE",
            Model = "Application",
            Category = "Application Métier",
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, "benjo");
        var saved = await controller.Save(new CartographyLayoutDto
        {
            ScopeType = "EmployerEntity",
            ScopeKey = "APICIL EPARGNE",
            Nodes =
            [
                new CartographyNodePositionDto
                {
                    ConfigurationItemId = 42,
                    X = 125.5,
                    Y = 480.25,
                },
            ],
        });

        var savedResult = Assert.IsType<OkObjectResult>(saved.Result);
        var savedLayout = Assert.IsType<CartographyLayoutDto>(savedResult.Value);
        var position = Assert.Single(savedLayout.Nodes);
        Assert.Equal(125.5, position.X);
        Assert.Equal(480.25, position.Y);

        var otherUser = CreateController(db, "other-user");
        var otherResult = await otherUser.Get(
            "EmployerEntity",
            "APICIL EPARGNE");
        var otherOk = Assert.IsType<OkObjectResult>(otherResult.Result);
        Assert.Empty(Assert.IsType<CartographyLayoutDto>(otherOk.Value).Nodes);

        var reset = await controller.Reset(
            "EmployerEntity",
            "APICIL EPARGNE");
        Assert.IsType<NoContentResult>(reset);

        var afterReset = await controller.Get(
            "EmployerEntity",
            "APICIL EPARGNE");
        var afterResetOk = Assert.IsType<OkObjectResult>(afterReset.Result);
        Assert.Empty(
            Assert.IsType<CartographyLayoutDto>(afterResetOk.Value).Nodes);
    }

    private static CartographyLayoutsController CreateController(
        ApplicationDBContext db,
        string userName)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, userName)],
            "test");
        return new CartographyLayoutsController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity),
                },
            },
        };
    }
}
