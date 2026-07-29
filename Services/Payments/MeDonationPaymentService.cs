using api.Configuration;
using api.Data;
using api.Dtos.Me;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace api.Services.Payments
{
    public sealed class MeDonationPaymentService : IMeDonationPaymentService
    {
        private static readonly PaymentStatus[] ReusableHelloAssoStatuses =
        [
            PaymentStatus.RedirectRequired,
            PaymentStatus.RedirectReady,
            PaymentStatus.Pending,
            PaymentStatus.Processing
        ];

        private readonly ApplicationDBContext _db;
        private readonly IPaymentProvider _helloAssoProvider;
        private readonly IPaymentReconciliationService _reconciliationService;
        private readonly IBankAccountProtector _bankAccountProtector;
        private readonly HelloAssoOptions _helloAssoOptions;
        private readonly PaymentsOptions _paymentsOptions;

        public MeDonationPaymentService(
            ApplicationDBContext db,
            IPaymentProvider helloAssoProvider,
            IPaymentReconciliationService reconciliationService,
            IBankAccountProtector bankAccountProtector,
            IOptions<HelloAssoOptions> helloAssoOptions,
            IOptions<PaymentsOptions> paymentsOptions)
        {
            _db = db;
            _helloAssoProvider = helloAssoProvider;
            _reconciliationService = reconciliationService;
            _bankAccountProtector = bankAccountProtector;
            _helloAssoOptions = helloAssoOptions.Value;
            _paymentsOptions = paymentsOptions.Value;
        }

        public async Task<MeDonationPaymentOptionsDto?> GetPaymentOptionsAsync(int userId, string publicId, CancellationToken cancellationToken)
        {
            var donation = await LoadScopedDonationAsync(userId, publicId, cancellationToken);
            if (donation is null)
            {
                return null;
            }

            return MapOptions(donation);
        }

        public async Task<MeHelloAssoPaymentStartedDto?> StartHelloAssoPaymentAsync(int userId, string publicId, CancellationToken cancellationToken)
        {
            var donation = await LoadScopedDonationAsync(userId, publicId, cancellationToken);
            if (donation is null)
            {
                return null;
            }

            EnsurePayableDonation(donation);
            var organizationSlug = ResolveHelloAssoSlug(donation.Organization);
            if (!IsHelloAssoAvailable(donation.Organization, organizationSlug))
            {
                throw new BusinessException("Le paiement HelloAsso n'est pas disponible pour cet organisme.");
            }

            var now = DateTime.UtcNow;
            var reusable = donation.PaymentAttempts
                .Where(x => x.Provider == PaymentProvider.HelloAsso
                    && x.RedirectUrl != null
                    && ReusableHelloAssoStatuses.Contains(x.PaymentStatus)
                    && (x.CheckoutUrlExpiresAt == null || x.CheckoutUrlExpiresAt > now))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            if (reusable is not null)
            {
                return new MeHelloAssoPaymentStartedDto(
                    donation.PublicId,
                    donation.Reference ?? string.Empty,
                    reusable.Id,
                    reusable.InternalReference,
                    reusable.RedirectUrl!,
                    reusable.CheckoutUrlExpiresAt);
            }

            foreach (var stale in donation.PaymentAttempts.Where(x =>
                x.Provider == PaymentProvider.HelloAsso
                && ReusableHelloAssoStatuses.Contains(x.PaymentStatus)
                && x.CheckoutUrlExpiresAt is not null
                && x.CheckoutUrlExpiresAt <= now))
            {
                stale.PaymentStatus = PaymentStatus.Expired;
                stale.ExpiredAt = now;
                stale.UpdatedAt = now;
            }

            var attempt = new PaymentAttempt
            {
                DonationId = donation.Id,
                Provider = PaymentProvider.HelloAsso,
                InternalReference = await GeneratePaymentReferenceAsync(cancellationToken),
                IdempotencyKey = $"helloasso:{donation.PublicId}:{Guid.NewGuid():N}",
                Amount = donation.Amount,
                Currency = donation.Currency,
                PaymentStatus = PaymentStatus.Created,
                CheckoutUrlExpiresAt = now.AddHours(24),
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.PaymentAttempts.Add(attempt);
            await _db.SaveChangesAsync(cancellationToken);

            var donor = donation.DonorSnapshot;
            var birthDate = donor?.BirthDate ?? donation.Donor.BirthDate;
            var credentialKey = ResolveCredentialKey(donation.Organization);
            var metadata = new Dictionary<string, string>
            {
                ["flow"] = "my-space",
                ["donationId"] = donation.Id.ToString(),
                ["paymentAttemptId"] = attempt.Id.ToString(),
                ["donationReference"] = donation.Reference ?? string.Empty,
                ["donationPublicId"] = donation.PublicId,
                ["paymentReference"] = attempt.InternalReference,
            };

            var checkout = await _helloAssoProvider.CreateCheckoutAsync(
                new CreateCheckoutCommand(
                    organizationSlug,
                    HelloAssoAmountConverter.EuroToCents(donation.Amount),
                    $"Don {donation.Reference ?? donation.PublicId}",
                    BuildFrontendUrl("/my-space/donations/payment/helloasso/return", donation.PublicId),
                    BuildFrontendUrl("/my-space/donations/payment/helloasso/back", donation.PublicId),
                    BuildFrontendUrl("/my-space/donations/payment/helloasso/error", donation.PublicId),
                    donor?.FirstName ?? donation.Donor.FirstName,
                    donor?.LastName ?? donation.Donor.LastName,
                    donor?.Email ?? donation.Donor.Email ?? donation.User?.Email ?? string.Empty,
                    donor?.AddressLine1 ?? donation.Donor.AddressLine1,
                    donor?.PostalCode ?? donation.Donor.PostalCode,
                    donor?.City ?? donation.Donor.City,
                    donor?.Country ?? donation.Donor.CountryCode,
                    birthDate,
                    metadata,
                    credentialKey),
                cancellationToken);

            if (!checkout.Success || string.IsNullOrWhiteSpace(checkout.RedirectUrl))
            {
                var failureMessage = BuildHelloAssoCheckoutFailureMessage(checkout);
                attempt.PaymentStatus = PaymentStatus.Failed;
                attempt.FailedAt = now;
                attempt.FailureCode = checkout.ErrorCode;
                attempt.FailureMessage = Truncate(failureMessage, 1000);
                donation.Status = DonationStatus.AwaitingPayment;
                donation.UpdatedAt = now;
                await _db.SaveChangesAsync(cancellationToken);
                throw new BusinessException(failureMessage);
            }

            attempt.ProviderCheckoutIntentId = checkout.CheckoutIntentId;
            attempt.RedirectUrl = checkout.RedirectUrl;
            attempt.PaymentStatus = PaymentStatus.RedirectRequired;
            attempt.UpdatedAt = DateTime.UtcNow;
            donation.Status = DonationStatus.PaymentPending;
            donation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return new MeHelloAssoPaymentStartedDto(
                donation.PublicId,
                donation.Reference ?? string.Empty,
                attempt.Id,
                attempt.InternalReference,
                checkout.RedirectUrl,
                attempt.CheckoutUrlExpiresAt);
        }

        public async Task<MeBankTransferInstructionsDto?> StartBankTransferAsync(int userId, string publicId, CancellationToken cancellationToken)
        {
            var donation = await LoadScopedDonationAsync(userId, publicId, cancellationToken);
            if (donation is null)
            {
                return null;
            }

            EnsurePayableDonation(donation);
            var bankAccount = ResolveActiveBankAccount(donation.Organization);
            if (bankAccount is null)
            {
                throw new BusinessException("Le virement bancaire n'est pas disponible pour cet organisme.");
            }

            var now = DateTime.UtcNow;
            var attempt = donation.PaymentAttempts
                .Where(x => x.Provider == PaymentProvider.BankTransfer
                    && x.PaymentStatus is PaymentStatus.Pending or PaymentStatus.Created or PaymentStatus.Processing)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            if (attempt is null)
            {
                attempt = new PaymentAttempt
                {
                    DonationId = donation.Id,
                    Provider = PaymentProvider.BankTransfer,
                    InternalReference = await GeneratePaymentReferenceAsync(cancellationToken),
                    IdempotencyKey = $"bank:{donation.PublicId}",
                    Amount = donation.Amount,
                    Currency = donation.Currency,
                    PaymentStatus = PaymentStatus.Pending,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _db.PaymentAttempts.Add(attempt);
            }

            donation.PaymentMethod = DonationPaymentMethod.BankTransfer;
            donation.Status = DonationStatus.PaymentPending;
            donation.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);

            return MapBankInstructions(donation, attempt, bankAccount);
        }

        public async Task<MeDonationPaymentStatusDto?> GetPaymentStatusAsync(int userId, string publicId, CancellationToken cancellationToken)
        {
            var donation = await LoadScopedDonationAsync(userId, publicId, cancellationToken);
            if (donation is null)
            {
                return null;
            }

            var attempt = LatestAttempt(donation);
            if (attempt is not null && ShouldReconcileHelloAssoAttempt(attempt))
            {
                await _reconciliationService.ReconcileHelloAssoAttemptAsync(attempt.Id, cancellationToken);
                donation = await LoadScopedDonationAsync(userId, publicId, cancellationToken) ?? donation;
                attempt = LatestAttempt(donation);
            }

            return MapStatus(donation, attempt, null);
        }

        public async Task<MeDonationPaymentStatusDto?> DeclareBankTransferAsync(int userId, string publicId, DeclareBankTransferDto dto, CancellationToken cancellationToken)
        {
            var donation = await LoadScopedDonationAsync(userId, publicId, cancellationToken);
            if (donation is null)
            {
                return null;
            }

            var attempt = donation.PaymentAttempts
                .Where(x => x.Provider == PaymentProvider.BankTransfer
                    && x.PaymentStatus is PaymentStatus.Pending or PaymentStatus.Created or PaymentStatus.Processing)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault()
                ?? throw new BusinessException("Aucun virement bancaire n'est en attente pour ce don.");

            attempt.PaymentStatus = PaymentStatus.Processing;
            attempt.DonorTransferDeclaredAt = DateTime.UtcNow;
            attempt.DonorTransferDeclarationComment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim();
            attempt.UpdatedAt = DateTime.UtcNow;
            donation.Status = DonationStatus.PaymentPending;
            donation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return MapStatus(donation, attempt, "Votre declaration de virement a ete enregistree. Le don sera confirme apres rapprochement administratif.");
        }

        private async Task<Donation?> LoadScopedDonationAsync(int userId, string publicId, CancellationToken cancellationToken)
        {
            return await _db.Donations
                .Include(x => x.User)
                .Include(x => x.Donor)
                .Include(x => x.DonorSnapshot)
                .Include(x => x.Organization)
                    .ThenInclude(x => x.BankAccounts)
                .Include(x => x.PaymentAttempts)
                .Include(x => x.TaxReceipts)
                .FirstOrDefaultAsync(x =>
                    x.PublicId == publicId
                    && (x.UserId == userId
                        || x.Donor.UserId == userId
                        || (x.DonorSnapshot != null && x.DonorSnapshot.UserId == userId)),
                    cancellationToken);
        }

        private MeDonationPaymentOptionsDto MapOptions(Donation donation)
        {
            var organizationSlug = ResolveHelloAssoSlug(donation.Organization);
            var isPayable = IsPayable(donation);
            var bankAccount = ResolveActiveBankAccount(donation.Organization);

            return new MeDonationPaymentOptionsDto(
                donation.PublicId,
                donation.Reference ?? string.Empty,
                donation.Amount,
                donation.Currency,
                donation.Status.ToString(),
                isPayable,
                isPayable && IsHelloAssoAvailable(donation.Organization, organizationSlug),
                isPayable && bankAccount is not null,
                false,
                false,
                isPayable ? null : "Ce don n'est plus payable.");
        }

        private MeBankTransferInstructionsDto MapBankInstructions(Donation donation, PaymentAttempt attempt, OrganizationBankAccount bankAccount)
        {
            var iban = _bankAccountProtector.Unprotect(bankAccount.EncryptedIban);
            var bic = _bankAccountProtector.Unprotect(bankAccount.EncryptedBic);
            return new MeBankTransferInstructionsDto(
                donation.PublicId,
                donation.Reference ?? string.Empty,
                attempt.Id,
                attempt.InternalReference,
                donation.Amount,
                donation.Currency,
                bankAccount.AccountHolder,
                iban,
                bic,
                bankAccount.BankName,
                bankAccount.CountryCode,
                bankAccount.Instructions,
                attempt.InternalReference);
        }

        private static MeDonationPaymentStatusDto MapStatus(Donation donation, PaymentAttempt? attempt, string? message)
        {
            var receipt = donation.TaxReceipts
                .Where(x => x.Status is TaxReceiptStatus.Generated or TaxReceiptStatus.Sent or TaxReceiptStatus.EmailFailed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            return new MeDonationPaymentStatusDto(
                donation.PublicId,
                donation.Reference ?? string.Empty,
                donation.Status.ToString(),
                attempt?.Provider.ToString(),
                attempt?.PaymentStatus.ToString(),
                donation.PaymentConfirmedAt,
                receipt is not null,
                receipt?.ReceiptNumber,
                message);
        }

        private static PaymentAttempt? LatestAttempt(Donation donation) =>
            donation.PaymentAttempts.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

        private static bool ShouldReconcileHelloAssoAttempt(PaymentAttempt attempt) =>
            attempt.Provider == PaymentProvider.HelloAsso
            && !string.IsNullOrWhiteSpace(attempt.ProviderCheckoutIntentId)
            && attempt.PaymentStatus is PaymentStatus.RedirectRequired
                or PaymentStatus.RedirectReady
                or PaymentStatus.Pending
                or PaymentStatus.Processing
                or PaymentStatus.Authorized;

        private OrganizationBankAccount? ResolveActiveBankAccount(BeneficiaryOrganization organization)
        {
            if (!organization.IsBankTransferEnabled)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            return organization.BankAccounts
                .Where(x => x.IsActive && x.IsVerified && x.ValidFrom <= now && (x.ValidTo == null || x.ValidTo > now))
                .OrderByDescending(x => x.ValidFrom)
                .FirstOrDefault();
        }

        private bool IsHelloAssoAvailable(BeneficiaryOrganization organization, string organizationSlug)
        {
            var hasCredentials = _helloAssoOptions.HasCredential(organization.HelloAssoCredentialKey)
                || (!string.IsNullOrWhiteSpace(ResolveCredentialKey(organization))
                    && _helloAssoOptions.HasCredential(ResolveCredentialKey(organization)))
                || _helloAssoOptions.HasGlobalCredentials;

            return (_helloAssoOptions.Enabled || _helloAssoOptions.HasAnyCredentials)
                && !string.IsNullOrWhiteSpace(organizationSlug)
                && hasCredentials
                && (organization.IsHelloAssoPaymentEnabled
                    || !string.IsNullOrWhiteSpace(organization.HelloAssoOrganizationSlug)
                    || !string.IsNullOrWhiteSpace(_helloAssoOptions.OrganizationSlug));
        }

        private string ResolveHelloAssoSlug(BeneficiaryOrganization organization) =>
            organization.HelloAssoOrganizationSlug ?? _helloAssoOptions.OrganizationSlug;

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

        private static void EnsurePayableDonation(Donation donation)
        {
            if (!IsPayable(donation))
            {
                throw new BusinessException("Ce don n'est plus payable.");
            }

            if (!donation.Organization.IsActive || !donation.Organization.IsDonationEnabled)
            {
                throw new BusinessException("L'organisme selectionne n'est plus disponible pour les dons.");
            }
        }

        private static bool IsPayable(Donation donation) =>
            donation.PaymentConfirmedAt is null
            && !donation.IsCancelled
            && donation.Status is DonationStatus.AwaitingPayment or DonationStatus.PaymentPending or DonationStatus.Failed;

        private string BuildFrontendUrl(string path, string publicId)
        {
            var baseUrl = _paymentsOptions.PublicBaseUrl.TrimEnd('/');
            var separator = path.Contains('?') ? "&" : "?";
            return $"{baseUrl}{path}{separator}donation={Uri.EscapeDataString(publicId)}";
        }

        private async Task<string> GeneratePaymentReferenceAsync(CancellationToken cancellationToken)
        {
            var prefix = $"PAY-{DateTime.UtcNow:yyyyMMdd}-";
            var count = await _db.PaymentAttempts.CountAsync(x => x.InternalReference.StartsWith(prefix), cancellationToken);
            return $"{prefix}{count + 1:000000}";
        }

        private static string BuildHelloAssoCheckoutFailureMessage(CreateCheckoutResult checkout)
        {
            var detail = ExtractHelloAssoErrorDetail(checkout.RawTechnicalPayload)
                ?? checkout.ErrorMessage
                ?? "Reponse HelloAsso non exploitable.";

            return string.IsNullOrWhiteSpace(checkout.ErrorCode)
                ? $"HelloAsso a refuse la creation du paiement : {detail}"
                : $"HelloAsso a refuse la creation du paiement (HTTP {checkout.ErrorCode}) : {detail}";
        }

        private static string? ExtractHelloAssoErrorDetail(string? rawPayload)
        {
            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                return null;
            }

            try
            {
                var root = JsonNode.Parse(rawPayload);
                var firstObjectError = root?["errors"]?.AsArray().OfType<JsonObject>().FirstOrDefault();
                var objectErrorMessage = firstObjectError?["message"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(objectErrorMessage))
                {
                    return objectErrorMessage;
                }

                var errorsObject = root?["errors"]?.AsObject();
                if (errorsObject is not null)
                {
                    var firstValidationError = errorsObject
                        .SelectMany(x => x.Value?.AsArray().Select(v => v?.ToString()) ?? [])
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                    if (!string.IsNullOrWhiteSpace(firstValidationError))
                    {
                        return firstValidationError;
                    }
                }

                return root?["detail"]?.GetValue<string>()
                    ?? root?["title"]?.GetValue<string>()
                    ?? Truncate(rawPayload, 300);
            }
            catch
            {
                return Truncate(rawPayload, 300);
            }
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
