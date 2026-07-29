using System.Net.Mail;
using System.Text;
using api.Configuration;
using api.Dtos.Development;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace api.Controllers
{
    [ApiController]
    [Route("api/development/email")]
    [AllowAnonymous]
    public sealed class DevelopmentEmailController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ISmtpMailSender _mailSender;
        private readonly MailSettings _mailSettings;

        public DevelopmentEmailController(
            IWebHostEnvironment environment,
            ISmtpMailSender mailSender,
            IOptions<MailSettings> mailSettings)
        {
            _environment = environment;
            _mailSender = mailSender;
            _mailSettings = mailSettings.Value;
        }

        [HttpGet("config")]
        public IActionResult GetEmailConfiguration()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            return Ok(new
            {
                environment = _environment.EnvironmentName,
                host = _mailSettings.Host,
                port = _mailSettings.Port,
                enableSsl = _mailSettings.EnableSsl,
                fromAddress = _mailSettings.FromAddress,
                fromName = _mailSettings.FromName,
                hasUserName = !string.IsNullOrWhiteSpace(_mailSettings.UserName),
                hasPassword = !string.IsNullOrWhiteSpace(_mailSettings.Password)
            });
        }

        [HttpPost("test")]
        public async Task<IActionResult> SendTestEmail([FromBody] DevelopmentEmailTestRequestDto request, CancellationToken cancellationToken)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.Recipient))
            {
                return BadRequest(new { message = "Le destinataire est obligatoire." });
            }

            var timestampUtc = DateTime.UtcNow;
            var subject = "[Life Development] Test SMTP";
            var body = $"Bonjour,\n\nCeci est un e-mail de test Life envoyé depuis l'environnement Development.\nHorodatage UTC: {timestampUtc:O}";
            var attachmentBytes = Encoding.UTF8.GetBytes(
                $"SMTP test attachment\nEnvironment: Development\nTimestampUtc: {timestampUtc:O}\n");

            using var message = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(request.Recipient.Trim());
            message.Attachments.Add(new Attachment(new MemoryStream(attachmentBytes), "smtp-test.txt", "text/plain"));

            var sent = await _mailSender.SendAsync(message, "development-test", cancellationToken);
            if (!sent.Success)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = "Le message de test n'a pas pu être envoyé. Vérifiez la configuration SMTP/Mailjet.",
                    recipient = request.Recipient.Trim(),
                    errorType = sent.ErrorType,
                    errorMessage = sent.ErrorMessage,
                    sent = false
                });
            }

            return Ok(new
            {
                message = "Le message de test a été envoyé.",
                recipient = request.Recipient.Trim(),
                sent = true,
                timestampUtc
            });
        }
    }
}
