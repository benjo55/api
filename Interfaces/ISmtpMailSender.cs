using System.Net.Mail;

namespace api.Interfaces
{
    public sealed record SmtpMailSendResult(
        bool Success,
        string? ErrorType = null,
        string? ErrorMessage = null);

    public interface ISmtpMailSender
    {
        Task<SmtpMailSendResult> SendAsync(
            MailMessage message,
            string messageType,
            CancellationToken cancellationToken = default);
    }
}
