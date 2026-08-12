using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;
using api.Configuration;
using api.Data;
using api.Dtos.PublicDonations;
using api.Dtos.TaxReceipts;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using api.Services.TaxReceipts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Services.Payments
{
    public sealed class PublicDonationService : IPublicDonationService
    {
        private readonly ApplicationDBContext _db;
        private readonly IPaymentProvider _paymentProvider;
        private readonly HelloAssoOptions _helloAssoOptions;
        private readonly DonationCheckoutOptions _checkoutOptions;
        private readonly ITaxReceiptService _taxReceiptService;
        private readonly ITaxReceiptEmailService _taxReceiptEmailService;
        private readonly IDonationReceiptAccessTokenService _receiptTokenService;
        private readonly ILogger<PublicDonationService> _logger;
        private readonly IPublicOriginResolver? _publicOriginResolver;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IHostEnvironment? _environment;

        public PublicDonationService(
            ApplicationDBContext db,
            IPaymentProvider paymentProvider,
            IOptions<HelloAssoOptions> helloAssoOptions,
            IOptions<DonationCheckoutOptions> checkoutOptions,
            ITaxReceiptService taxReceiptService,
            ITaxReceiptEmailService taxReceiptEmailService,
            IDonationReceiptAccessTokenService receiptTokenService,
            ILogger<PublicDonationService> logger,
            IPublicOriginResolver? publicOriginResolver = null,
            IHttpContextAccessor? httpContextAccessor = null,
            IHostEnvironment? environment = null)
        {
            _db = db;
            _paymentProvider = paymentProvider;
            _helloAssoOptions = helloAssoOptions.Value;
            _checkoutOptions = checkoutOptions.Value;
            _taxReceiptService = taxReceiptService;
            _taxReceiptEmailService = taxReceiptEmailService;
            _receiptTokenService = receiptTokenService;
            _logger = logger;
            _publicOriginResolver = publicOriginResolver;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }

        public async Task<PublicDonationCheckoutResponse> InitializeCheckoutAsync(PublicDonationCheckoutRequest request, CancellationToken cancellationToken)
        {
            ValidateCheckoutRequest(request);

            var organization = await _db.BeneficiaryOrganizations
                .AsTracking()
                .FirstOrDefaultAsync(x => x.IsActive && x.IsDonationEnabled, cancellationToken)
                ?? throw new BusinessException("Aucune organisation active pour les dons.");

            var organizationSlug = ResolveHelloAssoSlug(organization);
            var credentialKey = ResolveCredentialKey(organization);
            if (!IsHelloAssoAvailable(organization, organizationSlug, credentialKey))
            {
                throw new BusinessException("Le paiement HelloAsso n'est pas disponible pour cet organisme.");
            }

            var donor = await FindOrCreateDonorAsync(request.Donor, cancellationToken);
            var reference = await GenerateDonationReferenceAsync(cancellationToken);

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var donation = new Donation
            {
                PublicId = Guid.NewGuid().ToString("N"),
                Reference = reference,
                OrganizationId = organization.Id,
                DonorId = donor.Id,
                DonationDate = DateTime.UtcNow,
                Amount = request.Amount,
                Currency = "EUR",
                DonationForm = DonationForm.PrivateDeed,
                DonationNature = DonationNature.Cash,
                PaymentMethod = DonationPaymentMethod.BankCard,
                TaxRegime = DonationTaxRegime.Article200,
                Status = DonationStatus.AwaitingPayment,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.Donations.Add(donation);
            await _db.SaveChangesAsync(cancellationToken);

            var paymentAttempt = new PaymentAttempt
            {
                DonationId = donation.Id,
                Provider = PaymentProvider.HelloAsso,
                InternalReference = $"PAY-PUBLIC-{Guid.NewGuid():N}"[..32],
                IdempotencyKey = $"public-helloasso:{donation.PublicId}",
                Amount = request.Amount,
                Currency = "EUR",
                PaymentStatus = PaymentStatus.Created,
                CheckoutUrlExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.PaymentAttempts.Add(paymentAttempt);
            await _db.SaveChangesAsync(cancellationToken);

            var amountInCents = HelloAssoAmountConverter.EuroToCents(request.Amount);
            var metadata = new Dictionary<string, string>
            {
                ["donationId"] = donation.Id.ToString(),
                ["paymentAttemptId"] = paymentAttempt.Id.ToString(),
                ["donationReference"] = donation.Reference ?? string.Empty,
                ["donationPublicId"] = donation.PublicId,
            };

            var returnUrl = BuildReturnUrl(donation.PublicId);
            var checkout = await _paymentProvider.CreateCheckoutAsync(
                new CreateCheckoutCommand(
                    organizationSlug,
                    amountInCents,
                    _helloAssoOptions.ItemName,
                    returnUrl,
                    BuildDonationUrl("/donate"),
                    BuildDonationUrl("/donate/error"),
                    donor.FirstName,
                    donor.LastName,
                    donor.Email ?? request.Donor.Email,
                    donor.AddressLine1,
                    donor.PostalCode,
                    donor.City,
                    donor.CountryCode,
                    donor.BirthDate,
                    metadata,
                    credentialKey),
                cancellationToken);

            if (!checkout.Success || string.IsNullOrWhiteSpace(checkout.RedirectUrl))
            {
                var failureMessage = BuildHelloAssoCheckoutFailureMessage(checkout);
                paymentAttempt.PaymentStatus = PaymentStatus.Refused;
                paymentAttempt.FailedAt = DateTime.UtcNow;
                paymentAttempt.FailureCode = checkout.ErrorCode;
                paymentAttempt.FailureMessage = Truncate(failureMessage, 1000);
                donation.Status = DonationStatus.Failed;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                throw new BusinessException(failureMessage);
            }

            paymentAttempt.ProviderCheckoutIntentId = checkout.CheckoutIntentId;
            paymentAttempt.RedirectUrl = checkout.RedirectUrl;
            paymentAttempt.PaymentStatus = PaymentStatus.RedirectReady;
            paymentAttempt.UpdatedAt = DateTime.UtcNow;
            donation.Status = DonationStatus.PaymentPending;
            donation.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new PublicDonationCheckoutResponse(
                donation.PublicId,
                donation.Reference ?? string.Empty,
                checkout.RedirectUrl);
        }

        public async Task<PublicDonationStatusResponse?> GetPublicStatusAsync(string publicId, CancellationToken cancellationToken)
        {
            var donation = await _db.Donations
                .Include(x => x.TaxReceipts)
                .FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);
            if (donation is null)
            {
                return null;
            }

            var receipt = donation.TaxReceipts
                .Where(x => x.Status is TaxReceiptStatus.Generated or TaxReceiptStatus.Sent or TaxReceiptStatus.EmailFailed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            var receiptToken = receipt is null
                ? null
                : _receiptTokenService.Create(publicId, TimeSpan.FromMinutes(_checkoutOptions.ReceiptTokenLifetimeMinutes));

            return new PublicDonationStatusResponse(
                donation.Reference ?? string.Empty,
                donation.Amount,
                donation.Currency,
                donation.Status.ToString(),
                receipt is not null,
                receiptToken);
        }

        public async Task<PublicDonationReceiptTokenResponse?> CreateReceiptTokenAsync(string publicId, CancellationToken cancellationToken)
        {
            var exists = await _db.Donations.AnyAsync(x => x.PublicId == publicId, cancellationToken);
            if (!exists)
            {
                return null;
            }

            return new PublicDonationReceiptTokenResponse(
                _receiptTokenService.Create(publicId, TimeSpan.FromMinutes(_checkoutOptions.ReceiptTokenLifetimeMinutes)));
        }

        public async Task<(byte[] Content, string FileName)?> DownloadReceiptAsync(string publicId, string token, CancellationToken cancellationToken)
        {
            if (!_receiptTokenService.Validate(publicId, token))
            {
                return null;
            }

            var receiptId = await _db.TaxReceipts
                .Include(x => x.Donation)
                .Where(x => x.Donation.PublicId == publicId
                    && (x.Status == TaxReceiptStatus.Generated
                        || x.Status == TaxReceiptStatus.Sent
                        || x.Status == TaxReceiptStatus.EmailFailed))
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (!receiptId.HasValue)
            {
                return null;
            }

            return await _taxReceiptService.GetPdfAsync(receiptId.Value, cancellationToken);
        }

        public async Task ProcessWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, string? remoteIpAddress, CancellationToken cancellationToken)
        {
            var reception = await _paymentProvider.ReceiveWebhookAsync(rawBody, headers, remoteIpAddress, cancellationToken);
            if (!reception.Accepted)
            {
                throw new BusinessException(reception.ErrorMessage ?? "Webhook refuse");
            }

            var payloadHash = HelloAssoSecurity.ComputeSha256(rawBody);
            var exists = await _db.PaymentWebhookInbox
                .AnyAsync(x => x.Provider == PaymentProvider.HelloAsso && x.PayloadHash == payloadHash, cancellationToken);
            if (exists)
            {
                return;
            }

            _db.PaymentWebhookInbox.Add(new PaymentWebhookInbox
            {
                Provider = PaymentProvider.HelloAsso,
                PayloadHash = payloadHash,
                EventType = reception.EventType,
                ExternalObjectId = reception.ExternalObjectId,
                RawPayload = rawBody,
                ReceivedAt = DateTime.UtcNow,
                ProcessingStatus = WebhookProcessingStatus.Pending,
                AttemptCount = 0,
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> ProcessPendingWebhooksAsync(CancellationToken cancellationToken)
        {
            var pendingItems = await _db.PaymentWebhookInbox
                .Where(x => x.Provider == PaymentProvider.HelloAsso && x.ProcessingStatus == WebhookProcessingStatus.Pending && x.AttemptCount < 8)
                .OrderBy(x => x.ReceivedAt)
                .Take(25)
                .ToListAsync(cancellationToken);

            var processedCount = 0;
            foreach (var inbox in pendingItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                inbox.AttemptCount += 1;
                try
                {
                    await ProcessSingleWebhookAsync(inbox, cancellationToken);
                    inbox.ProcessingStatus = WebhookProcessingStatus.Processed;
                    inbox.ProcessedAt = DateTime.UtcNow;
                    inbox.LastError = null;
                    processedCount += 1;
                }
                catch (Exception ex)
                {
                    inbox.LastError = ex.Message;
                    if (inbox.AttemptCount >= 8)
                    {
                        inbox.ProcessingStatus = WebhookProcessingStatus.Failed;
                    }
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return processedCount;
        }

        public async Task ForceReconcileAsync(int donationId, CancellationToken cancellationToken)
        {
            var attempt = await _db.PaymentAttempts
                .Include(x => x.Donation)
                .FirstOrDefaultAsync(x => x.DonationId == donationId, cancellationToken)
                ?? throw new BusinessException("Tentative de paiement introuvable");

            await ReconcileAttemptAsync(attempt, cancellationToken);
        }

        public async Task ResendReceiptAsync(int donationId, CancellationToken cancellationToken)
        {
            var receipt = await _db.TaxReceipts
                .Include(x => x.Donation)
                    .ThenInclude(x => x.Donor)
                .Where(x => x.DonationId == donationId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new BusinessException("Recu fiscal introuvable");

            await _taxReceiptEmailService.SendAsync(
                receipt.Id,
                new SendTaxReceiptEmailDto(null, null, null),
                "admin-resend",
                cancellationToken);
        }

        private async Task ProcessSingleWebhookAsync(PaymentWebhookInbox inbox, CancellationToken cancellationToken)
        {
            var node = JsonSerializer.Deserialize<JsonElement>(inbox.RawPayload);
            var data = node.TryGetProperty("data", out var dataNode) ? dataNode : node;
            var checkoutIntentId = TryRead(data, "checkoutIntentId")
                ?? TryRead(data, "checkoutIntent", "id")
                ?? TryRead(node, "checkoutIntentId");

            if (string.IsNullOrWhiteSpace(checkoutIntentId))
            {
                throw new BusinessException("checkoutIntentId absent du webhook");
            }

            var attempt = await _db.PaymentAttempts
                .Include(x => x.Donation)
                    .ThenInclude(x => x.Donor)
                .Include(x => x.Donation)
                    .ThenInclude(x => x.Organization)
                .FirstOrDefaultAsync(x => x.ProviderCheckoutIntentId == checkoutIntentId, cancellationToken)
                ?? throw new BusinessException("Tentative de paiement inconnue");

            await ReconcileAttemptAsync(attempt, cancellationToken);
        }

        private async Task ReconcileAttemptAsync(PaymentAttempt attempt, CancellationToken cancellationToken)
        {
            var donation = attempt.Donation;
            var reconciliation = await _paymentProvider.ReconcilePaymentAsync(
                new PaymentReconciliationCommand(_helloAssoOptions.OrganizationSlug, attempt.ProviderCheckoutIntentId ?? string.Empty),
                cancellationToken);

            attempt.LastReconciledAt = DateTime.UtcNow;

            if (!reconciliation.Found)
            {
                throw new BusinessException("Checkout HelloAsso introuvable");
            }

            if (!reconciliation.Metadata.TryGetValue("donationId", out var donationIdValue)
                || !int.TryParse(donationIdValue, out var donationId)
                || donationId != donation.Id)
            {
                throw new BusinessException("Metadonnees checkout incoherentes");
            }

            if (reconciliation.AmountInCents is null || HelloAssoAmountConverter.CentsToEuro(reconciliation.AmountInCents.Value) != donation.Amount)
            {
                throw new BusinessException("Montant reconcile incoherent");
            }

            if (!string.Equals(reconciliation.Currency, donation.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException("Devise incoherente");
            }

            attempt.ProviderOrderId = reconciliation.ExternalOrderId;
            attempt.ProviderPaymentId = reconciliation.ExternalPaymentId;
            attempt.ProviderPaymentState = reconciliation.ProviderPaymentState;
            attempt.PaymentStatus = HelloAssoPaymentStatusMapper.Map(reconciliation.ProviderPaymentState);

            if (reconciliation.IsAuthorized)
            {
                attempt.AuthorizedAt = DateTime.UtcNow;
                donation.PaymentConfirmedAt = DateTime.UtcNow;
                donation.Status = DonationStatus.Paid;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                await EnsureReceiptGeneratedAndSentAsync(donation.Id, cancellationToken);
            }
            else if (attempt.PaymentStatus == PaymentStatus.Refused)
            {
                attempt.FailedAt = DateTime.UtcNow;
                donation.Status = DonationStatus.Failed;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                donation.Status = DonationStatus.PaymentPending;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task EnsureReceiptGeneratedAndSentAsync(int donationId, CancellationToken cancellationToken)
        {
            var existing = await _db.TaxReceipts
                .Where(x => x.DonationId == donationId
                    && (x.Status == TaxReceiptStatus.Generated
                        || x.Status == TaxReceiptStatus.Sent
                        || x.Status == TaxReceiptStatus.EmailFailed))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                if (existing.Status == TaxReceiptStatus.EmailFailed)
                {
                    await _taxReceiptEmailService.SendAsync(existing.Id, new SendTaxReceiptEmailDto(null, null, null), "system-retry", cancellationToken);
                }
                return;
            }

            var donation = await _db.Donations
                .Include(x => x.Organization)
                .Include(x => x.Donor)
                .FirstOrDefaultAsync(x => x.Id == donationId, cancellationToken)
                ?? throw new BusinessException("Don introuvable");

            if (!donation.Organization.IsEligibleForTaxReceipt)
            {
                donation.Status = DonationStatus.Completed;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            var created = await _taxReceiptService.CreateForDonationAsync(
                donation.Id,
                new CreateTaxReceiptDto(
                    donation.OrganizationId,
                    "2041-RD",
                    "11580*05",
                    $"helloasso-{donation.PublicId}"),
                "system",
                cancellationToken);

            var generation = await _taxReceiptService.GenerateAsync(created.Id, "system", cancellationToken);
            await _taxReceiptEmailService.SendAsync(generation.Receipt.Id, new SendTaxReceiptEmailDto(null, null, null), "system", cancellationToken);

            donation.Status = DonationStatus.Completed;
            donation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        private void ValidateCheckoutRequest(PublicDonationCheckoutRequest request)
        {
            if (request.Amount <= 0)
            {
                throw new BusinessException("Le montant doit etre strictement positif.");
            }

            if (request.Amount < _checkoutOptions.MinAmountEur)
            {
                throw new BusinessException($"Montant minimum: {_checkoutOptions.MinAmountEur:0.00} EUR");
            }

            if (request.Amount > _checkoutOptions.MaxAmountEur)
            {
                throw new BusinessException($"Montant maximum: {_checkoutOptions.MaxAmountEur:0.00} EUR");
            }

            if (decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero) != request.Amount)
            {
                throw new BusinessException("Le montant ne peut pas contenir plus de 2 decimales.");
            }

            if (string.IsNullOrWhiteSpace(request.Donor.FirstName)
                || string.IsNullOrWhiteSpace(request.Donor.LastName)
                || string.IsNullOrWhiteSpace(request.Donor.Email)
                || string.IsNullOrWhiteSpace(request.Donor.Address)
                || string.IsNullOrWhiteSpace(request.Donor.PostalCode)
                || string.IsNullOrWhiteSpace(request.Donor.City)
                || string.IsNullOrWhiteSpace(request.Donor.Country))
            {
                throw new BusinessException("Informations donateur incompletes.");
            }

            _ = NormalizeCountryCode(request.Donor.Country);
        }

        private async Task<Donor> FindOrCreateDonorAsync(PublicDonationDonorInput donorInput, CancellationToken cancellationToken)
        {
            var normalizedEmail = donorInput.Email.Trim().ToLowerInvariant();
            var existing = await _db.Donors
                .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == normalizedEmail && !x.IsArchived, cancellationToken);

            if (existing is not null)
            {
                var countryCode = NormalizeCountryCode(donorInput.Country);
                existing.FirstName = donorInput.FirstName.Trim();
                existing.LastName = donorInput.LastName.Trim();
                existing.Email = normalizedEmail;
                existing.AddressLine1 = donorInput.Address.Trim();
                existing.PostalCode = donorInput.PostalCode.Trim();
                existing.City = donorInput.City.Trim();
                existing.CountryCode = countryCode;
                existing.StreetName = donorInput.Address.Trim();
                existing.UpdatedAt = DateTime.UtcNow;
                return existing;
            }

            var donorCountryCode = NormalizeCountryCode(donorInput.Country);
            var donor = new Donor
            {
                DonorType = DonorType.Individual,
                FirstName = donorInput.FirstName.Trim(),
                LastName = donorInput.LastName.Trim(),
                Email = normalizedEmail,
                AddressLine1 = donorInput.Address.Trim(),
                StreetName = donorInput.Address.Trim(),
                PostalCode = donorInput.PostalCode.Trim(),
                City = donorInput.City.Trim(),
                CountryCode = donorCountryCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.Donors.Add(donor);
            await _db.SaveChangesAsync(cancellationToken);
            return donor;
        }

        private async Task<string> GenerateDonationReferenceAsync(CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var like = $"DON-{year}-%";
            var references = await _db.Donations
                .Where(x => x.Reference != null && EF.Functions.Like(x.Reference, like))
                .Select(x => x.Reference!)
                .ToListAsync(cancellationToken);

            var max = 0;
            foreach (var reference in references)
            {
                var parts = reference.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out var seq) && seq > max)
                {
                    max = seq;
                }
            }

            return $"DON-{year}-{(max + 1):000000}";
        }

        private bool IsHelloAssoAvailable(
            BeneficiaryOrganization organization,
            string organizationSlug,
            string? credentialKey)
        {
            var hasCredentials = (!string.IsNullOrWhiteSpace(credentialKey)
                    && _helloAssoOptions.HasCredential(credentialKey))
                || _helloAssoOptions.HasGlobalCredentials;

            return (_helloAssoOptions.Enabled || _helloAssoOptions.HasAnyCredentials)
                && !string.IsNullOrWhiteSpace(organizationSlug)
                && hasCredentials
                && (organization.IsHelloAssoPaymentEnabled
                    || !string.IsNullOrWhiteSpace(organization.HelloAssoOrganizationSlug)
                    || !string.IsNullOrWhiteSpace(_helloAssoOptions.OrganizationSlug));
        }

        private string ResolveHelloAssoSlug(BeneficiaryOrganization organization) =>
            !string.IsNullOrWhiteSpace(organization.HelloAssoOrganizationSlug)
                ? organization.HelloAssoOrganizationSlug.Trim()
                : _helloAssoOptions.OrganizationSlug.Trim();

        private string? ResolveCredentialKey(BeneficiaryOrganization organization)
        {
            if (!string.IsNullOrWhiteSpace(organization.HelloAssoCredentialKey))
            {
                return organization.HelloAssoCredentialKey.Trim();
            }

            return _helloAssoOptions.Credentials.Count == 1
                ? _helloAssoOptions.Credentials.Keys.Single()
                : null;
        }

        private static string NormalizeCountryCode(string country)
        {
            var value = country.Trim().ToUpperInvariant();
            if (value.Length == 2)
            {
                return value;
            }

            if (value.Length == 3)
            {
                foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
                {
                    try
                    {
                        var region = new RegionInfo(culture.Name);
                        if (string.Equals(region.ThreeLetterISORegionName, value, StringComparison.OrdinalIgnoreCase))
                        {
                            return region.TwoLetterISORegionName.ToUpperInvariant();
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Ignore cultures without a usable region.
                    }
                }
            }

            throw new BusinessException("Le pays doit etre renseigne au format ISO2 ou ISO3.");
        }

        private string BuildReturnUrl(string publicId)
        {
            var url = BuildDonationUrl("/donate/return");
            var separator = url.Contains('?') ? "&" : "?";
            return $"{url}{separator}donation={Uri.EscapeDataString(publicId)}";
        }

        private string BuildDonationUrl(string path)
        {
            try
            {
                return $"{ResolveDonationOrigin()}{path}";
            }
            catch
            {
                return $"{GetHelloAssoFallbackOrigin()}{path}";
            }
        }

        private string ResolveDonationOrigin()
        {
            var requestOrigin = ResolveRequestOrigin();
            if (!string.IsNullOrWhiteSpace(requestOrigin))
            {
                return requestOrigin;
            }

            return _publicOriginResolver?.GetOrigin(SiteExperience.Donation)
                ?? GetHelloAssoFallbackOrigin();
        }

        private string? ResolveRequestOrigin()
        {
            var origin = _httpContextAccessor?.HttpContext?.Request.Headers.Origin.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(origin)
                || !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var normalized = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            if (_environment?.IsDevelopment() == true)
            {
                return normalized;
            }

            var resolved = _publicOriginResolver?.Resolve(uri.Host);
            return resolved is { IsKnownHost: true, Experience: SiteExperience.Donation }
                ? normalized
                : null;
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

        private string GetHelloAssoFallbackOrigin()
        {
            if (Uri.TryCreate(_helloAssoOptions.ReturnUrl, UriKind.Absolute, out var returnUri))
            {
                return returnUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            }

            return "http://localhost:5173";
        }

        private static string? TryRead(JsonElement root, string property)
        {
            return root.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString()
                : null;
        }

        private static string? TryRead(JsonElement root, string parent, string property)
        {
            if (!root.TryGetProperty(parent, out var p) || p.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return p.TryGetProperty(property, out var child) && child.ValueKind == JsonValueKind.String
                ? child.GetString()
                : null;
        }
    }
}
