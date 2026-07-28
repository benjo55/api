using api.Data;
using api.Dtos.TaxReceipts;
using api.Exceptions;
using api.Interfaces;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Payments
{
    public sealed class DonationPaidProcessor : IDonationPaidProcessor
    {
        private readonly ApplicationDBContext _db;
        private readonly ITaxReceiptService _taxReceiptService;
        private readonly ITaxReceiptEmailService _taxReceiptEmailService;
        private readonly ILogger<DonationPaidProcessor> _logger;

        public DonationPaidProcessor(
            ApplicationDBContext db,
            ITaxReceiptService taxReceiptService,
            ITaxReceiptEmailService taxReceiptEmailService,
            ILogger<DonationPaidProcessor> logger)
        {
            _db = db;
            _taxReceiptService = taxReceiptService;
            _taxReceiptEmailService = taxReceiptEmailService;
            _logger = logger;
        }

        public async Task ProcessAsync(int donationId, string actor, CancellationToken cancellationToken)
        {
            var donation = await _db.Donations
                .Include(x => x.Organization)
                .Include(x => x.TaxReceipts)
                .FirstOrDefaultAsync(x => x.Id == donationId, cancellationToken)
                ?? throw new BusinessException("Don introuvable.");

            if (donation.PostPaymentProcessedAt is not null)
            {
                return;
            }

            if (donation.PaymentConfirmedAt is null)
            {
                throw new BusinessException("Le paiement du don n'est pas confirme.");
            }

            try
            {
                if (!donation.Organization.IsEligibleForTaxReceipt)
                {
                    donation.Status = DonationStatus.Completed;
                    donation.PostPaymentProcessedAt = DateTime.UtcNow;
                    donation.PostPaymentProcessingError = null;
                    donation.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                    return;
                }

                var existing = donation.TaxReceipts
                    .Where(x => x.Status is TaxReceiptStatus.Generated or TaxReceiptStatus.Sent or TaxReceiptStatus.EmailFailed)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

                if (existing is null)
                {
                    var created = await _taxReceiptService.CreateForDonationAsync(
                        donation.Id,
                        new CreateTaxReceiptDto(
                            donation.OrganizationId,
                            "2041-RD",
                            "11580*05",
                            $"paid-{donation.PublicId}"),
                        actor,
                        cancellationToken);

                    var generation = await _taxReceiptService.GenerateAsync(created.Id, actor, cancellationToken);
                    existing = await _db.TaxReceipts.FirstAsync(x => x.Id == generation.Receipt.Id, cancellationToken);
                }

                await _taxReceiptEmailService.SendAsync(existing.Id, new SendTaxReceiptEmailDto(null, null, null), actor, cancellationToken);

                donation.Status = DonationStatus.Completed;
                donation.PostPaymentProcessedAt = DateTime.UtcNow;
                donation.PostPaymentProcessingError = null;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                donation.PostPaymentProcessingError = ex.Message;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogError(ex, "Echec du traitement post-paiement du don {DonationId}", donationId);
            }
        }
    }
}
