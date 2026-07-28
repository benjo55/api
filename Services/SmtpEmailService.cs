using api.Interfaces;

namespace api.Services
{
    public sealed class SmtpEmailService : IEmailService
    {
        private readonly ISmtpMailSender _mailSender;

        public SmtpEmailService(ISmtpMailSender mailSender)
        {
            _mailSender = mailSender;
        }

        public async Task<bool> SendEmailAsync(
            string recipientEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            using var message = new System.Net.Mail.MailMessage
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(recipientEmail);

            var result = await _mailSender.SendAsync(message, "auth", cancellationToken);
            return result.Success;
        }
    }
}
