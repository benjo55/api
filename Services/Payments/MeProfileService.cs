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
                    $"/back-office/donation-space/donations/{x.PublicId}"))
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
                        $"/back-office/donation-space/donations/{x.PublicId}"))
                    .ToList());
        }

        public async Task<MePrivateSpaceDto> GetPrivateSpaceAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Person)
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                ?? throw new BusinessException("Utilisateur introuvable.");

            var donationSummary = await BuildDonationSummaryAsync(userId, cancellationToken);
            if (user.Person == null)
            {
                return new MePrivateSpaceDto(
                    null,
                    new MePrivateSpaceMetricsDto(0, 0m, 0m, 0m, 0, 0, null),
                    Array.Empty<MeContractSummaryDto>(),
                    Array.Empty<MeOperationSummaryDto>(),
                    Array.Empty<MeDocumentSummaryDto>(),
                    donationSummary,
                    new[]
                    {
                        new MeNewsItemDto(
                            DateTime.UtcNow,
                            "Rattachement personne requis",
                            "Votre compte n'est pas encore rattaché à une fiche personne. Un administrateur doit finaliser le lien pour afficher vos contrats.",
                            "warning",
                            null,
                            null)
                    });
            }

            var personId = user.Person.Id;
            var contractRows = await _db.Contracts
                .AsNoTracking()
                .Where(c => c.PersonId == personId)
                .OrderBy(c => c.ContractNumber)
                .Select(c => new
                {
                    c.Id,
                    c.ContractNumber,
                    c.ContractLabel,
                    c.ContractType,
                    c.Status,
                    c.Currency,
                    c.CurrentValue,
                    c.TotalPaidPremiums,
                    c.NetInvested,
                    c.PerformancePercent,
                    c.DateEffect,
                    c.DateMaturity,
                    ProductName = c.Product != null ? c.Product.ProductName : null,
                    c.HasAlert,
                    c.Locked,
                    DocumentCount = c.Documents.Count(),
                    OperationCount = c.Operations.Count()
                })
                .ToListAsync(cancellationToken);

            var contractIds = contractRows.Select(c => c.Id).ToList();

            var valuationDates = await _db.ContractValuations
                .AsNoTracking()
                .Where(v => contractIds.Contains(v.ContractId))
                .GroupBy(v => v.ContractId)
                .Select(g => new
                {
                    ContractId = g.Key,
                    LastValuationDate = g.Max(v => (DateTime?)v.ValuationDate)
                })
                .ToDictionaryAsync(x => x.ContractId, x => x.LastValuationDate, cancellationToken);

            var supportRows = await _db.FinancialSupportAllocations
                .AsNoTracking()
                .Where(a => contractIds.Contains(a.ContractId))
                .Select(a => new
                {
                    a.ContractId,
                    a.CurrentAmount,
                    a.Compartment.ManagementMode,
                    SupportNature = a.Support.SupportNature,
                    a.Support.MifidRiskTolerance,
                    a.Support.LastValuationDate
                })
                .ToListAsync(cancellationToken);

            var supportsByContract = supportRows
                .GroupBy(a => a.ContractId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var pendingOperationContractIds = await _db.Operations
                .AsNoTracking()
                .Where(o => o.Contract.PersonId == personId && o.Status == OperationStatus.Pending)
                .Select(o => o.ContractId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var pendingOperationContractIdSet = pendingOperationContractIds.ToHashSet();

            var recentDocumentThreshold = DateTime.UtcNow.AddDays(-45);
            var newDocumentContractIds = await _db.Documents
                .AsNoTracking()
                .Where(d =>
                    d.ContractId.HasValue &&
                    d.Contract != null &&
                    d.Contract.PersonId == personId &&
                    d.UploadedAt >= recentDocumentThreshold)
                .Select(d => d.ContractId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
            var newDocumentContractIdSet = newDocumentContractIds.ToHashSet();

            var contracts = contractRows
                .Select(c =>
                {
                    supportsByContract.TryGetValue(c.Id, out var contractSupports);
                    contractSupports ??= [];

                    var supportsCurrentValue = contractSupports.Sum(s => s.CurrentAmount);
                    var euroFundValue = contractSupports
                        .Where(s => s.SupportNature == FinancialSupportNature.EuroFund)
                        .Sum(s => s.CurrentAmount);
                    var unitLinkedValue = contractSupports
                        .Where(s => s.SupportNature == FinancialSupportNature.UnitLinked)
                        .Sum(s => s.CurrentAmount);
                    var lastSupportValuationDate = contractSupports
                        .Select(s => s.LastValuationDate)
                        .Where(d => d.HasValue)
                        .DefaultIfEmpty(null)
                        .Max();
                    var lastValuationDate = valuationDates.GetValueOrDefault(c.Id) ?? lastSupportValuationDate;
                    var valueStatus = ResolveContractValueStatus(
                        c.Status,
                        c.CurrentValue,
                        c.TotalPaidPremiums,
                        lastValuationDate);
                    var hasPendingOperation = pendingOperationContractIdSet.Contains(c.Id);
                    var hasNewDocument = newDocumentContractIdSet.Contains(c.Id);

                    return new MeContractSummaryDto(
                        c.Id,
                        c.ContractNumber,
                        c.ContractLabel,
                        c.ContractType,
                        c.Status,
                        c.Currency,
                        c.CurrentValue,
                        c.TotalPaidPremiums,
                        c.NetInvested,
                        c.PerformancePercent,
                        c.DateEffect,
                        c.DateMaturity,
                        c.ProductName,
                        c.HasAlert,
                        c.DocumentCount,
                        c.OperationCount,
                        lastValuationDate,
                        valueStatus,
                        contractSupports
                            .Select(s => s.ManagementMode)
                            .FirstOrDefault(mode => !string.IsNullOrWhiteSpace(mode)),
                        contractSupports
                            .Select(s => ExtractRiskLevel(s.MifidRiskTolerance))
                            .Where(level => level.HasValue)
                            .DefaultIfEmpty(null)
                            .Max(),
                        supportsCurrentValue > 0m ? Math.Round(euroFundValue / supportsCurrentValue * 100m, 2) : null,
                        supportsCurrentValue > 0m ? Math.Round(unitLinkedValue / supportsCurrentValue * 100m, 2) : null,
                        null,
                        hasPendingOperation,
                        hasNewDocument,
                        BuildContractCapabilities(
                            c.Status,
                            c.Locked,
                            c.CurrentValue,
                            valueStatus,
                            hasPendingOperation));
                })
                .ToList();

            var recentOperationRows = await _db.Operations
                .AsNoTracking()
                .Where(o => o.Contract.PersonId == personId)
                .OrderByDescending(o => o.OperationDate)
                .ThenByDescending(o => o.Id)
                .Take(8)
                .Select(o => new
                {
                    o.Id,
                    o.ContractId,
                    o.Contract.ContractNumber,
                    o.Type,
                    o.Status,
                    o.OperationDate,
                    o.ExecutionDate,
                    o.Amount,
                    o.Currency
                })
                .ToListAsync(cancellationToken);

            var recentOperations = recentOperationRows
                .Select(o => new MeOperationSummaryDto(
                    o.Id,
                    o.ContractId,
                    o.ContractNumber,
                    o.Type.ToString(),
                    o.Status.ToString(),
                    o.OperationDate,
                    o.ExecutionDate,
                    o.Amount,
                    o.Currency))
                .ToList();

            var recentDocuments = await _db.Documents
                .AsNoTracking()
                .Where(d => d.Contract != null && d.Contract.PersonId == personId)
                .OrderByDescending(d => d.UploadedAt)
                .ThenByDescending(d => d.Id)
                .Take(8)
                .Select(d => new MeDocumentSummaryDto(
                    d.Id,
                    d.ContractId,
                    d.Contract != null ? d.Contract.ContractNumber : null,
                    d.FileName,
                    d.FileType,
                    d.UploadedAt,
                    d.Url))
                .ToListAsync(cancellationToken);

            var metrics = new MePrivateSpaceMetricsDto(
                contracts.Count,
                contracts.Sum(c => c.CurrentValue),
                contracts.Sum(c => c.TotalPaidPremiums),
                contracts.Sum(c => c.NetInvested),
                contracts.Count(c => c.HasAlert),
                contracts.Sum(c => c.DocumentCount),
                recentOperations.Select(o => (DateTime?)o.OperationDate).FirstOrDefault());

            var alerts = BuildPrivateSpaceAlerts(user, contracts, donationSummary);

            return new MePrivateSpaceDto(
                new MePersonAccessDto(
                    user.Person.Id,
                    user.Person.FirstName,
                    user.Person.LastName,
                    $"{user.Person.FirstName} {user.Person.LastName}".Trim(),
                    string.IsNullOrWhiteSpace(user.Person.Email1) ? null : user.Person.Email1,
                    string.IsNullOrWhiteSpace(user.Person.PhoneNumber) ? null : user.Person.PhoneNumber,
                    user.Person.BirthDate == default ? null : user.Person.BirthDate.Date,
                    user.Person.Role,
                    user.Person.Status),
                metrics,
                contracts,
                recentOperations,
                recentDocuments,
                donationSummary,
                alerts);
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

        private async Task<MeDonationSummaryDto> BuildDonationSummaryAsync(
            int userId,
            CancellationToken cancellationToken)
        {
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

            return new MeDonationSummaryDto(donationCount, confirmedTotal, documentCount, lastDonationDate);
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
                    "/back-office/donation-space/donations/new"));
            }

            if (documentCount > 0)
            {
                items.Add(new MeNewsItemDto(
                    now,
                    "Documents disponibles",
                    $"{documentCount} reçu(s) fiscal(aux) sont disponibles dans votre historique.",
                    "success",
                    "Consulter mes dons",
                    "/back-office/donation-space/donations"));
            }

            if (items.Count == 0)
            {
                items.Add(new MeNewsItemDto(
                    now,
                    "Votre espace est prêt",
                    "Votre compte est opérationnel. Retrouvez ici vos dons, reçus et prochaines actions.",
                    "success",
                    "Voir mes dons",
                    "/back-office/donation-space/donations"));
            }

            return items.Take(4).ToList();
        }

        private static IReadOnlyList<MeNewsItemDto> BuildPrivateSpaceAlerts(
            User user,
            IReadOnlyList<MeContractSummaryDto> contracts,
            MeDonationSummaryDto donationSummary)
        {
            var now = DateTime.UtcNow;
            var items = new List<MeNewsItemDto>();

            if (!user.EmailConfirmed)
            {
                items.Add(new MeNewsItemDto(now, "Adresse e-mail à confirmer", "Confirmez votre adresse e-mail pour sécuriser l'accès à vos informations.", "warning", null, null));
            }

            var alertCount = contracts.Count(c => c.HasAlert);
            if (alertCount > 0)
            {
                items.Add(new MeNewsItemDto(now, "Contrats à suivre", $"{alertCount} contrat(s) comportent une alerte de suivi.", "warning", null, null));
            }

            if (contracts.Count == 0)
            {
                items.Add(new MeNewsItemDto(now, "Aucun contrat visible", "Aucun contrat n'est actuellement rattaché à votre fiche personne.", "info", null, null));
            }

            if (donationSummary.AvailableDocumentCount > 0)
            {
                items.Add(new MeNewsItemDto(now, "Reçus fiscaux disponibles", $"{donationSummary.AvailableDocumentCount} document(s) de don sont disponibles.", "success", "Voir mes dons", "/back-office/donation-space/donations"));
            }

            if (items.Count == 0)
            {
                items.Add(new MeNewsItemDto(now, "Espace à jour", "Vos contrats et vos informations personnelles sont accessibles depuis cet espace.", "success", null, null));
            }

            return items.Take(4).ToList();
        }

        private static string ResolveContractValueStatus(
            string status,
            decimal currentValue,
            decimal totalPaidPremiums,
            DateTime? lastValuationDate)
        {
            var normalizedStatus = NormalizeStatus(status);
            if (normalizedStatus.Contains("activation") || normalizedStatus.Contains("ouverture"))
            {
                return "activating";
            }

            if (currentValue == 0m && totalPaidPremiums == 0m)
            {
                return "notFunded";
            }

            if (currentValue == 0m && !lastValuationDate.HasValue)
            {
                return "unavailable";
            }

            if (currentValue == 0m)
            {
                return "zero";
            }

            return "known";
        }

        private static MeContractCapabilitiesDto BuildContractCapabilities(
            string status,
            bool locked,
            decimal currentValue,
            string valueStatus,
            bool hasPendingOperation)
        {
            var disabledReasons = new Dictionary<string, string>();
            var normalizedStatus = NormalizeStatus(status);
            var closed = normalizedStatus.Contains("clos") || normalizedStatus.Contains("resilie");
            var activating = normalizedStatus.Contains("activation") || normalizedStatus.Contains("ouverture");
            var valueUnavailable = valueStatus is "unavailable" or "pending" or "error";

            if (closed)
            {
                disabledReasons["all"] = "Contrat clôturé.";
            }
            else if (activating)
            {
                disabledReasons["all"] = "Contrat en cours d'activation.";
            }
            else if (locked)
            {
                disabledReasons["all"] = "Contrat verrouillé.";
            }
            else if (hasPendingOperation)
            {
                disabledReasons["all"] = "Une opération est déjà en cours sur ce contrat.";
            }

            if (valueUnavailable)
            {
                disabledReasons["value"] = "Valeur du contrat indisponible.";
            }

            if (currentValue <= 0m)
            {
                disabledReasons["fundedValue"] = "Valeur insuffisante pour cette opération.";
            }

            var commonAvailable = !disabledReasons.ContainsKey("all");
            var valueAvailable = !disabledReasons.ContainsKey("value");
            var funded = !disabledReasons.ContainsKey("fundedValue");

            return new MeContractCapabilitiesDto(
                CanMakePayment: commonAvailable,
                CanArbitrate: commonAvailable && valueAvailable && funded,
                CanRedeem: commonAvailable && valueAvailable && funded,
                CanSchedulePayments: commonAvailable,
                CanUpdateBeneficiaryClause: commonAvailable,
                CanChangeManagementMode: commonAvailable && valueAvailable,
                DisabledReasons: disabledReasons);
        }

        private static int? ExtractRiskLevel(string? risk)
        {
            if (string.IsNullOrWhiteSpace(risk)) return null;

            foreach (var c in risk)
            {
                if (char.IsDigit(c))
                {
                    var value = c - '0';
                    if (value is >= 1 and <= 7)
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        private static string NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return string.Empty;

            return status
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .Aggregate(string.Empty, (current, c) => current + c)
                .ToLowerInvariant()
                .Trim();
        }

        [GeneratedRegex(@"^\d{5}$")]
        private static partial Regex FrenchPostalCodeRegex();
    }
}
