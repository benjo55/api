using api.Configuration;
using api.Data;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Services.Payments
{
    public sealed class PaymentReconciliationService : IPaymentReconciliationService
    {
        private readonly ApplicationDBContext _db;
        private readonly IPaymentProvider _helloAssoProvider;
        private readonly HelloAssoOptions _helloAssoOptions;
        private readonly IDonationPaidProcessor _paidProcessor;

        public PaymentReconciliationService(
            ApplicationDBContext db,
            IPaymentProvider helloAssoProvider,
            IOptions<HelloAssoOptions> helloAssoOptions,
            IDonationPaidProcessor paidProcessor)
        {
            _db = db;
            _helloAssoProvider = helloAssoProvider;
            _helloAssoOptions = helloAssoOptions.Value;
            _paidProcessor = paidProcessor;
        }

        public async Task ReconcileHelloAssoAttemptAsync(int paymentAttemptId, CancellationToken cancellationToken)
        {
            var attempt = await LoadAttemptAsync(paymentAttemptId, cancellationToken)
                ?? throw new BusinessException("Tentative de paiement introuvable.");

            await ReconcileHelloAssoAttemptAsync(attempt, cancellationToken);
        }

        public async Task ReconcileHelloAssoCheckoutAsync(string checkoutIntentId, CancellationToken cancellationToken)
        {
            var attempt = await _db.PaymentAttempts
                .Include(x => x.Donation)
                    .ThenInclude(x => x.Organization)
                .FirstOrDefaultAsync(x =>
                    x.Provider == PaymentProvider.HelloAsso
                    && x.ProviderCheckoutIntentId == checkoutIntentId,
                    cancellationToken)
                ?? throw new BusinessException("Tentative de paiement HelloAsso inconnue.");

            await ReconcileHelloAssoAttemptAsync(attempt, cancellationToken);
        }

        private async Task ReconcileHelloAssoAttemptAsync(PaymentAttempt attempt, CancellationToken cancellationToken)
        {
            var donation = attempt.Donation;
            var organizationSlug = donation.Organization.HelloAssoOrganizationSlug ?? _helloAssoOptions.OrganizationSlug;
            if (string.IsNullOrWhiteSpace(organizationSlug) || string.IsNullOrWhiteSpace(attempt.ProviderCheckoutIntentId))
            {
                throw new BusinessException("Configuration HelloAsso incomplete pour la reconciliation.");
            }

            var reconciliation = await _helloAssoProvider.ReconcilePaymentAsync(
                new PaymentReconciliationCommand(
                    organizationSlug,
                    attempt.ProviderCheckoutIntentId,
                    ResolveCredentialKey(donation.Organization)),
                cancellationToken);

            attempt.LastReconciledAt = DateTime.UtcNow;
            attempt.UpdatedAt = DateTime.UtcNow;

            if (!reconciliation.Found)
            {
                attempt.PaymentStatus = PaymentStatus.Processing;
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            ValidateReconciliation(donation, attempt, reconciliation);

            attempt.ProviderOrderId = reconciliation.ExternalOrderId;
            attempt.ProviderPaymentId = reconciliation.ExternalPaymentId;
            attempt.ProviderPaymentState = reconciliation.ProviderPaymentState;
            attempt.PaymentStatus = HelloAssoPaymentStatusMapper.Map(reconciliation.ProviderPaymentState);

            if (reconciliation.IsAuthorized)
            {
                var now = DateTime.UtcNow;
                attempt.PaymentStatus = PaymentStatus.Succeeded;
                attempt.AuthorizedAt ??= now;
                attempt.PaidAt ??= now;
                donation.PaymentConfirmedAt ??= now;
                donation.ConfirmedPaymentProvider = PaymentProvider.HelloAsso;
                donation.PaymentMethod = DonationPaymentMethod.BankCard;
                donation.Status = DonationStatus.Paid;
                donation.UpdatedAt = now;
                await _db.SaveChangesAsync(cancellationToken);
                await _paidProcessor.ProcessAsync(donation.Id, "helloasso-reconciliation", cancellationToken);
                return;
            }

            if (attempt.PaymentStatus is PaymentStatus.Refused or PaymentStatus.Failed or PaymentStatus.Cancelled or PaymentStatus.Expired)
            {
                attempt.FailedAt ??= DateTime.UtcNow;
                donation.Status = DonationStatus.Failed;
            }
            else
            {
                attempt.PaymentStatus = PaymentStatus.Processing;
                donation.Status = DonationStatus.PaymentPending;
            }

            donation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<PaymentAttempt?> LoadAttemptAsync(int paymentAttemptId, CancellationToken cancellationToken)
        {
            return await _db.PaymentAttempts
                .Include(x => x.Donation)
                    .ThenInclude(x => x.Organization)
                .FirstOrDefaultAsync(x => x.Id == paymentAttemptId, cancellationToken);
        }

        private string? ResolveCredentialKey(BeneficiaryOrganization organization)
        {
            if (!string.IsNullOrWhiteSpace(organization.HelloAssoCredentialKey))
            {
                return organization.HelloAssoCredentialKey;
            }

            return _helloAssoOptions.Credentials.Count == 1
                ? _helloAssoOptions.Credentials.Keys.Single()
                : null;
        }

        private static void ValidateReconciliation(Donation donation, PaymentAttempt attempt, PaymentReconciliationResult reconciliation)
        {
            if (reconciliation.Metadata.TryGetValue("donationId", out var donationIdValue)
                && int.TryParse(donationIdValue, out var donationId)
                && donationId != donation.Id)
            {
                throw new BusinessException("Metadonnees HelloAsso incoherentes.");
            }

            if (reconciliation.Metadata.TryGetValue("paymentAttemptId", out var attemptIdValue)
                && int.TryParse(attemptIdValue, out var attemptId)
                && attemptId != attempt.Id)
            {
                throw new BusinessException("Metadonnees tentative de paiement incoherentes.");
            }

            if (reconciliation.AmountInCents is null
                || HelloAssoAmountConverter.CentsToEuro(reconciliation.AmountInCents.Value) != donation.Amount)
            {
                throw new BusinessException("Montant HelloAsso incoherent.");
            }

            if (!string.IsNullOrWhiteSpace(reconciliation.Currency)
                && !string.Equals(reconciliation.Currency, donation.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException("Devise HelloAsso incoherente.");
            }
        }
    }
}
