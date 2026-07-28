namespace api.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(
            string recipientEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default);
    }
}
