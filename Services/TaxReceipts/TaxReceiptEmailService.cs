using System.Globalization;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using api.Data;
using api.Dtos.TaxReceipts;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.TaxReceipts
{
    public sealed class TaxReceiptEmailService : ITaxReceiptEmailService
    {
        private const string UserFriendlyFailureMessage =
            "Le reçu fiscal a été généré, mais son envoi par e-mail a échoué. Vous pouvez le télécharger et réessayer l'envoi.";

        private readonly ApplicationDBContext _db;
        private readonly IDocumentBinaryStorage _storage;
        private readonly ISmtpMailSender _mailSender;
        private readonly ILogger<TaxReceiptEmailService> _logger;
        private readonly IWebHostEnvironment _environment;

        public TaxReceiptEmailService(
            ApplicationDBContext db,
            IDocumentBinaryStorage storage,
            ISmtpMailSender mailSender,
            ILogger<TaxReceiptEmailService> logger,
            IWebHostEnvironment environment)
        {
            _db = db;
            _storage = storage;
            _mailSender = mailSender;
            _logger = logger;
            _environment = environment;
        }

        public async Task<TaxReceiptEmailSendResultDto> SendAsync(
            int taxReceiptId,
            SendTaxReceiptEmailDto dto,
            string? userName,
            CancellationToken cancellationToken = default,
            int? currentUserId = null,
            bool canAccessAllReceipts = false)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            var receipt = await _db.TaxReceipts
                .Include(x => x.Donation)
                    .ThenInclude(x => x.Donor)
                .Include(x => x.Donation)
                    .ThenInclude(x => x.DonorSnapshot)
                .Include(x => x.DocumentArtifact)
                .FirstOrDefaultAsync(x => x.Id == taxReceiptId, cancellationToken)
                ?? throw new BusinessException("TaxReceiptNotFound");

            if (receipt.Status is not (TaxReceiptStatus.Generated or TaxReceiptStatus.Sent or TaxReceiptStatus.EmailFailed))
            {
                throw new BusinessException("TaxReceiptPdfNotGenerated");
            }

            var currentUser = currentUserId.HasValue
                ? await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == currentUserId.Value, cancellationToken)
                : null;

            EnsureAccess(receipt, currentUser, currentUserId, canAccessAllReceipts);
            var recipient = ResolveRecipient(receipt, dto, currentUser, canAccessAllReceipts);
            var subject = string.IsNullOrWhiteSpace(dto.Subject)
                ? $"Votre reçu fiscal {receipt.ReceiptNumber} - Life"
                : dto.Subject.Trim();
            var logoBytes = TryReadLogo();
            var (plainTextBody, htmlBody) = BuildBody(receipt, logoBytes is not null);
            var body = string.IsNullOrWhiteSpace(dto.Body) ? plainTextBody : dto.Body;
            var attemptNumber = await _db.TaxReceiptEmailHistory
                .CountAsync(x => x.TaxReceiptId == receipt.Id, cancellationToken) + 1;

            var history = new TaxReceiptEmailHistory
            {
                TaxReceiptId = receipt.Id,
                RecipientEmail = recipient.Address,
                Subject = subject,
                Status = TaxReceiptEmailStatus.Pending,
                RetryCount = attemptNumber
            };

            _db.TaxReceiptEmailHistory.Add(history);
            receipt.Status = TaxReceiptStatus.Pending;
            receipt.LastEmailStatus = TaxReceiptEmailStatus.Pending;
            receipt.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Envoi reçu fiscal démarré. CorrelationId={CorrelationId}, ReceiptId={ReceiptId}, ReceiptNumber={ReceiptNumber}, DonationId={DonationId}, Recipient={Recipient}, Attempt={Attempt}, User={User}",
                correlationId,
                receipt.Id,
                receipt.ReceiptNumber,
                receipt.DonationId,
                MaskEmail(recipient.Address),
                attemptNumber,
                userName ?? "system");

            try
            {
                var artifact = receipt.DocumentArtifact ?? throw new BusinessException("TaxReceiptPdfNotGenerated");
                var pdf = await _storage.ReadAsync(artifact.StorageKey, cancellationToken);
                var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pdf));
                if (!string.Equals(hash, receipt.PdfHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessException("TaxReceiptPdfHashMismatch");
                }

                using var message = new MailMessage
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                message.To.Add(recipient);
                if (string.IsNullOrWhiteSpace(dto.Body))
                {
                    message.AlternateViews.Add(
                        AlternateView.CreateAlternateViewFromString(plainTextBody, Encoding.UTF8, MediaTypeNames.Text.Plain));
                    var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html);
                    if (logoBytes is not null)
                    {
                        htmlView.LinkedResources.Add(new LinkedResource(new MemoryStream(logoBytes), MediaTypeNames.Image.Png)
                        {
                            ContentId = "life-ssbd-logo",
                            TransferEncoding = TransferEncoding.Base64
                        });
                    }

                    message.AlternateViews.Add(htmlView);
                }

                message.Attachments.Add(new Attachment(
                    new MemoryStream(pdf),
                    BuildAttachmentFileName(receipt),
                    MediaTypeNames.Application.Pdf));

                var sendResult = await _mailSender.SendAsync(message, "tax-receipt", cancellationToken);
                if (!sendResult.Success)
                {
                    var error = LimitError(sendResult.ErrorMessage ?? sendResult.ErrorType ?? "EmailSendFailed");
                    history.Status = TaxReceiptEmailStatus.Failed;
                    history.ErrorMessage = error;
                    receipt.Status = TaxReceiptStatus.EmailFailed;
                    receipt.LastEmailStatus = TaxReceiptEmailStatus.Failed;
                    receipt.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);

                    _logger.LogWarning(
                        "Envoi reçu fiscal échoué. CorrelationId={CorrelationId}, ReceiptId={ReceiptId}, ReceiptNumber={ReceiptNumber}, DonationId={DonationId}, Recipient={Recipient}, ErrorType={ErrorType}, Error={Error}",
                        correlationId,
                        receipt.Id,
                        receipt.ReceiptNumber,
                        receipt.DonationId,
                        MaskEmail(recipient.Address),
                        sendResult.ErrorType ?? "SmtpFailure",
                        error);

                    return BuildResult(false, receipt, history, UserFriendlyFailureMessage);
                }

                history.Status = TaxReceiptEmailStatus.Sent;
                history.SentAt = DateTime.UtcNow;
                history.ErrorMessage = null;
                receipt.Status = TaxReceiptStatus.Sent;
                receipt.SentAt = history.SentAt;
                receipt.SentToEmail = recipient.Address;
                receipt.LastEmailStatus = TaxReceiptEmailStatus.Sent;
                receipt.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Envoi reçu fiscal accepté par SMTP. CorrelationId={CorrelationId}, ReceiptId={ReceiptId}, ReceiptNumber={ReceiptNumber}, DonationId={DonationId}, Recipient={Recipient}, Attachment={Attachment}",
                    correlationId,
                    receipt.Id,
                    receipt.ReceiptNumber,
                    receipt.DonationId,
                    MaskEmail(recipient.Address),
                    BuildAttachmentFileName(receipt));

                return BuildResult(
                    true,
                    receipt,
                    history,
                    $"Le reçu fiscal {receipt.ReceiptNumber} a été envoyé à votre adresse e-mail.");
            }
            catch (Exception ex) when (ex is not BusinessException)
            {
                var error = LimitError(ex.Message);
                history.Status = TaxReceiptEmailStatus.Failed;
                history.ErrorMessage = error;
                receipt.Status = TaxReceiptStatus.EmailFailed;
                receipt.LastEmailStatus = TaxReceiptEmailStatus.Failed;
                receipt.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogError(
                    ex,
                    "Envoi reçu fiscal échoué avec exception. CorrelationId={CorrelationId}, ReceiptId={ReceiptId}, ReceiptNumber={ReceiptNumber}, DonationId={DonationId}, Recipient={Recipient}, ExceptionType={ExceptionType}",
                    correlationId,
                    receipt.Id,
                    receipt.ReceiptNumber,
                    receipt.DonationId,
                    MaskEmail(recipient.Address),
                    ex.GetType().Name);

                return BuildResult(false, receipt, history, UserFriendlyFailureMessage);
            }
        }

        private static void EnsureAccess(TaxReceipt receipt, User? currentUser, int? currentUserId, bool canAccessAllReceipts)
        {
            if (!currentUserId.HasValue)
            {
                return;
            }

            if (currentUser is null)
            {
                throw new BusinessException("UserNotFound");
            }

            if (!currentUser.EmailConfirmed)
            {
                throw new BusinessException("EmailNotConfirmed");
            }

            if (canAccessAllReceipts)
            {
                return;
            }

            var ownsReceipt = receipt.Donation.UserId == currentUserId.Value
                || receipt.Donation.Donor.UserId == currentUserId.Value
                || receipt.Donation.DonorSnapshot?.UserId == currentUserId.Value;

            if (!ownsReceipt)
            {
                throw new BusinessException("TaxReceiptForbidden");
            }
        }

        private static MailAddress ResolveRecipient(
            TaxReceipt receipt,
            SendTaxReceiptEmailDto dto,
            User? currentUser,
            bool canAccessAllReceipts)
        {
            var resolved = currentUser is not null && !canAccessAllReceipts
                ? currentUser.Email
                : FirstNonEmpty(
                    dto.RecipientEmail,
                    currentUser?.Email,
                    receipt.Donation.DonorSnapshot?.Email,
                    receipt.Donation.Donor.Email);

            if (string.IsNullOrWhiteSpace(resolved))
            {
                throw new BusinessException("EmailMissing");
            }

            try
            {
                return new MailAddress(resolved.Trim());
            }
            catch (FormatException)
            {
                throw new BusinessException("EmailInvalid");
            }
        }

        private byte[]? TryReadLogo()
        {
            var logoPath = Path.Combine(_environment.ContentRootPath, "Assets", "LogoSSBD.png");
            if (!File.Exists(logoPath))
            {
                _logger.LogWarning("Logo e-mail Life introuvable. Path={LogoPath}", logoPath);
                return null;
            }

            return File.ReadAllBytes(logoPath);
        }

        private static (string PlainText, string Html) BuildBody(TaxReceipt receipt, bool includeLogo)
        {
            var culture = CultureInfo.GetCultureInfo("fr-FR");
            var donorName = FirstNonEmpty(
                receipt.Donation.DonorSnapshot?.FirstName,
                receipt.Donation.Donor.FirstName,
                receipt.Donation.DonorSnapshot?.LastName,
                receipt.Donation.Donor.LastName,
                "Madame, Monsieur")!;
            var donorLastName = FirstNonEmpty(
                receipt.Donation.DonorSnapshot?.LastName,
                receipt.Donation.Donor.LastName,
                donorName)!;
            var amount = receipt.Donation.Amount.ToString("C", culture);
            var donationDate = receipt.Donation.DonationDate.ToString("dd MMMM yyyy", culture);
            var receiptNumber = receipt.ReceiptNumber;

            var plain = $"""
                Bonjour {donorName},

                Nous vous remercions très sincèrement pour votre don de {amount} effectué le {donationDate}.

                Votre reçu fiscal n° {receiptNumber} est joint à ce message au format PDF. Ce document atteste de votre don et peut être conservé avec vos justificatifs fiscaux.

                Le reçu reste également disponible dans votre espace personnel Life, rubrique « Reçus fiscaux », afin que vous puissiez le retrouver ou le télécharger de nouveau à tout moment.

                Récapitulatif :
                - Donateur : {donorName} {donorLastName}
                - Montant du don : {amount}
                - Date du don : {donationDate}
                - Numéro du reçu fiscal : {receiptNumber}

                Si vous constatez une erreur dans les informations figurant sur le reçu, merci de nous contacter avant toute utilisation administrative du document.

                Cordialement,

                L'équipe Life
                Software Superior by Design
                """;

            var logoHtml = includeLogo
                ? """
                  <div style="margin:0 0 24px 0;">
                    <img src="cid:life-ssbd-logo" alt="SSBD - Software Superior by Design" width="240" style="display:block;width:240px;max-width:100%;height:auto;border:0;" />
                  </div>
                  """
                : """
                  <div style="font-size:18px;line-height:1.3;font-weight:700;color:#0f2f5f;margin-bottom:24px;">
                    SSBD<br><span style="font-size:13px;font-weight:500;color:#25303b;">Software Superior by Design</span>
                  </div>
                  """;
            var encodedDonorName = System.Net.WebUtility.HtmlEncode(donorName);
            var encodedDonorFullName = System.Net.WebUtility.HtmlEncode($"{donorName} {donorLastName}".Trim());
            var encodedAmount = System.Net.WebUtility.HtmlEncode(amount);
            var encodedDonationDate = System.Net.WebUtility.HtmlEncode(donationDate);
            var encodedReceiptNumber = System.Net.WebUtility.HtmlEncode(receiptNumber);

            var html = $"""
                <!doctype html>
                <html lang="fr">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>Votre reçu fiscal {encodedReceiptNumber}</title>
                </head>
                <body style="margin:0;padding:0;background:#f4f7fb;font-family:Arial,Helvetica,sans-serif;color:#152238;">
                  <div style="max-width:680px;margin:0 auto;padding:28px 20px;">
                    <div style="background:#ffffff;border:1px solid #d9e2ef;border-radius:8px;padding:28px;">
                      {logoHtml}
                      <p style="margin:0 0 16px 0;font-size:16px;line-height:1.55;">Bonjour {encodedDonorName},</p>
                      <p style="margin:0 0 16px 0;font-size:16px;line-height:1.55;">
                        Nous vous remercions très sincèrement pour votre don de <strong>{encodedAmount}</strong> effectué le <strong>{encodedDonationDate}</strong>.
                      </p>
                      <p style="margin:0 0 16px 0;font-size:16px;line-height:1.55;">
                        Votre reçu fiscal <strong>n° {encodedReceiptNumber}</strong> est joint à ce message au format PDF. Ce document atteste de votre don et peut être conservé avec vos justificatifs fiscaux.
                      </p>
                      <p style="margin:0 0 22px 0;font-size:16px;line-height:1.55;">
                        Le reçu reste également disponible dans votre espace personnel Life, rubrique <strong>Reçus fiscaux</strong>, afin que vous puissiez le retrouver ou le télécharger de nouveau à tout moment.
                      </p>
                      <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:6px;padding:16px 18px;margin:0 0 22px 0;">
                        <p style="margin:0 0 10px 0;font-size:14px;font-weight:700;color:#0f2f5f;">Récapitulatif du reçu</p>
                        <p style="margin:0;font-size:14px;line-height:1.7;">
                          Donateur : <strong>{encodedDonorFullName}</strong><br>
                          Montant du don : <strong>{encodedAmount}</strong><br>
                          Date du don : <strong>{encodedDonationDate}</strong><br>
                          Numéro du reçu fiscal : <strong>{encodedReceiptNumber}</strong>
                        </p>
                      </div>
                      <p style="margin:0 0 22px 0;font-size:14px;line-height:1.55;color:#475569;">
                        Si vous constatez une erreur dans les informations figurant sur le reçu, merci de nous contacter avant toute utilisation administrative du document.
                      </p>
                      <p style="margin:0;font-size:16px;line-height:1.55;">
                        Cordialement,<br>
                        <strong>L'équipe Life</strong><br>
                        <span style="color:#64748b;">Software Superior by Design</span>
                      </p>
                    </div>
                  </div>
                </body>
                </html>
                """;

            return (plain, html);
        }

        private static TaxReceiptEmailSendResultDto BuildResult(
            bool success,
            TaxReceipt receipt,
            TaxReceiptEmailHistory history,
            string message) =>
            new(
                success,
                receipt.Id,
                receipt.ReceiptNumber,
                receipt.Status,
                history.Status,
                history.SentAt,
                history.RecipientEmail,
                message,
                history.ToDto());

        private static string BuildAttachmentFileName(TaxReceipt receipt)
        {
            var fileName = $"Recu-fiscal-{receipt.ReceiptNumber}.pdf";
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalid, '-');
            }

            return fileName;
        }

        private static string? FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

        private static string LimitError(string error) =>
            error.Length <= 1000 ? error : error[..1000];

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
