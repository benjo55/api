using api.Data;
using api.Dtos.Generic;
using api.Dtos.Me;
using api.Dtos.TaxReceipts;
using api.Exceptions;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Payments
{
    public sealed class MeDonationsService : IMeDonationsService
    {
        private static readonly TaxReceiptStatus[] DownloadableReceiptStatuses =
        [
            TaxReceiptStatus.Generated,
            TaxReceiptStatus.Sent,
            TaxReceiptStatus.EmailFailed
        ];

        private readonly ApplicationDBContext _db;
        private readonly ITaxReceiptService _taxReceiptService;
        private readonly ITaxReceiptEmailService _taxReceiptEmailService;

        public MeDonationsService(
            ApplicationDBContext db,
            ITaxReceiptService taxReceiptService,
            ITaxReceiptEmailService taxReceiptEmailService)
        {
            _db = db;
            _taxReceiptService = taxReceiptService;
            _taxReceiptEmailService = taxReceiptEmailService;
        }

        public async Task<PagedResult<MeDonationListItemDto>> GetMyDonationsAsync(int userId, QueryObject query, CancellationToken cancellationToken = default)
        {
            var donations = BuildScopedDonationQuery(userId)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<DonationStatus>(query.Status, true, out var status))
            {
                donations = donations.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                donations = donations.Where(x =>
                    x.PublicId.Contains(search)
                    || (x.Reference != null && x.Reference.Contains(search))
                    || x.Donor.FirstName.Contains(search)
                    || x.Donor.LastName.Contains(search)
                    || (x.DonorSnapshot != null
                        && (x.DonorSnapshot.FirstName.Contains(search)
                            || x.DonorSnapshot.LastName.Contains(search)
                            || x.DonorSnapshot.Email.Contains(search))));
            }

            donations = query.SortBy switch
            {
                "amount" => query.IsDescending ? donations.OrderByDescending(x => x.Amount) : donations.OrderBy(x => x.Amount),
                "status" => query.IsDescending ? donations.OrderByDescending(x => x.Status) : donations.OrderBy(x => x.Status),
                _ => query.IsDescending ? donations.OrderBy(x => x.DonationDate) : donations.OrderByDescending(x => x.DonationDate)
            };

            var pageSize = Math.Max(1, query.PageSize);
            var pageNumber = Math.Max(1, query.PageNumber);
            var totalCount = await donations.CountAsync(cancellationToken);
            var items = await donations
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<MeDonationListItemDto>
            {
                Items = items.Select(MapListItem).ToList(),
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasNextPage = pageNumber * pageSize < totalCount,
                CurrentPage = pageNumber
            };
        }

        public async Task<MeDonationDetailDto?> GetMyDonationAsync(int userId, string publicId, CancellationToken cancellationToken = default)
        {
            var donation = await BuildScopedDonationQuery(userId)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);

            return donation is null ? null : MapDetail(donation);
        }

        public async Task<MeDonationIntentCreatedDto> CreateDonationIntentAsync(int userId, CreateMeDonationIntentDto dto, CancellationToken cancellationToken = default)
        {
            if (!dto.ConfirmInformationAccuracy)
            {
                throw new BusinessException("Vous devez confirmer l'exactitude des informations.");
            }

            if (dto.Amount <= 0)
            {
                throw new BusinessException("Le montant du don doit être strictement positif.");
            }

            if (decimal.Round(dto.Amount, 2) != dto.Amount)
            {
                throw new BusinessException("Le montant du don ne peut pas comporter plus de deux décimales.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                ?? throw new BusinessException("Utilisateur introuvable.");
            if (!user.EmailConfirmed)
            {
                throw new BusinessException("Votre adresse e-mail doit être confirmée avant de pouvoir effectuer un don.");
            }

            var donor = await _db.Donors.FirstOrDefaultAsync(x => x.UserId == userId && !x.IsArchived, cancellationToken)
                ?? throw new BusinessException("Complétez vos informations personnelles avant de poursuivre.");
            if (!MeProfileService.IsProfileComplete(donor))
            {
                throw new BusinessException("Complétez vos informations personnelles avant de poursuivre.");
            }

            var organization = await _db.BeneficiaryOrganizations
                .FirstOrDefaultAsync(x => x.Id == dto.OrganizationId && x.IsActive && x.IsDonationEnabled, cancellationToken)
                ?? throw new BusinessException("L'organisme sélectionné n'est pas disponible pour les dons.");

            var now = DateTime.UtcNow;
            var donation = new Donation
            {
                UserId = userId,
                OrganizationId = organization.Id,
                DonorId = donor.Id,
                DonationDate = now,
                Amount = dto.Amount,
                Currency = "EUR",
                DonationForm = DonationForm.ManualGiftDeclaration,
                Purpose = Clean(dto.Purpose),
                DonationNature = DonationNature.Cash,
                PaymentMethod = null,
                TaxRegime = DonationTaxRegime.Article200,
                Article200Amount = dto.Amount,
                Status = DonationStatus.AwaitingPayment,
                LegacyDonationLinkStatus = DonationLegacyLinkStatus.NotRequired,
                Comments = Clean(dto.Comment),
                Reference = await GenerateDonationReferenceAsync(cancellationToken),
                CreatedAt = now,
                UpdatedAt = now,
                DonorSnapshot = new DonationDonorSnapshot
                {
                    UserId = userId,
                    FirstName = donor.FirstName,
                    LastName = donor.LastName,
                    BirthDate = donor.BirthDate,
                    Email = user.Email,
                    AddressLine1 = donor.AddressLine1,
                    AddressLine2 = donor.AddressLine2,
                    PostalCode = donor.PostalCode,
                    City = donor.City,
                    Country = donor.CountryCode,
                    CreatedAt = now
                }
            };

            _db.Donations.Add(donation);
            await _db.SaveChangesAsync(cancellationToken);
            await _db.Entry(donation).Reference(x => x.Donor).LoadAsync(cancellationToken);
            await _db.Entry(donation).Reference(x => x.Organization).LoadAsync(cancellationToken);
            await _db.Entry(donation).Reference(x => x.DonorSnapshot).LoadAsync(cancellationToken);
            await _db.Entry(donation).Collection(x => x.TaxReceipts).LoadAsync(cancellationToken);

            return new MeDonationIntentCreatedDto(
                MapListItem(donation),
                "Votre intention de don a été enregistrée. Aucun paiement n'a encore été effectué.");
        }

        public async Task<(byte[] Content, string FileName)?> DownloadMyReceiptAsync(int userId, string publicId, CancellationToken cancellationToken = default)
        {
            var donation = await BuildScopedDonationQuery(userId)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);

            if (donation is null)
            {
                return null;
            }

            var receipt = ResolveLatestDownloadableReceipt(donation);
            if (receipt is null)
            {
                throw new BusinessException("TaxReceiptNotFound");
            }

            return await _taxReceiptService.GetPdfAsync(receipt.Id, cancellationToken);
        }

        public async Task<MeDonationReceiptResendResultDto?> ResendMyReceiptAsync(int userId, string publicId, string? userName, CancellationToken cancellationToken = default)
        {
            var donation = await BuildScopedDonationQuery(userId)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);

            if (donation is null)
            {
                return null;
            }

            var receipt = ResolveLatestDownloadableReceipt(donation);
            if (receipt is null)
            {
                throw new BusinessException("TaxReceiptNotFound");
            }

            var emailResult = await _taxReceiptEmailService.SendAsync(
                receipt.Id,
                new SendTaxReceiptEmailDto(null, null, null),
                userName,
                cancellationToken,
                userId,
                false);

            return new MeDonationReceiptResendResultDto(
                publicId,
                receipt.Id,
                receipt.ReceiptNumber,
                emailResult.EmailStatus,
                emailResult.SentAt,
                emailResult.RecipientEmail);
        }

        private IQueryable<Donation> BuildScopedDonationQuery(int userId)
        {
            return _db.Donations
                .Include(x => x.Donor)
                .Include(x => x.DonorSnapshot)
                .Include(x => x.Organization)
                .Include(x => x.TaxReceipts)
                .Where(x =>
                    x.UserId == userId
                    || x.Donor.UserId == userId
                    || (x.DonorSnapshot != null && x.DonorSnapshot.UserId == userId));
        }

        private static TaxReceipt? ResolveLatestDownloadableReceipt(Donation donation)
        {
            return donation.TaxReceipts
                .Where(x => DownloadableReceiptStatuses.Contains(x.Status))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();
        }

        internal static MeDonationListItemDto MapListItem(Donation donation)
        {
            var receipt = ResolveLatestDownloadableReceipt(donation);

            return new MeDonationListItemDto(
                donation.PublicId,
                donation.Reference ?? string.Empty,
                donation.DonationDate,
                donation.Amount,
                donation.Currency,
                donation.Status,
                donation.PaymentConfirmedAt,
                receipt is not null,
                receipt?.ReceiptNumber,
                receipt?.Status,
                receipt?.GeneratedAt,
                receipt?.SentAt);
        }

        private async Task<string> GenerateDonationReferenceAsync(CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"INT-{year}-";
            var count = await _db.Donations.CountAsync(x => x.Reference != null && x.Reference.StartsWith(prefix), cancellationToken);
            return $"{prefix}{count + 1:000000}";
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static MeDonationDetailDto MapDetail(Donation donation)
        {
            var snapshot = donation.DonorSnapshot;
            var donor = donation.Donor;
            var receipt = ResolveLatestDownloadableReceipt(donation);

            var receiptDto = receipt is null
                ? null
                : new MeDonationReceiptInfoDto(
                    receipt.Id,
                    receipt.ReceiptNumber,
                    receipt.Status,
                    receipt.GeneratedAt,
                    receipt.SentAt,
                    receipt.SentToEmail,
                    receipt.LastEmailStatus);

            return new MeDonationDetailDto(
                donation.PublicId,
                donation.Reference ?? string.Empty,
                donation.DonationDate,
                donation.Amount,
                donation.Currency,
                donation.Status,
                donation.PaymentConfirmedAt,
                donation.LegacyDonationLinkStatus,
                donation.Purpose ?? donation.OtherFormDescription,
                donation.Comments,
                snapshot?.FirstName ?? donor.FirstName,
                snapshot?.LastName ?? donor.LastName,
                snapshot?.Email ?? donor.Email ?? string.Empty,
                snapshot?.AddressLine1 ?? donor.AddressLine1,
                snapshot?.AddressLine2 ?? donor.AddressLine2,
                snapshot?.PostalCode ?? donor.PostalCode,
                snapshot?.City ?? donor.City,
                snapshot?.Country ?? donor.CountryCode,
                donation.Organization.Name,
                receiptDto);
        }
    }
}
