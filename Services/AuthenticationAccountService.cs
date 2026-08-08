using System.Net;
using System.Security.Cryptography;
using System.Text;
using api.Configuration;
using api.Data;
using api.Dtos.Auth;
using api.Interfaces;
using api.Models;
using api.Security;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public sealed class AuthenticationAccountService : IAuthenticationAccountService
    {
        public const string EmailConfirmationRequiredCode = "EMAIL_CONFIRMATION_REQUIRED";
        public const string EmailAlreadyConfirmedCode = "EMAIL_ALREADY_CONFIRMED";
        public const string InvalidOrExpiredTokenCode = "INVALID_OR_EXPIRED_TOKEN";
        public const string PasswordResetSuccessfulCode = "PASSWORD_RESET_SUCCESSFUL";
        public const string PasswordPolicyNotSatisfiedCode = "PASSWORD_POLICY_NOT_SATISFIED";
        public const string TooManyRequestsCode = "TOO_MANY_REQUESTS";

        private const string GenericResendMessage =
            "Si un compte correspondant existe et nécessite une confirmation, un nouvel e-mail vient d'être envoyé.";
        private const string GenericForgotPasswordMessage =
            "Si un compte est associé à cette adresse, un lien de réinitialisation vient d'être envoyé.";
        private static readonly TimeSpan EmailConfirmationLifetime = TimeSpan.FromHours(24);

        private readonly ApplicationDBContext _db;
        private readonly IEmailService _emailService;
        private readonly AuthenticationOptions _options;
        private readonly ILogger<AuthenticationAccountService> _logger;
        private readonly IPublicOriginResolver? _publicOriginResolver;

        public AuthenticationAccountService(
            ApplicationDBContext db,
            IEmailService emailService,
            IOptions<AuthenticationOptions> options,
            ILogger<AuthenticationAccountService> logger,
            IPublicOriginResolver? publicOriginResolver = null)
        {
            _db = db;
            _emailService = emailService;
            _options = options.Value;
            _logger = logger;
            _publicOriginResolver = publicOriginResolver;
        }

        public async Task<RegisterAccountResult> RegisterAsync(
            RegisterRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var firstName = request.FirstName.Trim();
            var lastName = request.LastName.Trim();
            var userName = request.UserName.Trim();
            var email = request.Email.Trim();
            var normalizedPhoneNumber = NormalizePhoneNumber(request.PhoneNumber);

            if (!request.AcceptPrivacyPolicy)
            {
                throw new AuthFunctionalException(
                    "PRIVACY_POLICY_REQUIRED",
                    "Vous devez accepter la politique de confidentialité.",
                    StatusCodes.Status400BadRequest,
                    "acceptPrivacyPolicy");
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new AuthFunctionalException(
                    "FIRST_NAME_REQUIRED",
                    "Le prénom est obligatoire.",
                    StatusCodes.Status400BadRequest,
                    "firstName");
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                throw new AuthFunctionalException(
                    "LAST_NAME_REQUIRED",
                    "Le nom est obligatoire.",
                    StatusCodes.Status400BadRequest,
                    "lastName");
            }

            if (!IsValidPhoneNumber(normalizedPhoneNumber))
            {
                throw new AuthFunctionalException(
                    "INVALID_PHONE_NUMBER",
                    "Le numéro de téléphone n'est pas valide.",
                    StatusCodes.Status400BadRequest,
                    "phoneNumber");
            }

            if (!IsPasswordValid(request.Password))
            {
                throw new AuthFunctionalException(
                    PasswordPolicyNotSatisfiedCode,
                    $"Le mot de passe doit contenir au moins {_options.PasswordMinLength} caractères.",
                    StatusCodes.Status400BadRequest,
                    "password");
            }

            if (await _db.Users.AnyAsync(u => u.NormalizedUsername == Normalize(userName), cancellationToken))
            {
                throw new AuthFunctionalException(
                    "USERNAME_ALREADY_EXISTS",
                    "Ce nom d'utilisateur est déjà utilisé.",
                    StatusCodes.Status409Conflict,
                    "userName");
            }

            if (await _db.Users.AnyAsync(u => u.NormalizedEmail == Normalize(email), cancellationToken))
            {
                throw new AuthFunctionalException(
                    "EMAIL_ALREADY_EXISTS",
                    "Cette adresse e-mail est déjà utilisée.",
                    StatusCodes.Status409Conflict,
                    "email");
            }

            var matchingPersons = await FindUnlinkedPersonsByEmailAsync(email, cancellationToken);
            if (matchingPersons.Count > 1)
            {
                throw new AuthFunctionalException(
                    "PERSON_EMAIL_AMBIGUOUS",
                    "Plusieurs fiches personne correspondent à cette adresse e-mail. Un administrateur doit effectuer le rattachement.",
                    StatusCodes.Status409Conflict,
                    "email");
            }

            var defaultRole = await ResolveRegistrationRoleAsync(request, cancellationToken);

            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(cancellationToken)
                : null;

            var now = DateTime.UtcNow;
            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Username = userName,
                NormalizedUsername = Normalize(userName),
                Email = email,
                NormalizedEmail = Normalize(email),
                PhoneNumber = normalizedPhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedDate = now,
                PrivacyPolicyAcceptedAt = now,
                EmailConfirmed = false,
                Status = UserStatus.PendingEmailConfirmation,
                LastEmailConfirmationSentAt = now,
                UserRoles = defaultRole == null
                    ? new List<UserRole>()
                    : new List<UserRole> { new UserRole { RoleId = defaultRole.Id } }
            };

            _db.Users.Add(user);

            var linkedPerson = matchingPersons.SingleOrDefault();
            if (linkedPerson == null)
            {
                linkedPerson = new Person
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email1 = email,
                    PhoneNumber = normalizedPhoneNumber,
                    Role = "Client",
                    Status = "Prospect",
                    BirthDate = DateTime.UtcNow.Date,
                    CreatedDate = now,
                    UpdatedDate = now,
                    User = user
                };
                _db.Persons.Add(linkedPerson);
            }
            else
            {
                linkedPerson.User = user;
                linkedPerson.UpdatedDate = now;
                if (string.IsNullOrWhiteSpace(linkedPerson.FirstName)) linkedPerson.FirstName = firstName;
                if (string.IsNullOrWhiteSpace(linkedPerson.LastName)) linkedPerson.LastName = lastName;
                if (string.IsNullOrWhiteSpace(linkedPerson.PhoneNumber)) linkedPerson.PhoneNumber = normalizedPhoneNumber;
                if (string.IsNullOrWhiteSpace(linkedPerson.Email1)) linkedPerson.Email1 = email;
            }

            await _db.SaveChangesAsync(cancellationToken);

            var rawToken = await CreateTokenAsync(
                user,
                UserSecurityTokenTypes.EmailConfirmation,
                EmailConfirmationLifetime,
                ipAddress,
                cancellationToken);
            if (transaction != null) await transaction.CommitAsync(cancellationToken);

            var confirmationSent = await SendConfirmationEmailAsync(user, rawToken, cancellationToken);

            _logger.LogInformation(
                "Inscription enregistrée pour UserId={UserId}, confirmationEmailSent={ConfirmationEmailSent}",
                user.Id,
                confirmationSent);

            var registrationMessage = confirmationSent
                ? "Votre compte a bien été créé. Un e-mail de confirmation vient de vous être envoyé."
                : "Votre compte a bien été créé. L'envoi de l'e-mail de confirmation n'est pas disponible pour le moment.";

            return new RegisterAccountResult(
                user.Id,
                user.Username,
                user.FirstName,
                user.LastName,
                user.Email,
                MaskEmail(user.Email),
                registrationMessage);
        }

        private async Task<Role?> ResolveRegistrationRoleAsync(
            RegisterRequestDto request,
            CancellationToken cancellationToken)
        {
            var experience = ResolveRegistrationExperience(request.SiteExperience);
            var roleCode = experience switch
            {
                SiteExperience.Urbanization => SystemRoles.Cartography,
                SiteExperience.Donation => SystemRoles.Donor,
                _ => SystemRoles.LegacyUser
            };

            return await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == roleCode, cancellationToken)
                ?? await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == SystemRoles.LegacyUser, cancellationToken);
        }

        private SiteExperience ResolveRegistrationExperience(SiteExperience? requestedExperience)
        {
            if (requestedExperience.HasValue)
            {
                return requestedExperience.Value;
            }

            try
            {
                return _publicOriginResolver?.ResolveCurrent().Experience
                    ?? SiteExperience.Insurance;
            }
            catch
            {
                return SiteExperience.Insurance;
            }
        }

        private async Task<List<Person>> FindUnlinkedPersonsByEmailAsync(
            string email,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = Normalize(email);
            return await _db.Persons
                .Where(p => p.UserId == null
                    && ((p.Email1 != null && p.Email1.Trim().ToUpper() == normalizedEmail)
                        || (p.Email2 != null && p.Email2.Trim().ToUpper() == normalizedEmail)))
                .Take(2)
                .ToListAsync(cancellationToken);
        }

        public async Task<AuthActionResult> ConfirmEmailAsync(
            ConfirmEmailRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null)
            {
                return InvalidToken();
            }

            if (user.EmailConfirmed)
            {
                return AlreadyConfirmed();
            }

            var token = await FindValidTokenAsync(
                user.Id,
                UserSecurityTokenTypes.EmailConfirmation,
                request.Token,
                cancellationToken);

            if (token == null)
            {
                _logger.LogWarning("Tentative de confirmation invalide pour UserId={UserId}", user.Id);
                return InvalidToken();
            }

            var now = DateTime.UtcNow;
            token.UsedAt = now;
            user.EmailConfirmed = true;
            user.EmailConfirmedAt = now;
            if (user.Status == UserStatus.PendingEmailConfirmation)
            {
                user.Status = UserStatus.Active;
            }
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _db.ChangeTracker.Clear();
                var currentUser = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

                if (currentUser?.EmailConfirmed == true)
                {
                    _logger.LogInformation("Adresse e-mail déjà confirmée pour UserId={UserId}", user.Id);
                    return AlreadyConfirmed();
                }

                _logger.LogWarning("Conflit de confirmation e-mail pour UserId={UserId}", user.Id);
                return InvalidToken();
            }

            _logger.LogInformation("Adresse e-mail confirmée pour UserId={UserId}", user.Id);
            await SendWelcomeEmailAsync(user, cancellationToken);
            return new AuthActionResult(
                "EMAIL_CONFIRMED",
                "Votre adresse e-mail est maintenant confirmée. Vous pouvez vous connecter à votre compte.");
        }

        public async Task<AuthActionResult> ResendConfirmationEmailAsync(
            ResendConfirmationEmailRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == Normalize(request.Email), cancellationToken);

            if (user == null || user.EmailConfirmed)
            {
                _logger.LogInformation("Demande de renvoi de confirmation traitée génériquement.");
                return new AuthActionResult("CONFIRMATION_EMAIL_RESEND_ACCEPTED", GenericResendMessage, EmailDelivered: false);
            }

            if (user.LastEmailConfirmationSentAt is { } lastSent
                && DateTime.UtcNow - lastSent < _options.MinimumEmailResendInterval)
            {
                _logger.LogWarning("Renvoi de confirmation trop rapproché pour UserId={UserId}", user.Id);
                return new AuthActionResult(
                    TooManyRequestsCode,
                    "Une demande a déjà été prise en compte récemment. Réessayez dans quelques minutes.",
                    StatusCodes.Status429TooManyRequests);
            }

            var rawToken = await CreateTokenAsync(
                user,
                UserSecurityTokenTypes.EmailConfirmation,
                EmailConfirmationLifetime,
                ipAddress,
                cancellationToken);
            user.LastEmailConfirmationSentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            var confirmationSent = await SendConfirmationEmailAsync(user, rawToken, cancellationToken);

            if (confirmationSent)
            {
                _logger.LogInformation("E-mail de confirmation renvoyé pour UserId={UserId}", user.Id);
                return new AuthActionResult(
                    "CONFIRMATION_EMAIL_RESEND_ACCEPTED",
                    "Un nouvel e-mail de confirmation a été envoyé.",
                    EmailDelivered: true);
            }

            _logger.LogWarning("E-mail de confirmation non envoyé (SMTP/Mailjet indisponible ou mal configuré) pour UserId={UserId}", user.Id);
            return new AuthActionResult(
                "CONFIRMATION_EMAIL_RESEND_FAILED",
                "Le message de confirmation n’a pas pu être envoyé. Vérifiez la configuration SMTP/Mailjet et les journaux serveur.",
                EmailDelivered: false);
        }

        public async Task<AuthActionResult> RequestPasswordResetAsync(
            ForgotPasswordRequestDto request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == Normalize(request.Email), cancellationToken);

            if (user == null)
            {
                _logger.LogInformation("Demande de mot de passe oublié traitée génériquement.");
                return new AuthActionResult("PASSWORD_RESET_REQUEST_ACCEPTED", GenericForgotPasswordMessage);
            }

            var rawToken = await CreateTokenAsync(
                user,
                UserSecurityTokenTypes.PasswordReset,
                _options.PasswordResetTokenLifetime,
                ipAddress,
                cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            var passwordResetEmailSent = await SendPasswordResetEmailAsync(user, rawToken, cancellationToken);

            _logger.LogInformation(
                "Demande de mot de passe oublié enregistrée pour UserId={UserId}, passwordResetEmailSent={PasswordResetEmailSent}",
                user.Id,
                passwordResetEmailSent);
            return new AuthActionResult("PASSWORD_RESET_REQUEST_ACCEPTED", GenericForgotPasswordMessage);
        }

        public async Task<AuthActionResult> ResetPasswordAsync(
            ResetPasswordRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                return new AuthActionResult(
                    PasswordPolicyNotSatisfiedCode,
                    "Les deux mots de passe ne correspondent pas.",
                    StatusCodes.Status400BadRequest,
                    "confirmPassword");
            }

            if (!IsPasswordValid(request.NewPassword))
            {
                return new AuthActionResult(
                    PasswordPolicyNotSatisfiedCode,
                    $"Le mot de passe doit contenir au moins {_options.PasswordMinLength} caractères.",
                    StatusCodes.Status400BadRequest,
                    "newPassword");
            }

            var user = await _db.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null)
            {
                return InvalidToken();
            }

            var token = await FindValidTokenAsync(
                user.Id,
                UserSecurityTokenTypes.PasswordReset,
                request.Token,
                cancellationToken);

            if (token == null)
            {
                _logger.LogWarning("Tentative de reset invalide pour UserId={UserId}", user.Id);
                return InvalidToken();
            }

            var now = DateTime.UtcNow;
            token.UsedAt = now;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordChangedAt = now;
            user.MustChangePassword = false;
            await RevokeTokensAsync(user.Id, UserSecurityTokenTypes.PasswordReset, cancellationToken);
            token.UsedAt = now;
            token.RevokedAt = null;
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Mot de passe réinitialisé pour UserId={UserId}", user.Id);
            return new AuthActionResult(
                PasswordResetSuccessfulCode,
                "Votre mot de passe a été réinitialisé. Vous pouvez maintenant vous connecter avec votre nouveau mot de passe.");
        }

        public static string Normalize(string value) => value.Trim().ToUpperInvariant();

        public static string NormalizePhoneNumber(string phoneNumber)
        {
            var trimmed = phoneNumber.Trim();
            var normalized = new string(trimmed.Where(c => char.IsDigit(c) || c == '+').ToArray());

            return normalized.StartsWith("00", StringComparison.Ordinal)
                ? "+" + normalized[2..]
                : normalized;
        }

        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (phoneNumber.StartsWith("+", StringComparison.Ordinal))
            {
                return phoneNumber.Length is >= 9 and <= 16
                    && phoneNumber.Skip(1).All(char.IsDigit);
            }

            return phoneNumber.Length == 10
                && phoneNumber.StartsWith("0", StringComparison.Ordinal)
                && phoneNumber.All(char.IsDigit);
        }

        private bool IsPasswordValid(string password) =>
            !string.IsNullOrWhiteSpace(password)
            && password.Length >= _options.PasswordMinLength;

        private async Task<string> CreateTokenAsync(
            User user,
            string tokenType,
            TimeSpan lifetime,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            await RevokeTokensAsync(user.Id, tokenType, cancellationToken);

            var rawToken = GenerateToken();
            _db.UserSecurityTokens.Add(new UserSecurityToken
            {
                UserId = user.Id,
                TokenType = tokenType,
                TokenHash = HashToken(rawToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(lifetime),
                CreatedByIpAddress = ipAddress
            });

            await _db.SaveChangesAsync(cancellationToken);
            return rawToken;
        }

        private async Task RevokeTokensAsync(
            int userId,
            string tokenType,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var tokens = await _db.UserSecurityTokens
                .Where(t =>
                    t.UserId == userId
                    && t.TokenType == tokenType
                    && t.UsedAt == null
                    && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.RevokedAt = now;
            }
        }

        private async Task<UserSecurityToken?> FindValidTokenAsync(
            int userId,
            string tokenType,
            string rawToken,
            CancellationToken cancellationToken)
        {
            var tokenHash = HashToken(rawToken);
            var now = DateTime.UtcNow;
            var token = await _db.UserSecurityTokens
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId
                    && t.TokenType == tokenType
                    && t.TokenHash == tokenHash
                    && t.UsedAt == null
                    && t.RevokedAt == null,
                    cancellationToken);

            if (token == null)
            {
                return null;
            }

            if (token.ExpiresAt < now)
            {
                token.RevokedAt = now;
                await _db.SaveChangesAsync(cancellationToken);
                return null;
            }

            return token;
        }

        private async Task<bool> SendConfirmationEmailAsync(
            User user,
            string rawToken,
            CancellationToken cancellationToken)
        {
            var link = BuildLink("confirm-email", user.Id, rawToken);
            var html = BuildActionEmail(
                "Confirmer votre adresse e-mail",
                "Votre compte Life a été créé.",
                "Confirmer mon adresse e-mail",
                link,
                $"Ce lien est valable {EmailConfirmationLifetime.TotalHours:0} heures.");

            return await _emailService.SendEmailAsync(
                user.Email,
                "Confirmez votre adresse e-mail Life",
                html,
                cancellationToken);
        }

        private async Task SendWelcomeEmailAsync(User user, CancellationToken cancellationToken)
        {
            var profileLink = $"{ResolveFrontendOrigin()}/my-space";
            var html = BuildWelcomeEmail(user, profileLink);
            var sent = await _emailService.SendEmailAsync(
                user.Email,
                "Félicitations, bienvenue dans Life",
                html,
                cancellationToken);

            if (!sent)
            {
                _logger.LogWarning("E-mail de bienvenue non envoyé pour UserId={UserId}", user.Id);
            }
        }

        private async Task<bool> SendPasswordResetEmailAsync(
            User user,
            string rawToken,
            CancellationToken cancellationToken)
        {
            var link = BuildLink("reset-password", user.Id, rawToken);
            var html = BuildActionEmail(
                "Réinitialiser votre mot de passe",
                "Une demande de réinitialisation de mot de passe Life a été reçue.",
                "Réinitialiser mon mot de passe",
                link,
                $"Ce lien est valable {_options.PasswordResetTokenLifetime.TotalMinutes:0} minute(s) et ne fonctionne qu'une fois.");

            return await _emailService.SendEmailAsync(
                user.Email,
                "Réinitialisation de votre mot de passe Life",
                html,
                cancellationToken);
        }

        private string BuildLink(string path, int userId, string rawToken)
        {
            var baseUrl = ResolveFrontendOrigin();
            return QueryHelpers.AddQueryString(
                $"{baseUrl}/{path}",
                new Dictionary<string, string?>
                {
                    ["userId"] = userId.ToString(),
                    ["token"] = rawToken
                });
        }

        private string ResolveFrontendOrigin()
        {
            try
            {
                return _publicOriginResolver?.ResolveCurrent().Origin
                    ?? _options.FrontendBaseUrl.TrimEnd('/');
            }
            catch
            {
                return _options.FrontendBaseUrl.TrimEnd('/');
            }
        }

        private static string BuildActionEmail(
            string title,
            string explanation,
            string buttonText,
            string link,
            string lifetimeText)
        {
            var encodedLink = WebUtility.HtmlEncode(link);

            return $"""
                <!doctype html>
                <html lang="fr">
                <body style="margin:0;font-family:Arial,sans-serif;background:#f6f8fb;color:#102033;">
                  <div style="max-width:560px;margin:0 auto;padding:28px 16px;">
                    <div style="background:#ffffff;border:1px solid #d8e2ee;border-radius:12px;padding:28px;">
                      <h1 style="margin:0 0 12px;font-size:24px;">Life</h1>
                      <h2 style="margin:0 0 16px;font-size:20px;">{WebUtility.HtmlEncode(title)}</h2>
                      <p>{WebUtility.HtmlEncode(explanation)}</p>
                      <p>
                        <a href="{encodedLink}" style="display:inline-block;background:#0ea5e9;color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:700;">
                          {WebUtility.HtmlEncode(buttonText)}
                        </a>
                      </p>
                      <p>{WebUtility.HtmlEncode(lifetimeText)}</p>
                      <p>Si le bouton ne fonctionne pas, copiez ce lien dans votre navigateur :</p>
                      <p style="word-break:break-all;"><a href="{encodedLink}">{encodedLink}</a></p>
                      <p style="color:#5f6f82;">Si vous n'êtes pas à l'origine de cette demande, ignorez cet e-mail.</p>
                    </div>
                  </div>
                </body>
                </html>
                """;
        }

        private static string BuildWelcomeEmail(User user, string profileLink)
        {
            var encodedLink = WebUtility.HtmlEncode(profileLink);
            var displayName = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = user.Username;
            }

            return $"""
                <!doctype html>
                <html lang="fr">
                <body style="margin:0;font-family:Arial,sans-serif;background:#f6f8fb;color:#102033;">
                  <div style="max-width:620px;margin:0 auto;padding:28px 16px;">
                    <div style="background:#ffffff;border:1px solid #d8e2ee;border-radius:12px;padding:30px;">
                      <p style="margin:0 0 8px;color:#2563eb;font-size:13px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;">Life</p>
                      <h1 style="margin:0 0 16px;font-size:26px;line-height:1.25;">Félicitations, votre compte est confirmé.</h1>
                      <p style="font-size:16px;line-height:1.55;">Bonjour {WebUtility.HtmlEncode(displayName)},</p>
                      <p style="font-size:16px;line-height:1.55;">
                        Votre adresse e-mail est maintenant vérifiée. Vous êtes connecté à l'écosystème Life et votre espace personnel est prêt.
                      </p>
                      <p style="font-size:16px;line-height:1.55;">
                        Vous pouvez compléter votre profil donateur, suivre vos dons et retrouver progressivement vos documents fiscaux depuis un seul poste de pilotage.
                      </p>
                      <p style="margin:24px 0;">
                        <a href="{encodedLink}" style="display:inline-block;background:#0ea5e9;color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:700;">
                          Accéder à mon espace
                        </a>
                      </p>
                      <p style="color:#5f6f82;font-size:14px;line-height:1.55;">
                        Si vous n'êtes pas à l'origine de cette confirmation, contactez l'équipe Life.
                      </p>
                    </div>
                  </div>
                </body>
                </html>
                """;
        }

        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        private static string HashToken(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }

        private static string MaskEmail(string email)
        {
            var parts = email.Split('@', 2);
            if (parts.Length != 2 || parts[0].Length == 0)
            {
                return "***";
            }

            return $"{parts[0][0]}***@{parts[1]}";
        }

        private static AuthActionResult InvalidToken() =>
            new(
                InvalidOrExpiredTokenCode,
                "Le lien est invalide ou a expiré.",
                StatusCodes.Status400BadRequest);

        private static AuthActionResult AlreadyConfirmed() =>
            new(
                EmailAlreadyConfirmedCode,
                "Cette adresse e-mail est déjà confirmée.");
    }

    public sealed class AuthFunctionalException : Exception
    {
        public AuthFunctionalException(
            string code,
            string message,
            int statusCode,
            string? field = null)
            : base(message)
        {
            Code = code;
            StatusCode = statusCode;
            Field = field;
        }

        public string Code { get; }

        public int StatusCode { get; }

        public string? Field { get; }
    }
}
