using System.Net;
using System.Text.RegularExpressions;
using api.Configuration;
using api.Controllers;
using api.Data;
using api.Dtos.Auth;
using api.Interfaces;
using api.Models;
using api.Repository;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace api.Tests;

public class AuthRegistrationTests
{
    [Fact]
    public async Task Register_CreatesUnconfirmedUserWithConfirmationTokenAndEmail()
    {
        await using var db = CreateDbContext();
        db.Roles.Add(new Role { RoleCode = "User", RoleName = "Utilisateur" });
        await db.SaveChangesAsync();
        var (controller, emailService) = CreateController(db);

        var result = await controller.Register(new RegisterRequestDto
        {
            UserName = "  benjamin  ",
            Email = "  BENJAMIN@example.com ",
            PhoneNumber = "06 12 34 56 78",
            Password = "Motdepasse10!",
            FirstName = "Benjamin",
            LastName = "Dupont",
            AcceptPrivacyPolicy = true
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);

        var user = await db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.SecurityTokens)
            .SingleAsync();
        Assert.Equal("benjamin", user.Username);
        Assert.Equal("BENJAMIN", user.NormalizedUsername);
        Assert.Equal("BENJAMIN@example.com", user.Email);
        Assert.Equal("BENJAMIN@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal("0612345678", user.PhoneNumber);
        Assert.False(user.EmailConfirmed);
        Assert.NotEqual("Motdepasse10!", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Motdepasse10!", user.PasswordHash));
        Assert.Single(user.UserRoles);

        var token = Assert.Single(user.SecurityTokens);
        Assert.Equal(UserSecurityTokenTypes.EmailConfirmation, token.TokenType);
        Assert.Null(token.UsedAt);
        Assert.Single(emailService.SentMessages);
        Assert.Contains("confirm-email", emailService.SentMessages[0].HtmlBody);
        Assert.Contains("Ce lien est valable 24 heures.", emailService.SentMessages[0].HtmlBody);
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_ConfirmsUserAndUsesToken()
    {
        await using var db = CreateDbContext();
        var (controller, emailService) = CreateController(db);
        await RegisterAsync(controller, "benjamin@example.com");
        var user = await db.Users.SingleAsync();
        var rawToken = ExtractToken(emailService.SentMessages.Single().HtmlBody);

        var result = await controller.ConfirmEmail(new ConfirmEmailRequestDto
        {
            UserId = user.Id,
            Token = rawToken
        }, CancellationToken.None);

        var ok = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        await db.Entry(user).ReloadAsync();
        var token = await db.UserSecurityTokens.SingleAsync();
        Assert.True(user.EmailConfirmed);
        Assert.NotNull(user.EmailConfirmedAt);
        Assert.NotNull(token.UsedAt);
        Assert.Equal(2, emailService.SentMessages.Count);
        Assert.Contains("Félicitations", emailService.SentMessages[1].Subject);
        Assert.Contains("/my-space", emailService.SentMessages[1].HtmlBody);
    }

    [Fact]
    public async Task ConfirmEmail_WithExpiredToken_ReturnsBadRequest()
    {
        await using var db = CreateDbContext();
        var (controller, emailService) = CreateController(db);
        await RegisterAsync(controller, "benjamin@example.com");
        var user = await db.Users.SingleAsync();
        var token = await db.UserSecurityTokens.SingleAsync();
        token.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        var rawToken = ExtractToken(emailService.SentMessages.Single().HtmlBody);

        var result = await controller.ConfirmEmail(new ConfirmEmailRequestDto
        {
            UserId = user.Id,
            Token = rawToken
        }, CancellationToken.None);

        var badRequest = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        await db.Entry(token).ReloadAsync();
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task Login_RejectsUnconfirmedUser()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Username = "benjamin",
            NormalizedUsername = "BENJAMIN",
            Email = "benjamin@example.com",
            NormalizedEmail = "BENJAMIN@EXAMPLE.COM",
            PhoneNumber = "0612345678",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Motdepasse10!"),
            EmailConfirmed = false
        });
        await db.SaveChangesAsync();
        var (controller, _) = CreateController(db);

        var result = await controller.Login(new LoginRequestDto
        {
            Username = "benjamin",
            Password = "Motdepasse10!"
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
        Assert.Contains(
            AuthenticationAccountService.EmailConfirmationRequiredCode,
            unauthorized.Value?.ToString() ?? "");
    }

    [Fact]
    public async Task ForgotPassword_ForUnknownEmail_ReturnsGenericResponseWithoutEmail()
    {
        await using var db = CreateDbContext();
        var (controller, emailService) = CreateController(db);

        var result = await controller.ForgotPassword(new ForgotPasswordRequestDto
        {
            Email = "missing@example.com"
        }, CancellationToken.None);

        var ok = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_UpdatesPasswordAndUsesToken()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Username = "benjamin",
            NormalizedUsername = "BENJAMIN",
            Email = "benjamin@example.com",
            NormalizedEmail = "BENJAMIN@EXAMPLE.COM",
            PhoneNumber = "0612345678",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Motdepasse10!"),
            EmailConfirmed = true,
            EmailConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var (controller, emailService) = CreateController(db);

        await controller.ForgotPassword(new ForgotPasswordRequestDto
        {
            Email = "benjamin@example.com"
        }, CancellationToken.None);
        var user = await db.Users.SingleAsync();
        var rawToken = ExtractToken(emailService.SentMessages.Single().HtmlBody);

        var result = await controller.ResetPassword(new ResetPasswordRequestDto
        {
            UserId = user.Id,
            Token = rawToken,
            NewPassword = "Nouveaupass10!",
            ConfirmPassword = "Nouveaupass10!"
        }, CancellationToken.None);

        var ok = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        await db.Entry(user).ReloadAsync();
        var token = await db.UserSecurityTokens.SingleAsync();
        Assert.True(BCrypt.Net.BCrypt.Verify("Nouveaupass10!", user.PasswordHash));
        Assert.NotNull(user.PasswordChangedAt);
        Assert.NotNull(token.UsedAt);
    }

    [Fact]
    public async Task Register_ReturnsConflictWhenEmailAlreadyExistsCaseInsensitive()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Username = "existing",
            NormalizedUsername = "EXISTING",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PhoneNumber = "0612345678",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Motdepasse10!"),
            EmailConfirmed = true,
            EmailConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var (controller, _) = CreateController(db);

        var result = await controller.Register(new RegisterRequestDto
        {
            UserName = "other",
            Email = "TEST@example.com",
            PhoneNumber = "+33612345678",
            Password = "Motdepasse10!",
            FirstName = "Other",
            LastName = "User",
            AcceptPrivacyPolicy = true
        }, CancellationToken.None);

        var conflict = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsBadRequestWhenPhoneNumberIsInvalid()
    {
        await using var db = CreateDbContext();
        var (controller, _) = CreateController(db);

        var result = await controller.Register(new RegisterRequestDto
        {
            UserName = "benjamin",
            Email = "benjamin@example.com",
            PhoneNumber = "123",
            Password = "Motdepasse10!",
            FirstName = "Benjamin",
            LastName = "Dupont",
            AcceptPrivacyPolicy = true
        }, CancellationToken.None);

        var badRequest = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    private static async Task RegisterAsync(AuthController controller, string email)
    {
        await controller.Register(new RegisterRequestDto
        {
            UserName = "benjamin",
            Email = email,
            PhoneNumber = "06 12 34 56 78",
            Password = "Motdepasse10!",
            FirstName = "Benjamin",
            LastName = "Dupont",
            AcceptPrivacyPolicy = true
        }, CancellationToken.None);
    }

    private static (AuthController Controller, FakeEmailService EmailService) CreateController(ApplicationDBContext db)
    {
        var userRepository = new UserRepository(db);
        var rolePermissionRepository = new RolePermissionRepository(db);
        var roleRepository = new RoleRepository(db);
        var emailService = new FakeEmailService();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests",
                ["Jwt:DurationInMinutes"] = "120"
            })
            .Build();

        var accountService = new AuthenticationAccountService(
            db,
            emailService,
            Options.Create(new AuthenticationOptions
            {
                FrontendBaseUrl = "http://localhost:5173",
                MinimumEmailResendInterval = TimeSpan.Zero
            }),
            NullLogger<AuthenticationAccountService>.Instance);

        var controller = new AuthController(
            userRepository,
            rolePermissionRepository,
            new AuthService(configuration),
            roleRepository,
            db,
            accountService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return (controller, emailService);
    }

    private static ApplicationDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDBContext(options);
    }

    private static string ExtractToken(string htmlBody)
    {
        var decoded = WebUtility.HtmlDecode(htmlBody);
        var match = Regex.Match(decoded, @"[?&]token=([^&""<\s]+)");

        Assert.True(match.Success, "Le corps de l'e-mail doit contenir un token.");
        return match.Groups[1].Value;
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<SentEmail> SentMessages { get; } = new();

        public Task<bool> SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            SentMessages.Add(new SentEmail(to, subject, htmlBody));
            return Task.FromResult(true);
        }
    }

    private sealed record SentEmail(string To, string Subject, string HtmlBody);
}
