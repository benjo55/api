using System.Net.Mail;
using System.Text;
using api.Dtos.Development;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/development/email")]
    [AllowAnonymous]
    public sealed class DevelopmentEmailController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ISmtpMailSender _mailSender;

        public DevelopmentEmailController(IWebHostEnvironment environment, ISmtpMailSender mailSender)
        {
            _environment = environment;
            _mailSender = mailSender;
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
            var subject = "[Life Development] Test SMTP Mailpit";
            var body = $"Bonjour,\n\nCeci est un e-mail de test Life envoyé depuis l'environnement Development.\nHorodatage UTC: {timestampUtc:O}\n\nMailpit doit afficher ce message dans son interface web locale.";
            var attachmentBytes = Encoding.UTF8.GetBytes(
                $"Mailpit test attachment\nEnvironment: Development\nTimestampUtc: {timestampUtc:O}\n");

            using var message = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(request.Recipient.Trim());
            message.Attachments.Add(new Attachment(new MemoryStream(attachmentBytes), "mailpit-test.txt", "text/plain"));

            var sent = await _mailSender.SendAsync(message, "development-test", cancellationToken);
            if (!sent.Success)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = "Le message de test n'a pas pu être envoyé. Vérifiez que Mailpit est démarré.",
                    recipient = request.Recipient.Trim(),
                    sent = false
                });
            }

            return Ok(new
            {
                message = "Le message de test a été envoyé à Mailpit.",
                recipient = request.Recipient.Trim(),
                sent = true,
                timestampUtc
            });
        }
    }
}
