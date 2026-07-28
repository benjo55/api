using System.Net;
using System.Net.Mail;
using api.Configuration;
using api.Interfaces;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public sealed class SmtpMailSender : ISmtpMailSender
    {
        private readonly MailSettings _settings;
        private readonly ILogger<SmtpMailSender> _logger;

        public SmtpMailSender(IOptions<MailSettings> settings, ILogger<SmtpMailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<SmtpMailSendResult> SendAsync(
            MailMessage message,
            string messageType,
            CancellationToken cancellationToken = default)
        {
            var recipients = message.To
                .Cast<MailAddress>()
                .Select(address => MaskEmail(address.Address))
                .ToArray();
            var host = _settings.Host?.Trim();

            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning(
                    "Mail non envoyé: SMTP non configuré. Type={MessageType}, Host={Host}, Recipients={Recipients}",
                    messageType,
                    host ?? string.Empty,
                    string.Join(", ", recipients));
                return new SmtpMailSendResult(false, "SmtpNotConfigured", "SMTP non configuré.");
            }

            try
            {
                if (message.From is null)
                {
                    message.From = new MailAddress(_settings.FromAddress, _settings.FromName);
                }

                using var client = new SmtpClient(host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrWhiteSpace(_settings.UserName))
                {
                    client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);
                }

                await client.SendMailAsync(message, cancellationToken);

                _logger.LogInformation(
                    "Mail envoyé. Type={MessageType}, Host={Host}, Port={Port}, EnableSsl={EnableSsl}, Recipients={Recipients}, AttachmentCount={AttachmentCount}",
                    messageType,
                    host,
                    _settings.Port,
                    _settings.EnableSsl,
                    string.Join(", ", recipients),
                    message.Attachments.Count);

                return new SmtpMailSendResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Mail non envoyé. Type={MessageType}, Host={Host}, Port={Port}, EnableSsl={EnableSsl}, Recipients={Recipients}, Error={Error}",
                    messageType,
                    host,
                    _settings.Port,
                    _settings.EnableSsl,
                    string.Join(", ", recipients),
                    ex.Message);
                return new SmtpMailSendResult(false, ex.GetType().Name, ex.Message);
            }
        }

        private static string MaskEmail(string email)
        {
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
    }
}
