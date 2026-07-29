using System.Text.RegularExpressions;
using api.Data;
using api.Dtos.Me;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Payments
{
    public sealed partial class MeProfileService : IMeProfileService
    {
        private const int RequiredProfileFieldCount = 7;
        private readonly ApplicationDBContext _db;

        public MeProfileService(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<MeProfileDto> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(userId, cancellationToken);
            var donor = await EnsureDonorProfileAsync(user, cancellationToken);
            return MapProfile(donor);
        }

        public async Task<MeProfileDto> UpdateProfileAsync(int userId, SaveMeProfileDto dto, CancellationToken cancellationToken = default)
        {
            ValidateProfile(dto);

            var user = await GetUserAsync(userId, cancellationToken);
            var donor = await EnsureDonorProfileAsync(user, cancellationToken);
            donor.FirstName = CleanRequired(dto.FirstName);
            donor.LastName = CleanRequired(dto.LastName);
            donor.Phone = CleanOptional(dto.Phone);
            donor.BirthDate = NormalizeBirthDate(dto.BirthDate);
            donor.AddressLine1 = CleanRequired(dto.AddressLine1);
            donor.StreetName = donor.AddressLine1;
            donor.AddressLine2 = CleanOptional(dto.AddressLine2);
            donor.PostalCode = CleanRequired(dto.PostalCode);
            donor.City = CleanRequired(dto.City);
            donor.CountryCode = NormalizeCountry(dto.CountryCode);
            donor.CompanyName = CleanOptional(dto.CompanyName);
            donor.Email = user.Email;
            donor.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return MapProfile(donor);
        }

        public async Task<MeDashboardDto> GetDashboardAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(userId, cancellationToken);
            var donor = await EnsureDonorProfileAsync(user, cancellationToken);

            var donations = await BuildScopedDonationQuery(userId)
                .AsNoTracking()
                .OrderByDescending(x => x.DonationDate)
                .ThenByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync(cancellationToken);

            var donationCount = await BuildScopedDonationQuery(userId).CountAsync(cancellationToken);
            var confirmedTotal = await BuildScopedDonationQuery(userId)
                .Where(x =>
                    x.Status == DonationStatus.Paid
                    || x.Status == DonationStatus.ReceiptGenerated
                    || x.Status == DonationStatus.Completed)
                .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
            var lastDonationDate = await BuildScopedDonationQuery(userId)
                .OrderByDescending(x => x.DonationDate)
                .Select(x => (DateTime?)x.DonationDate)
                .FirstOrDefaultAsync(cancellationToken);
            var documentCount = await BuildScopedDonationQuery(userId)
                .SelectMany(x => x.TaxReceipts)
                .CountAsync(
                    x =>
                        x.Status == TaxReceiptStatus.Generated
                        || x.Status == TaxReceiptStatus.Sent
                        || x.Status == TaxReceiptStatus.EmailFailed,
                    cancellationToken);

            var recentDonations = donations.Select(MeDonationsService.MapListItem).ToList();
            var activity = donations
                .Select(x => new MeActivityItemDto(
                    x.UpdatedAt == default ? x.CreatedAt : x.UpdatedAt,
                    "DONATION",
                    $"{FormatAmount(x.Amount)} vers {x.Organization.Name}",
                    x.Status.ToString(),
                    $"/my-space/donations/{x.PublicId}"))
                .ToList();

            if (donor.UpdatedAt > donor.CreatedAt)
            {
                activity.Add(new MeActivityItemDto(
                    donor.UpdatedAt,
                    "PROFILE",
                    "Profil personnel mis à jour",
                    donor.IsArchived ? "Archivé" : "Actif",
                    "/my-space/profile"));
            }

            return new MeDashboardDto(
                new MeAccountDto(
                    user.Id,
                    user.Username,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.EmailConfirmed,
                    user.Status,
                    user.CreatedDate,
                    user.UpdatedDate),
                MapProfile(donor),
                new MeDonationSummaryDto(donationCount, confirmedTotal, documentCount, lastDonationDate),
                recentDonations,
                activity.OrderByDescending(x => x.Date).Take(5).ToList(),
                BuildNewsFeed(user, donor, donationCount, documentCount),
                donations
                    .Select(x => new MeFinancialFeedItemDto(
                        x.DonationDate,
                        x.Reference ?? x.PublicId,
                        x.Amount,
                        x.Currency,
                        x.Status,
                        x.Organization.Name,
                        $"/my-space/donations/{x.PublicId}"))
                    .ToList());
        }

        public async Task<IReadOnlyList<DonationOrganizationOptionDto>> GetDonationOrganizationsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.BeneficiaryOrganizations
                .AsNoTracking()
                .Where(x => x.IsActive && x.IsDonationEnabled)
                .OrderBy(x => x.Name)
                .Select(x => new DonationOrganizationOptionDto(
                    x.Id,
                    x.Name,
                    x.Purpose,
                    x.IsEligibleForTaxReceipt))
                .ToListAsync(cancellationToken);
        }

        private async Task<User> GetUserAsync(int userId, CancellationToken cancellationToken)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                ?? throw new BusinessException("Utilisateur introuvable.");
        }

        private async Task<Donor> EnsureDonorProfileAsync(User user, CancellationToken cancellationToken)
        {
            var donor = await _db.Donors.FirstOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
            if (donor is not null)
            {
                return donor;
            }

            donor = new Donor
            {
                UserId = user.Id,
                DonorType = DonorType.Individual,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                AddressLine1 = string.Empty,
                StreetName = string.Empty,
                PostalCode = string.Empty,
                City = string.Empty,
                CountryCode = "FR"
            };

            _db.Donors.Add(donor);
            await _db.SaveChangesAsync(cancellationToken);
            return donor;
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

        public static MeProfileDto MapProfile(Donor donor)
        {
            var completed = CountCompletedRequiredFields(donor);
            var percentage = (int)Math.Round(completed / (decimal)RequiredProfileFieldCount * 100m, MidpointRounding.AwayFromZero);

            return new MeProfileDto(
                donor.Id,
                donor.FirstName,
                donor.LastName,
                donor.Phone,
                donor.BirthDate,
                donor.AddressLine1,
                donor.AddressLine2,
                donor.PostalCode,
                donor.City,
                donor.CountryCode,
                donor.CompanyName,
                FormatAddress(donor),
                completed == RequiredProfileFieldCount,
                percentage,
                donor.CreatedAt,
                donor.UpdatedAt);
        }

        public static bool IsProfileComplete(Donor donor) => CountCompletedRequiredFields(donor) == RequiredProfileFieldCount;

        public static string FormatAddress(Donor donor)
        {
            return string.Join(", ", new[]
            {
                donor.AddressLine1,
                donor.AddressLine2,
                $"{donor.PostalCode} {donor.City}".Trim(),
                donor.CountryCode
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static int CountCompletedRequiredFields(Donor donor)
        {
            var completedTextFields = new[]
            {
                donor.FirstName,
                donor.LastName,
                donor.AddressLine1,
                donor.PostalCode,
                donor.City,
                donor.CountryCode
            }.Count(x => !string.IsNullOrWhiteSpace(x));

            return completedTextFields + (donor.BirthDate.HasValue ? 1 : 0);
        }

        private static void ValidateProfile(SaveMeProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FirstName))
            {
                throw new BusinessException("Le prénom est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(dto.LastName))
            {
                throw new BusinessException("Le nom est obligatoire.");
            }

            if (!dto.BirthDate.HasValue)
            {
                throw new BusinessException("La date de naissance est obligatoire.");
            }

            var birthDate = NormalizeBirthDate(dto.BirthDate)!.Value;
            var today = DateTime.UtcNow.Date;
            if (birthDate >= today || birthDate < today.AddYears(-120))
            {
                throw new BusinessException("La date de naissance n'est pas valide.");
            }

            if (string.IsNullOrWhiteSpace(dto.AddressLine1))
            {
                throw new BusinessException("L'adresse est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(dto.PostalCode))
            {
                throw new BusinessException("Le code postal est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(dto.City))
            {
                throw new BusinessException("La ville est obligatoire.");
            }

            var country = NormalizeCountry(dto.CountryCode);
            if (country == "FR" && !FrenchPostalCodeRegex().IsMatch(dto.PostalCode.Trim()))
            {
                throw new BusinessException("Le code postal français doit contenir cinq chiffres.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                var normalizedPhone = AuthenticationAccountService.NormalizePhoneNumber(dto.Phone);
                if (!AuthenticationAccountService.IsValidPhoneNumber(normalizedPhone))
                {
                    throw new BusinessException("Le numéro de téléphone n'est pas valide.");
                }
            }
        }

        private static string CleanRequired(string value) => value.Trim();

        private static string? CleanOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static DateTime? NormalizeBirthDate(DateTime? value) => value?.Date;

        private static string NormalizeCountry(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "FR" : value.Trim().ToUpperInvariant();

        private static string FormatAmount(decimal amount) =>
            amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));

        private static IReadOnlyList<MeNewsItemDto> BuildNewsFeed(
            User user,
            Donor donor,
            int donationCount,
            int documentCount)
        {
            var now = DateTime.UtcNow;
            var items = new List<MeNewsItemDto>();

            if (!user.EmailConfirmed)
            {
                items.Add(new MeNewsItemDto(
                    now,
                    "Adresse e-mail à confirmer",
                    "Confirmez votre adresse e-mail pour accéder au parcours de don.",
                    "warning",
                    "Renvoyer l'e-mail",
                    "/login"));
            }

            if (!IsProfileComplete(donor))
            {
                items.Add(new MeNewsItemDto(
                    donor.UpdatedAt,
                    "Profil à compléter",
                    "Votre adresse complète et votre date de naissance sont nécessaires pour préparer correctement vos futurs documents.",
                    "warning",
                    "Compléter mon profil",
                    "/my-space/profile"));
            }

            if (donationCount == 0)
            {
                items.Add(new MeNewsItemDto(
                    now,
                    "Premier don",
                    "Vous pouvez enregistrer une intention de don. Aucun paiement ne sera déclenché à cette étape.",
                    "info",
                    "Faire un don",
                    "/my-space/donations/new"));
            }

            if (documentCount > 0)
            {
                items.Add(new MeNewsItemDto(
                    now,
                    "Documents disponibles",
                    $"{documentCount} reçu(s) fiscal(aux) sont disponibles dans votre historique.",
                    "success",
                    "Consulter mes dons",
                    "/my-space/donations"));
            }

            if (items.Count == 0)
            {
                items.Add(new MeNewsItemDto(
                    now,
                    "Votre espace est prêt",
                    "Votre compte est opérationnel. Retrouvez ici vos dons, reçus et prochaines actions.",
                    "success",
                    "Voir mes dons",
                    "/my-space/donations"));
            }

            return items.Take(4).ToList();
        }

        [GeneratedRegex(@"^\d{5}$")]
        private static partial Regex FrenchPostalCodeRegex();
    }
}
