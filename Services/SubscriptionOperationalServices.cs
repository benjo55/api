using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using api.Configuration;
using api.Data;
using api.Dtos.Subscription;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QRCoder;

namespace api.Services
{
    public sealed class SubscriptionMfaService : ISubscriptionMfaService
    {
        private readonly ApplicationDBContext _db;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SubscriptionOperationsOptions _options;

        public SubscriptionMfaService(
            ApplicationDBContext db,
            IEmailService emailService,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory,
            IOptions<SubscriptionOperationsOptions> options)
        {
            _db = db;
            _emailService = emailService;
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<SubscriptionMfaChallengeDto> CreateChallengeAsync(
            int userId,
            int draftId,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            var draft = await RequireOwnedDraftAsync(userId, draftId, cancellationToken);
            var user = draft.User ?? throw new InvalidOperationException("Utilisateur introuvable.");

            if (IsTotpPreferred()
                && await HasActiveTotpFactorAsync(userId, cancellationToken))
            {
                var totpExpiresAt = DateTime.UtcNow.Add(_options.Mfa.ChallengeLifetime);
                await AddAuditAsync(draftId, userId, "MfaChallengeCreated", new { Channel = "TOTP", Delivered = true, expiresAt = totpExpiresAt }, cancellationToken);
                return new SubscriptionMfaChallengeDto(
                    draftId,
                    "Application d'authentification",
                    "Code TOTP",
                    totpExpiresAt,
                    true,
                    null);
            }

            if (IsTwilioVerifyEnabled()
                && TryNormalizePhone(user.PhoneNumber, out var twilioPhone))
            {
                var sent = await StartTwilioVerificationAsync(twilioPhone, cancellationToken);
                var twilioExpiresAt = DateTime.UtcNow.Add(_options.Mfa.ChallengeLifetime);
                await AddAuditAsync(draftId, userId, "MfaChallengeCreated", new { Channel = "TwilioVerify", Delivered = sent, expiresAt = twilioExpiresAt }, cancellationToken);
                if (sent || !_options.Mfa.EmailFallbackEnabled)
                {
                    return new SubscriptionMfaChallengeDto(
                        draftId,
                        "SMS Twilio Verify",
                        MaskPhone(twilioPhone),
                        twilioExpiresAt,
                        sent,
                        null);
                }
            }

            await RevokeActiveChallengesAsync(userId, cancellationToken);

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
            var expiresAt = DateTime.UtcNow.Add(_options.Mfa.ChallengeLifetime);
            _db.UserSecurityTokens.Add(new UserSecurityToken
            {
                UserId = userId,
                TokenType = UserSecurityTokenTypes.SubscriptionMfa,
                TokenHash = Hash(code),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                CreatedByIpAddress = ipAddress,
            });
            await _db.SaveChangesAsync(cancellationToken);

            var message = $"Votre code de validation Financial Life est {code}. Il expire dans {_options.Mfa.ChallengeLifetime.TotalMinutes:0} minutes.";
            var delivery = await DeliverAsync(user, code, message, cancellationToken);
            await AddAuditAsync(draftId, userId, "MfaChallengeCreated", new { delivery.Channel, delivery.Delivered, expiresAt }, cancellationToken);

            return new SubscriptionMfaChallengeDto(
                draftId,
                delivery.Channel,
                delivery.MaskedTarget,
                expiresAt,
                delivery.Delivered,
                _environment.IsDevelopment() ? code : null);
        }

        public async Task<SubscriptionTotpSetupDto> CreateTotpSetupAsync(
            int userId,
            int draftId,
            CancellationToken cancellationToken)
        {
            if (!_options.Mfa.Totp.Enabled)
            {
                throw new InvalidOperationException("La MFA par application d'authentification n'est pas activée.");
            }

            var draft = await RequireOwnedDraftAsync(userId, draftId, cancellationToken);
            var user = draft.User ?? throw new InvalidOperationException("Utilisateur introuvable.");
            var factor = await _db.UserMfaFactors
                .Where(x => x.UserId == userId
                            && x.FactorType == UserMfaFactorTypes.Totp
                            && x.RevokedAt == null)
                .OrderByDescending(x => x.ActivatedAt != null)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var alreadyEnabled = factor?.ActivatedAt != null;
            string secret;
            if (factor == null)
            {
                secret = GenerateTotpSecret();
                factor = new UserMfaFactor
                {
                    UserId = userId,
                    FactorType = UserMfaFactorTypes.Totp,
                    DisplayName = "Application d'authentification",
                    ProtectedSecret = ProtectSecret(secret),
                    CreatedAt = DateTime.UtcNow,
                };
                _db.UserMfaFactors.Add(factor);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                secret = UnprotectSecret(factor.ProtectedSecret);
            }

            var accountName = string.IsNullOrWhiteSpace(user.Email) ? user.Username : user.Email;
            var issuer = string.IsNullOrWhiteSpace(_options.Mfa.Totp.Issuer) ? "Financial Life" : _options.Mfa.Totp.Issuer;
            var otpAuthUri = BuildOtpAuthUri(issuer, accountName, secret);
            var qrCodeDataUri = BuildQrCodeDataUri(otpAuthUri);
            await AddAuditAsync(draftId, userId, "TotpSetupPrepared", new { alreadyEnabled }, cancellationToken);

            return new SubscriptionTotpSetupDto(
                draftId,
                alreadyEnabled,
                issuer,
                accountName,
                alreadyEnabled ? null : secret,
                otpAuthUri,
                qrCodeDataUri,
                alreadyEnabled
                    ? "Une application d'authentification est déjà active pour ce compte."
                    : "Scannez le QR code puis saisissez le code à 6 chiffres pour activer la vérification forte gratuite.");
        }

        public async Task<SubscriptionMfaVerificationDto> VerifyAsync(
            int userId,
            int draftId,
            string code,
            CancellationToken cancellationToken)
        {
            var draft = await RequireOwnedDraftAsync(userId, draftId, cancellationToken);
            var user = draft.User ?? throw new InvalidOperationException("Utilisateur introuvable.");
            var trimmedCode = (code ?? string.Empty).Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
            var now = DateTime.UtcNow;

            if (await TryVerifyTotpAsync(userId, trimmedCode, now, cancellationToken))
            {
                await AddAuditAsync(draftId, userId, "MfaVerified", new { provider = "TOTP", verifiedAt = now, expiresAt = now.Add(_options.Mfa.ChallengeLifetime) }, cancellationToken);
                return new SubscriptionMfaVerificationDto(draftId, true, now, now.Add(_options.Mfa.ChallengeLifetime), "Vérification forte validée avec l'application d'authentification.");
            }

            if (IsTwilioVerifyEnabled() && TryNormalizePhone(user.PhoneNumber, out var phone))
            {
                var approved = await CheckTwilioVerificationAsync(phone, trimmedCode, cancellationToken);
                if (approved)
                {
                    await AddAuditAsync(draftId, userId, "MfaVerified", new { provider = "TwilioVerify", verifiedAt = now, expiresAt = now.Add(_options.Mfa.ChallengeLifetime) }, cancellationToken);
                    return new SubscriptionMfaVerificationDto(draftId, true, now, now.Add(_options.Mfa.ChallengeLifetime), "Vérification forte validée par SMS.");
                }
            }

            var hash = Hash(trimmedCode);
            var token = await _db.UserSecurityTokens
                .FirstOrDefaultAsync(
                    x => x.UserId == userId
                         && x.TokenType == UserSecurityTokenTypes.SubscriptionMfa
                         && x.TokenHash == hash
                         && x.UsedAt == null
                         && x.RevokedAt == null,
                    cancellationToken);

            if (token == null || token.ExpiresAt < now)
            {
                if (token != null)
                {
                    token.RevokedAt = now;
                    await _db.SaveChangesAsync(cancellationToken);
                }
                return new SubscriptionMfaVerificationDto(draftId, false, null, null, "Code invalide ou expiré.");
            }

            token.UsedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(draftId, userId, "MfaVerified", new { verifiedAt = now, expiresAt = now.Add(_options.Mfa.ChallengeLifetime) }, cancellationToken);
            return new SubscriptionMfaVerificationDto(draftId, true, now, now.Add(_options.Mfa.ChallengeLifetime), "Vérification forte validée.");
        }

        public async Task<bool> HasRecentVerificationAsync(int userId, int draftId, CancellationToken cancellationToken)
        {
            var threshold = DateTime.UtcNow.Subtract(_options.Mfa.ChallengeLifetime);
            return await _db.SubscriptionDraftAuditEvents
                .AsNoTracking()
                .AnyAsync(
                    x => x.SubscriptionDraftId == draftId
                         && x.UserId == userId
                         && x.EventType == "MfaVerified"
                         && x.CreatedAt >= threshold,
                    cancellationToken);
        }

        private async Task<SubscriptionDraft> RequireOwnedDraftAsync(int userId, int draftId, CancellationToken cancellationToken) =>
            await _db.SubscriptionDrafts
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == draftId && x.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Brouillon de souscription introuvable ou non autorisé.");

        private async Task RevokeActiveChallengesAsync(int userId, CancellationToken cancellationToken)
        {
            var active = await _db.UserSecurityTokens
                .Where(x => x.UserId == userId
                            && x.TokenType == UserSecurityTokenTypes.SubscriptionMfa
                            && x.UsedAt == null
                            && x.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var token in active)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        private async Task<(string Channel, string MaskedTarget, bool Delivered)> DeliverAsync(
            User user,
            string code,
            string message,
            CancellationToken cancellationToken)
        {
            var preferSms = string.Equals(_options.Mfa.PreferredChannel, "Sms", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_options.Mfa.PreferredChannel, "SMS", StringComparison.OrdinalIgnoreCase);
            if (preferSms && TryNormalizePhone(user.PhoneNumber, out var phone))
            {
                var smsDelivered = await SendSmsAsync(phone, message, cancellationToken);
                if (smsDelivered || !_options.Mfa.EmailFallbackEnabled)
                {
                    return ("SMS", MaskPhone(phone), smsDelivered);
                }
            }

            var emailDelivered = await _emailService.SendEmailAsync(
                user.Email,
                "Code de validation Financial Life",
                BuildMfaEmail(user, code, _options.Mfa.ChallengeLifetime),
                cancellationToken);
            return ("Email", MaskEmail(user.Email), emailDelivered);
        }

        private async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken)
        {
            var sms = _options.Mfa.Sms;
            if (!sms.Enabled || string.IsNullOrWhiteSpace(sms.EndpointUrl))
            {
                return false;
            }

            var client = _httpClientFactory.CreateClient("subscription-sms");
            using var request = new HttpRequestMessage(HttpMethod.Post, sms.EndpointUrl);
            if (!string.IsNullOrWhiteSpace(sms.BearerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sms.BearerToken);
            }
            else if (!string.IsNullOrWhiteSpace(sms.BasicUsername))
            {
                var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{sms.BasicUsername}:{sms.BasicPassword}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", raw);
            }

            var body = sms.JsonBodyTemplate
                .Replace("{to}", EscapeJson(phoneNumber), StringComparison.Ordinal)
                .Replace("{message}", EscapeJson(message), StringComparison.Ordinal);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.SendAsync(request, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private bool IsTotpPreferred() =>
            _options.Mfa.Totp.Enabled
            && (string.Equals(_options.Mfa.Provider, "Totp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_options.Mfa.PreferredChannel, "Totp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_options.Mfa.PreferredChannel, "Authenticator", StringComparison.OrdinalIgnoreCase));

        private bool IsTwilioVerifyEnabled() =>
            _options.Mfa.TwilioVerify.Enabled
            && string.Equals(_options.Mfa.Provider, "TwilioVerify", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_options.Mfa.TwilioVerify.ServiceSid)
            && HasTwilioCredentials();

        private bool HasTwilioCredentials() =>
            (!string.IsNullOrWhiteSpace(_options.Mfa.TwilioVerify.ApiKey)
             && !string.IsNullOrWhiteSpace(_options.Mfa.TwilioVerify.ApiSecret))
            || (!string.IsNullOrWhiteSpace(_options.Mfa.TwilioVerify.AccountSid)
                && !string.IsNullOrWhiteSpace(_options.Mfa.TwilioVerify.AuthToken));

        private async Task<bool> HasActiveTotpFactorAsync(int userId, CancellationToken cancellationToken) =>
            await _db.UserMfaFactors
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId
                               && x.FactorType == UserMfaFactorTypes.Totp
                               && x.ActivatedAt != null
                               && x.RevokedAt == null,
                    cancellationToken);

        private async Task<bool> TryVerifyTotpAsync(
            int userId,
            string code,
            DateTime now,
            CancellationToken cancellationToken)
        {
            if (!_options.Mfa.Totp.Enabled || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var factors = await _db.UserMfaFactors
                .Where(x => x.UserId == userId
                            && x.FactorType == UserMfaFactorTypes.Totp
                            && x.RevokedAt == null)
                .OrderByDescending(x => x.ActivatedAt != null)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            foreach (var factor in factors)
            {
                var secret = UnprotectSecret(factor.ProtectedSecret);
                if (!TotpMatches(secret, code, now))
                {
                    continue;
                }

                factor.LastUsedAt = now;
                factor.ActivatedAt ??= now;
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }

        private async Task<bool> StartTwilioVerificationAsync(string phoneNumber, CancellationToken cancellationToken)
        {
            var twilio = _options.Mfa.TwilioVerify;
            var client = _httpClientFactory.CreateClient("twilio-verify");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"Services/{Uri.EscapeDataString(twilio.ServiceSid)}/Verifications");
            AddTwilioAuthorization(request);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = phoneNumber,
                ["Channel"] = string.IsNullOrWhiteSpace(twilio.Channel) ? "sms" : twilio.Channel,
            });

            try
            {
                using var response = await client.SendAsync(request, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> CheckTwilioVerificationAsync(string phoneNumber, string code, CancellationToken cancellationToken)
        {
            var twilio = _options.Mfa.TwilioVerify;
            var client = _httpClientFactory.CreateClient("twilio-verify");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"Services/{Uri.EscapeDataString(twilio.ServiceSid)}/VerificationCheck");
            AddTwilioAuthorization(request);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = phoneNumber,
                ["Code"] = code,
            });

            try
            {
                using var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                return document.RootElement.TryGetProperty("status", out var status)
                    && string.Equals(status.GetString(), "approved", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void AddTwilioAuthorization(HttpRequestMessage request)
        {
            var twilio = _options.Mfa.TwilioVerify;
            var username = string.IsNullOrWhiteSpace(twilio.ApiKey) ? twilio.AccountSid : twilio.ApiKey;
            var password = string.IsNullOrWhiteSpace(twilio.ApiSecret) ? twilio.AuthToken : twilio.ApiSecret;
            var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", raw);
        }

        private string BuildOtpAuthUri(string issuer, string accountName, string secret)
        {
            var label = $"{issuer}:{accountName}";
            var query = new Dictionary<string, string?>
            {
                ["secret"] = secret,
                ["issuer"] = issuer,
                ["algorithm"] = "SHA1",
                ["digits"] = _options.Mfa.Totp.Digits.ToString(CultureInfo.InvariantCulture),
                ["period"] = _options.Mfa.Totp.PeriodSeconds.ToString(CultureInfo.InvariantCulture),
            };
            var queryString = string.Join(
                "&",
                query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"));
            return $"otpauth://totp/{Uri.EscapeDataString(label)}?{queryString}";
        }

        private static string BuildQrCodeDataUri(string content)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(data);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCode.GetGraphic(6))}";
        }

        private static string GenerateTotpSecret()
        {
            var bytes = RandomNumberGenerator.GetBytes(20);
            return Base32Encode(bytes);
        }

        private bool TotpMatches(string base32Secret, string code, DateTime now)
        {
            if (!int.TryParse(code, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                return false;
            }

            var secret = Base32Decode(base32Secret);
            var period = Math.Max(15, _options.Mfa.Totp.PeriodSeconds);
            var digits = Math.Clamp(_options.Mfa.Totp.Digits, 6, 8);
            var unixTime = new DateTimeOffset(now).ToUnixTimeSeconds();
            var currentStep = unixTime / period;
            var drift = Math.Clamp(_options.Mfa.Totp.AllowedClockDriftSteps, 0, 3);
            for (var offset = -drift; offset <= drift; offset++)
            {
                if (FixedTimeEquals(ComputeTotp(secret, currentStep + offset, digits), code))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ComputeTotp(byte[] secret, long timestep, int digits)
        {
            var counter = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(timestep));
            using var hmac = new HMACSHA1(secret);
            var hash = hmac.ComputeHash(counter);
            var offset = hash[^1] & 0x0F;
            var binary =
                ((hash[offset] & 0x7f) << 24)
                | ((hash[offset + 1] & 0xff) << 16)
                | ((hash[offset + 2] & 0xff) << 8)
                | (hash[offset + 3] & 0xff);
            var modulo = (int)Math.Pow(10, digits);
            return (binary % modulo).ToString(new string('0', digits), CultureInfo.InvariantCulture);
        }

        private static bool FixedTimeEquals(string expected, string actual)
        {
            var expectedBytes = Encoding.UTF8.GetBytes(expected);
            var actualBytes = Encoding.UTF8.GetBytes(actual);
            return expectedBytes.Length == actualBytes.Length
                   && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }

        private string ProtectSecret(string secret)
        {
            var key = SecretProtectionKey();
            var nonce = RandomNumberGenerator.GetBytes(12);
            var plaintext = Encoding.UTF8.GetBytes(secret);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
            return string.Join(".", "v1", Convert.ToBase64String(nonce), Convert.ToBase64String(ciphertext), Convert.ToBase64String(tag));
        }

        private string UnprotectSecret(string protectedSecret)
        {
            var parts = protectedSecret.Split('.');
            if (parts.Length != 4 || parts[0] != "v1")
            {
                throw new InvalidOperationException("Secret MFA invalide.");
            }

            var nonce = Convert.FromBase64String(parts[1]);
            var ciphertext = Convert.FromBase64String(parts[2]);
            var tag = Convert.FromBase64String(parts[3]);
            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(SecretProtectionKey(), 16);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }

        private byte[] SecretProtectionKey()
        {
            var configuredKey = _options.Mfa.Totp.SecretEncryptionKey;
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                configuredKey = _environment.IsDevelopment()
                    ? "life-local-totp-secret-placeholder"
                    : throw new InvalidOperationException("SubscriptionOperations:Mfa:Totp:SecretEncryptionKey doit être défini avant d'activer TOTP en production.");
            }

            try
            {
                var decoded = Convert.FromBase64String(configuredKey);
                return decoded.Length >= 32 ? decoded[..32] : SHA256.HashData(decoded);
            }
            catch (FormatException)
            {
                return SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
            }
        }

        private static string Base32Encode(byte[] bytes)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var output = new StringBuilder();
            var bitBuffer = 0;
            var bitCount = 0;
            foreach (var value in bytes)
            {
                bitBuffer = (bitBuffer << 8) | value;
                bitCount += 8;
                while (bitCount >= 5)
                {
                    output.Append(alphabet[(bitBuffer >> (bitCount - 5)) & 31]);
                    bitCount -= 5;
                }
            }

            if (bitCount > 0)
            {
                output.Append(alphabet[(bitBuffer << (5 - bitCount)) & 31]);
            }

            return output.ToString();
        }

        private static byte[] Base32Decode(string value)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var bytes = new List<byte>();
            var bitBuffer = 0;
            var bitCount = 0;
            foreach (var raw in value.Trim().TrimEnd('=').ToUpperInvariant())
            {
                var index = alphabet.IndexOf(raw, StringComparison.Ordinal);
                if (index < 0)
                {
                    continue;
                }

                bitBuffer = (bitBuffer << 5) | index;
                bitCount += 5;
                if (bitCount >= 8)
                {
                    bytes.Add((byte)((bitBuffer >> (bitCount - 8)) & 255));
                    bitCount -= 8;
                }
            }

            return bytes.ToArray();
        }

        private static Task AddAuditAsync(ApplicationDBContext db, int draftId, int userId, string eventType, object details, CancellationToken cancellationToken)
        {
            db.SubscriptionDraftAuditEvents.Add(new SubscriptionDraftAuditEvent
            {
                SubscriptionDraftId = draftId,
                UserId = userId,
                EventType = eventType,
                StepKey = SubscriptionStepKeys.Signature,
                NewStateJson = JsonSerializer.Serialize(details, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                RulesVersion = "subscription-rules-v1",
            });
            return db.SaveChangesAsync(cancellationToken);
        }

        private Task AddAuditAsync(int draftId, int userId, string eventType, object details, CancellationToken cancellationToken) =>
            AddAuditAsync(_db, draftId, userId, eventType, details, cancellationToken);

        private static string Hash(string value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        private static string MaskEmail(string email)
        {
            var parts = email.Split('@', 2);
            if (parts.Length != 2 || parts[0].Length == 0) return "***";
            return $"{parts[0][0]}***@{parts[1]}";
        }

        private static bool TryNormalizePhone(string? value, out string normalized)
        {
            normalized = new string((value ?? string.Empty).Where(c => char.IsDigit(c) || c == '+').ToArray());
            if (normalized.StartsWith("00", StringComparison.Ordinal)) normalized = "+" + normalized[2..];
            return normalized.StartsWith("+", StringComparison.Ordinal)
                   && normalized.Length is >= 9 and <= 16
                   && normalized.Skip(1).All(char.IsDigit);
        }

        private static string MaskPhone(string phone) =>
            phone.Length <= 6 ? "****" : $"{phone[..4]} ** ** {phone[^2..]}";

        private static string EscapeJson(string value) =>
            value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

        private static string BuildMfaEmail(User user, string code, TimeSpan lifetime) =>
            $"""
            <!doctype html>
            <html lang="fr">
            <body style="font-family:Arial,sans-serif;color:#102033;">
              <h1>Code de validation Financial Life</h1>
              <p>Bonjour {System.Net.WebUtility.HtmlEncode(user.FirstName)},</p>
              <p>Votre code de validation est <strong style="font-size:22px;">{code}</strong>.</p>
              <p>Il est valable {lifetime.TotalMinutes:0} minutes et permet de poursuivre la signature de votre dossier de souscription.</p>
            </body>
            </html>
            """;
    }

    public sealed class SubscriptionPaymentPreparationService : ISubscriptionPaymentPreparationService
    {
        private readonly ApplicationDBContext _db;
        private readonly IIbanValidator _ibanValidator;
        private readonly SubscriptionOperationsOptions _options;

        public SubscriptionPaymentPreparationService(
            ApplicationDBContext db,
            IIbanValidator ibanValidator,
            IOptions<SubscriptionOperationsOptions> options)
        {
            _db = db;
            _ibanValidator = ibanValidator;
            _options = options.Value;
        }

        public async Task<SubscriptionPaymentPreparationDto> PrepareAsync(int userId, int draftId, CancellationToken cancellationToken)
        {
            var draft = await RequireOwnedDraftAsync(userId, draftId, cancellationToken);
            var investment = ReadObject(draft.InvestmentDataJson);
            var iban = ReadString(investment, "ibanLabel");
            var paymentMode = ReadString(investment, "paymentMode") ?? "Non précisé";
            if (paymentMode.Contains("SEPA", StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(iban) || !_ibanValidator.TryNormalizeIban(iban, out iban)))
            {
                throw new InvalidOperationException("Un IBAN valide est obligatoire pour préparer le prélèvement SEPA.");
            }

            var dto = new SubscriptionPaymentPreparationDto(
                draft.Id,
                paymentMode,
                ReadDecimal(investment, "initialAmount"),
                ReadBool(investment, "scheduledPaymentEnabled"),
                ReadDecimal(investment, "scheduledAmount"),
                ReadString(investment, "scheduledFrequency"),
                string.IsNullOrWhiteSpace(iban) ? null : MaskIban(iban),
                _options.Payment.ExecutionEnabled ? $"Préparé - {_options.Payment.Provider}" : $"Préparé - {_options.Payment.Provider} à connecter",
                DateTime.UtcNow);

            await AddAuditAsync(draft.Id, userId, "PaymentPrepared", dto, cancellationToken);
            return dto;
        }

        public async Task<bool> IsPreparedAsync(int userId, int draftId, CancellationToken cancellationToken) =>
            await _db.SubscriptionDraftAuditEvents
                .AsNoTracking()
                .AnyAsync(x => x.SubscriptionDraftId == draftId && x.UserId == userId && x.EventType == "PaymentPrepared", cancellationToken);

        private async Task<SubscriptionDraft> RequireOwnedDraftAsync(int userId, int draftId, CancellationToken cancellationToken) =>
            await _db.SubscriptionDrafts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == draftId && x.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Brouillon de souscription introuvable ou non autorisé.");

        private async Task AddAuditAsync(int draftId, int userId, string eventType, object details, CancellationToken cancellationToken)
        {
            _db.SubscriptionDraftAuditEvents.Add(new SubscriptionDraftAuditEvent
            {
                SubscriptionDraftId = draftId,
                UserId = userId,
                EventType = eventType,
                StepKey = SubscriptionStepKeys.Signature,
                NewStateJson = JsonSerializer.Serialize(details, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                RulesVersion = "subscription-rules-v1",
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        private static Dictionary<string, JsonElement> ReadObject(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, JsonElement>();
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.Clone())
                : new Dictionary<string, JsonElement>();
        }

        private static string? ReadString(Dictionary<string, JsonElement> values, string key)
        {
            if (!values.TryGetValue(key, out var value)) return null;
            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        private static decimal ReadDecimal(Dictionary<string, JsonElement> values, string key)
        {
            var raw = ReadString(values, key);
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) return value;
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.GetCultureInfo("fr-FR"), out value)) return value;
            return 0m;
        }

        private static bool ReadBool(Dictionary<string, JsonElement> values, string key) =>
            values.TryGetValue(key, out var value)
            && (value.ValueKind == JsonValueKind.True || (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed) && parsed));

        private static string MaskIban(string iban) =>
            iban.Length <= 8 ? "****" : $"{iban[..4]} **** **** **** {iban[^4..]}";
    }

    public sealed class SubscriptionSignatureService : ISubscriptionSignatureService
    {
        private readonly ApplicationDBContext _db;
        private readonly ISubscriptionDocumentService _documentService;
        private readonly ISubscriptionPaymentPreparationService _paymentService;
        private readonly ISubscriptionMfaService _mfaService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SubscriptionOperationsOptions _options;

        public SubscriptionSignatureService(
            ApplicationDBContext db,
            ISubscriptionDocumentService documentService,
            ISubscriptionPaymentPreparationService paymentService,
            ISubscriptionMfaService mfaService,
            IHttpClientFactory httpClientFactory,
            IOptions<SubscriptionOperationsOptions> options)
        {
            _db = db;
            _documentService = documentService;
            _paymentService = paymentService;
            _mfaService = mfaService;
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<SubscriptionSignatureEnvelopeDto> PrepareEnvelopeAsync(
            int userId,
            int draftId,
            string? userName,
            CancellationToken cancellationToken)
        {
            var dossier = await _documentService.GetDossierAsync(userId, draftId, cancellationToken);
            if (!dossier.IsComplete)
            {
                throw new InvalidOperationException("Le dossier documentaire doit être généré avant de préparer la signature.");
            }
            if (!await _paymentService.IsPreparedAsync(userId, draftId, cancellationToken))
            {
                throw new InvalidOperationException("Le paiement ou prélèvement doit être préparé avant la signature.");
            }
            if (!await _mfaService.HasRecentVerificationAsync(userId, draftId, cancellationToken))
            {
                throw new InvalidOperationException("Une vérification forte récente est obligatoire avant la signature.");
            }

            var preparedAt = DateTime.UtcNow;
            var draft = await _db.SubscriptionDrafts
                .Include(x => x.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == draftId && x.UserId == userId, cancellationToken)
                ?? throw new KeyNotFoundException("Brouillon de souscription introuvable ou non autorisé.");
            var user = draft.User ?? throw new InvalidOperationException("Utilisateur introuvable.");
            var files = new List<SubscriptionDocumentFileDto>();
            foreach (var document in dossier.Documents.Where(x => x.ArtifactId.HasValue))
            {
                files.Add(await _documentService.GetDocumentFileAsync(userId, draftId, document.ArtifactId!.Value, cancellationToken));
            }

            var dto = _options.Signature.ExecutionEnabled
                ? await CreateProviderEnvelopeAsync(draftId, user, files, preparedAt, cancellationToken)
                : new SubscriptionSignatureEnvelopeDto(
                    draftId,
                    $"SUB-{draftId}-{preparedAt:yyyyMMddHHmmss}",
                    _options.Signature.Provider,
                    "Préparée - provider à connecter",
                    preparedAt,
                    dossier.Documents.Select(x => x.DocumentName).ToArray());

            _db.SubscriptionDraftAuditEvents.Add(new SubscriptionDraftAuditEvent
            {
                SubscriptionDraftId = draftId,
                UserId = userId,
                EventType = "SignatureEnvelopePrepared",
                StepKey = SubscriptionStepKeys.Signature,
                NewStateJson = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                RulesVersion = "subscription-rules-v1",
            });
            await _db.SaveChangesAsync(cancellationToken);
            return dto;
        }

        public async Task<bool> IsEnvelopePreparedAsync(int userId, int draftId, CancellationToken cancellationToken) =>
            await _db.SubscriptionDraftAuditEvents
                .AsNoTracking()
                .AnyAsync(x => x.SubscriptionDraftId == draftId && x.UserId == userId && x.EventType == "SignatureEnvelopePrepared", cancellationToken);

        private async Task<SubscriptionSignatureEnvelopeDto> CreateProviderEnvelopeAsync(
            int draftId,
            User user,
            IReadOnlyList<SubscriptionDocumentFileDto> files,
            DateTime preparedAt,
            CancellationToken cancellationToken)
        {
            if (files.Count == 0)
            {
                throw new InvalidOperationException("Aucun PDF généré n'est disponible pour créer l'enveloppe de signature.");
            }

            if (string.Equals(_options.Signature.Provider, "DocuSeal", StringComparison.OrdinalIgnoreCase))
            {
                return await CreateDocuSealEnvelopeAsync(draftId, user, files, preparedAt, cancellationToken);
            }

            if (string.Equals(_options.Signature.Provider, "Youtrust", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_options.Signature.Provider, "Yousign", StringComparison.OrdinalIgnoreCase))
            {
                return await CreateYoutrustEnvelopeAsync(draftId, user, files, preparedAt, cancellationToken);
            }

            return new SubscriptionSignatureEnvelopeDto(
                draftId,
                $"SUB-{draftId}-{preparedAt:yyyyMMddHHmmss}",
                _options.Signature.Provider,
                "Préparée - provider non automatisé",
                preparedAt,
                files.Select(x => x.FileName).ToArray());
        }

        private async Task<SubscriptionSignatureEnvelopeDto> CreateDocuSealEnvelopeAsync(
            int draftId,
            User user,
            IReadOnlyList<SubscriptionDocumentFileDto> files,
            DateTime preparedAt,
            CancellationToken cancellationToken)
        {
            var options = _options.Signature.DocuSeal;
            if (!options.Enabled || string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException("Le provider DocuSeal n'est pas configuré.");
            }

            var client = _httpClientFactory.CreateClient("docuseal-signature");
            using var request = new HttpRequestMessage(HttpMethod.Post, "submissions/pdf");
            request.Headers.Add("X-Auth-Token", options.ApiKey);
            var payload = new
            {
                name = $"Souscription Financial Life {draftId}",
                send_email = options.SendEmail,
                send_sms = options.SendSms,
                order = "preserved",
                documents = files.Select(file => new
                {
                    name = file.FileName,
                    file = Convert.ToBase64String(file.Content),
                }).ToArray(),
                submitters = new[]
                {
                    new
                    {
                        role = string.IsNullOrWhiteSpace(options.SignerRole) ? "Souscripteur" : options.SignerRole,
                        email = user.Email,
                        name = DisplayName(user),
                        phone = TryNormalizePhone(user.PhoneNumber, out var phone) ? phone : null,
                        external_id = $"subscription-draft-{draftId}-user-{user.Id}",
                        require_phone_2fa = options.RequirePhone2Fa && TryNormalizePhone(user.PhoneNumber, out _),
                    },
                },
            };
            request.Content = JsonContent(payload);
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"DocuSeal a refusé la création de l'enveloppe ({(int)response.StatusCode}).");
            }

            using var json = JsonDocument.Parse(body);
            var reference = json.RootElement.TryGetProperty("id", out var id)
                ? $"DOCUSEAL-{id}"
                : $"DOCUSEAL-{draftId}-{preparedAt:yyyyMMddHHmmss}";
            var status = json.RootElement.TryGetProperty("status", out var statusElement)
                ? $"DocuSeal - {statusElement.GetString()}"
                : "DocuSeal - envoyée";
            return new SubscriptionSignatureEnvelopeDto(
                draftId,
                reference,
                "DocuSeal",
                status,
                preparedAt,
                files.Select(x => x.FileName).ToArray());
        }

        private async Task<SubscriptionSignatureEnvelopeDto> CreateYoutrustEnvelopeAsync(
            int draftId,
            User user,
            IReadOnlyList<SubscriptionDocumentFileDto> files,
            DateTime preparedAt,
            CancellationToken cancellationToken)
        {
            var options = _options.Signature.Youtrust;
            if (!options.Enabled || string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException("Le provider Youtrust/Yousign n'est pas configuré.");
            }

            var client = _httpClientFactory.CreateClient("youtrust-signature");
            var signatureRequest = await SendYoutrustJsonAsync(
                client,
                HttpMethod.Post,
                "signature_requests",
                options.ApiKey,
                new
                {
                    name = $"Souscription Financial Life {draftId}",
                    delivery_mode = string.IsNullOrWhiteSpace(options.DeliveryMode) ? "email" : options.DeliveryMode,
                    external_id = $"subscription-draft-{draftId}",
                },
                cancellationToken);
            var signatureRequestId = ReadRequiredString(signatureRequest, "id", "référence de signature Youtrust");

            var documentIds = new List<string>();
            foreach (var file in files)
            {
                var uploaded = await UploadYoutrustDocumentAsync(client, options.ApiKey, signatureRequestId, file, cancellationToken);
                documentIds.Add(ReadRequiredString(uploaded, "id", "document Youtrust"));
            }

            await SendYoutrustJsonAsync(
                client,
                HttpMethod.Post,
                $"signature_requests/{Uri.EscapeDataString(signatureRequestId)}/signers",
                options.ApiKey,
                new
                {
                    info = new
                    {
                        first_name = user.FirstName,
                        last_name = user.LastName,
                        email = user.Email,
                        phone_number = TryNormalizePhone(user.PhoneNumber, out var phone) ? phone : null,
                        locale = string.IsNullOrWhiteSpace(options.Locale) ? "fr" : options.Locale,
                    },
                    signature_level = string.IsNullOrWhiteSpace(options.SignatureLevel) ? "electronic_signature" : options.SignatureLevel,
                    signature_authentication_mode = string.IsNullOrWhiteSpace(options.AuthenticationMode) ? "otp_sms" : options.AuthenticationMode,
                    fields = new[]
                    {
                        new
                        {
                            type = "signature",
                            document_id = documentIds[0],
                            page = Math.Max(1, options.SignaturePage),
                            x = options.SignatureX,
                            y = options.SignatureY,
                            width = options.SignatureWidth,
                            height = options.SignatureHeight,
                        },
                    },
                },
                cancellationToken);

            if (options.AutoActivate)
            {
                await SendYoutrustJsonAsync(
                    client,
                    HttpMethod.Post,
                    $"signature_requests/{Uri.EscapeDataString(signatureRequestId)}/activate",
                    options.ApiKey,
                    new { },
                    cancellationToken);
            }

            return new SubscriptionSignatureEnvelopeDto(
                draftId,
                $"YOUTRUST-{signatureRequestId}",
                "Youtrust",
                options.AutoActivate ? "Youtrust - envoyée" : "Youtrust - brouillon prêt",
                preparedAt,
                files.Select(x => x.FileName).ToArray());
        }

        private static async Task<JsonDocument> SendYoutrustJsonAsync(
            HttpClient client,
            HttpMethod method,
            string path,
            string apiKey,
            object payload,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent(payload);
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Youtrust/Yousign a refusé l'opération ({(int)response.StatusCode}).");
            }

            return JsonDocument.Parse(body);
        }

        private static async Task<JsonDocument> UploadYoutrustDocumentAsync(
            HttpClient client,
            string apiKey,
            string signatureRequestId,
            SubscriptionDocumentFileDto file,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"signature_requests/{Uri.EscapeDataString(signatureRequestId)}/documents");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using var multipart = new MultipartFormDataContent();
            multipart.Add(new StringContent("signable_document"), "nature");
            var content = new ByteArrayContent(file.Content);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
            multipart.Add(content, "file", file.FileName);
            request.Content = multipart;

            var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Dispose();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Youtrust/Yousign a refusé le document ({(int)response.StatusCode}).");
            }

            return JsonDocument.Parse(body);
        }

        private static StringContent JsonContent(object payload) =>
            new(JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json");

        private static string ReadRequiredString(JsonDocument json, string propertyName, string label)
        {
            if (json.RootElement.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(property.GetString()))
            {
                return property.GetString()!;
            }

            throw new InvalidOperationException($"La réponse provider ne contient pas {label}.");
        }

        private static string DisplayName(User user)
        {
            var value = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return string.IsNullOrWhiteSpace(value) ? user.Username : value;
        }

        private static bool TryNormalizePhone(string? value, out string normalized)
        {
            normalized = new string((value ?? string.Empty).Where(c => char.IsDigit(c) || c == '+').ToArray());
            if (normalized.StartsWith("00", StringComparison.Ordinal)) normalized = "+" + normalized[2..];
            return normalized.StartsWith("+", StringComparison.Ordinal)
                   && normalized.Length is >= 9 and <= 16
                   && normalized.Skip(1).All(char.IsDigit);
        }
    }
}
