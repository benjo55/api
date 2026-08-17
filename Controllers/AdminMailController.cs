using System.Net.Mail;
using System.Text;
using api.Configuration;
using api.Dtos.Admin;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace api.Controllers
{
    [ApiController]
    [Route("api/admin/mail")]
    [Authorize(Roles = "Admin,Administrator,SuperAdministrator")]
    public sealed class AdminMailController : ControllerBase
    {
        [HttpGet("config")]
        public IActionResult GetMailConfiguration([FromServices] IOptions<MailSettings> options)
        {
            var settings = options.Value;
            var missingSettings = GetMissingMailSettings(settings);

            return Ok(new AdminMailConfigurationDto(
                settings.Provider,
                settings.Host,
                settings.Port,
                settings.EnableSsl,
                settings.FromAddress,
                settings.FromName,
                MaskEmail(settings.UserName),
                !string.IsNullOrWhiteSpace(settings.UserName),
                !string.IsNullOrWhiteSpace(settings.Password),
                missingSettings.Length == 0,
                missingSettings));
        }

        [HttpPost("test")]
        public async Task<IActionResult> SendMailTest(
            [FromBody] AdminMailTestRequestDto request,
            [FromServices] ISmtpMailSender mailSender,
            [FromServices] IOptions<MailSettings> options,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Recipient))
            {
                return BadRequest(new { message = "Le destinataire est obligatoire." });
            }

            try
            {
                _ = new MailAddress(request.Recipient.Trim());
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "Le destinataire n'est pas une adresse e-mail valide." });
            }

            var settings = options.Value;
            var missingSettings = GetMissingMailSettings(settings);
            if (missingSettings.Length > 0)
            {
                return BadRequest(new
                {
                    message = "La configuration SMTP est incomplète.",
                    missingSettings
                });
            }

            var timestampUtc = DateTime.UtcNow;
            var messageId = CreateMessageId(settings.FromAddress, timestampUtc);
            var body = new StringBuilder()
                .AppendLine("Bonjour,")
                .AppendLine()
                .AppendLine("Ceci est un e-mail de test SMTP Brevo envoyé depuis Life.")
                .AppendLine($"Horodatage UTC: {timestampUtc:O}")
                .AppendLine($"Message-ID: {messageId}")
                .ToString();

            using var message = new MailMessage
            {
                Subject = "[Financial Life] Test SMTP Brevo",
                Body = body,
                IsBodyHtml = false,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.Headers.Add("Message-ID", messageId);
            message.To.Add(request.Recipient.Trim());

            var result = await mailSender.SendAsync(message, "admin-mail-test", cancellationToken);
            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = "Le message de test n'a pas pu être envoyé.",
                    recipient = request.Recipient.Trim(),
                    messageId,
                    errorType = result.ErrorType,
                    errorMessage = result.ErrorMessage,
                    sent = false
                });
            }

            return Ok(new AdminMailTestResponseDto(
                true,
                request.Recipient.Trim(),
                messageId,
                timestampUtc,
                "Le message de test a été accepté par le relais SMTP."));
        }

        private static string[] GetMissingMailSettings(MailSettings settings)
        {
            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(settings.Host))
            {
                missing.Add("Host");
            }

            if (RequiresCredentials(settings.Host)
                && string.IsNullOrWhiteSpace(settings.UserName))
            {
                missing.Add("UserName");
            }

            if (RequiresCredentials(settings.Host)
                && string.IsNullOrWhiteSpace(settings.Password))
            {
                missing.Add("Password");
            }

            if (string.IsNullOrWhiteSpace(settings.FromAddress))
            {
                missing.Add("FromAddress");
            }

            return missing.ToArray();
        }

        private static bool RequiresCredentials(string? host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            return !host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                && !host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                && !host.Equals("::1", StringComparison.OrdinalIgnoreCase);
        }

        private static string? MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var atIndex = email.IndexOf('@');
            if (atIndex <= 0)
            {
                return "***";
            }

            var localPart = email[..atIndex];
            var domainPart = email[(atIndex + 1)..];
            var visible = localPart.Length <= 2 ? localPart : localPart[..2];
            return $"{visible}***@{domainPart}";
        }

        private static string CreateMessageId(string fromAddress, DateTime timestampUtc)
        {
            var domain = "life.local";
            var atIndex = fromAddress.IndexOf('@');
            if (atIndex >= 0 && atIndex < fromAddress.Length - 1)
            {
                domain = fromAddress[(atIndex + 1)..];
            }

            return $"<life-test-{timestampUtc:yyyyMMddHHmmss}-{Guid.NewGuid():N}@{domain}>";
        }
    }
}
