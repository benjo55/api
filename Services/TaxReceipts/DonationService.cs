using api.Data;
using api.Dtos.Generic;
using api.Dtos.TaxReceipts;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.TaxReceipts
{
    public sealed class DonationService : IDonationService
    {
        private readonly ApplicationDBContext _db;

        public DonationService(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<DonationDto>> GetAllAsync(api.Helpers.QueryObject query, CancellationToken cancellationToken = default)
        {
            var donations = _db.Donations.Include(x => x.Donor).AsNoTracking().AsQueryable();

            if (query.PersonId is not null)
            {
                donations = donations.Where(x => x.DonorId == query.PersonId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<DonationStatus>(query.Status, true, out var status))
            {
                donations = donations.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                donations = donations.Where(x =>
                    x.Reference != null && x.Reference.Contains(query.Search) ||
                    x.Donor.LastName.Contains(query.Search) ||
                    x.Donor.FirstName.Contains(query.Search));
            }

            donations = query.SortBy switch
            {
                "amount" => query.IsDescending ? donations.OrderByDescending(x => x.Amount) : donations.OrderBy(x => x.Amount),
                "donor" => query.IsDescending ? donations.OrderByDescending(x => x.Donor.LastName) : donations.OrderBy(x => x.Donor.LastName),
                _ => query.IsDescending ? donations.OrderByDescending(x => x.DonationDate) : donations.OrderBy(x => x.DonationDate)
            };

            var totalCount = await donations.CountAsync(cancellationToken);
            var pageSize = Math.Max(1, query.PageSize);
            var pageNumber = Math.Max(1, query.PageNumber);
            var items = await donations.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return new PagedResult<DonationDto>
            {
                Items = items.Select(x => x.ToDto()).ToList(),
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasNextPage = pageNumber * pageSize < totalCount,
                CurrentPage = pageNumber
            };
        }

        public async Task<DonationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var donation = await _db.Donations.Include(x => x.Donor).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            return donation?.ToDto();
        }

        public async Task<DonationDto> CreateAsync(SaveDonationDto dto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(dto, cancellationToken);
            var donation = new Donation();
            Apply(donation, dto);
            if (donation.OrganizationId <= 0)
            {
                donation.OrganizationId = await GetDefaultOrganizationIdAsync(cancellationToken);
            }
            _db.Donations.Add(donation);
            await _db.SaveChangesAsync(cancellationToken);
            await _db.Entry(donation).Reference(x => x.Donor).LoadAsync(cancellationToken);
            return donation.ToDto();
        }

        public async Task<DonationDto?> UpdateAsync(int id, SaveDonationDto dto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(dto, cancellationToken);
            var donation = await _db.Donations
                .Include(x => x.Donor)
                .Include(x => x.TaxReceipts)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (donation is null)
            {
                return null;
            }

            if (donation.TaxReceipts.Any(x => x.Status is TaxReceiptStatus.Generated or TaxReceiptStatus.Sent or TaxReceiptStatus.EmailFailed))
            {
                throw new BusinessException("TaxReceiptAlreadyGenerated");
            }

            Apply(donation, dto);
            if (donation.OrganizationId <= 0)
            {
                donation.OrganizationId = await GetDefaultOrganizationIdAsync(cancellationToken);
            }
            donation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return donation.ToDto();
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var donation = await _db.Donations
                .Include(x => x.TaxReceipts)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (donation is null)
            {
                return false;
            }

            if (donation.TaxReceipts.Any())
            {
                throw new BusinessException("DonationHasTaxReceipts");
            }

            _db.Donations.Remove(donation);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<DonationDto?> ValidateAsync(int id, CancellationToken cancellationToken = default)
        {
            var donation = await _db.Donations.Include(x => x.Donor).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (donation is null)
            {
                return null;
            }

            ValidateBusinessRules(donation);
            donation.Status = DonationStatus.Validated;
            donation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return donation.ToDto();
        }

        public async Task<DonationDto?> CancelAsync(int id, CancellationToken cancellationToken = default)
        {
            var donation = await _db.Donations.Include(x => x.Donor).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (donation is null)
            {
                return null;
            }

            donation.IsCancelled = true;
            donation.Status = DonationStatus.Cancelled;
            donation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return donation.ToDto();
        }

        public async Task<IReadOnlyList<TaxReceiptDto>> GetReceiptsAsync(int donationId, CancellationToken cancellationToken = default)
        {
            var receipts = await _db.TaxReceipts
                .Include(x => x.Donation)
                    .ThenInclude(x => x.Donor)
                .AsNoTracking()
                .Where(x => x.DonationId == donationId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return receipts.Select(x => x.ToDto()).ToList();
        }

        private async Task ValidateAsync(SaveDonationDto dto, CancellationToken cancellationToken)
        {
            if (!await _db.Donors.AnyAsync(x => x.Id == dto.DonorId && !x.IsArchived, cancellationToken))
            {
                throw new BusinessException("DonorNotFound");
            }

            var organizationId = dto.OrganizationId;
            if (!organizationId.HasValue)
            {
                organizationId = await _db.BeneficiaryOrganizations
                    .Where(x => x.IsActive)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (!organizationId.HasValue)
            {
                throw new BusinessException("BeneficiaryOrganizationNotFound");
            }

            if (!await _db.BeneficiaryOrganizations.AnyAsync(x => x.Id == organizationId.Value, cancellationToken))
            {
                throw new BusinessException("BeneficiaryOrganizationNotFound");
            }

            var candidate = new Donation();
            Apply(candidate, dto);
            candidate.OrganizationId = organizationId.Value;
            ValidateBusinessRules(candidate);
        }

        private static void Apply(Donation donation, SaveDonationDto dto)
        {
            if (dto.OrganizationId.HasValue)
            {
                donation.OrganizationId = dto.OrganizationId.Value;
            }
            donation.DonorId = dto.DonorId;
            donation.DonationDate = dto.DonationDate;
            donation.Amount = dto.Amount;
            donation.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EUR" : dto.Currency.Trim().ToUpperInvariant();
            donation.DonationForm = dto.DonationForm;
            donation.OtherFormDescription = Clean(dto.OtherFormDescription);
            donation.DonationNature = dto.DonationNature;
            donation.OtherNatureDescription = Clean(dto.OtherNatureDescription);
            donation.PaymentMethod = dto.PaymentMethod;
            donation.TaxRegime = dto.TaxRegime;
            donation.Article200Amount = dto.Article200Amount;
            donation.Article978Amount = dto.Article978Amount;
            donation.Reference = Clean(dto.Reference);
            donation.ExternalReference = Clean(dto.ExternalReference);
            donation.Comments = Clean(dto.Comments);
        }

        public static void ValidateBusinessRules(Donation donation)
        {
            if (donation.Amount <= 0)
            {
                throw new BusinessException("DonationAmountMustBePositive");
            }

            if (donation.DonationDate == default)
            {
                throw new BusinessException("DonationDateRequired");
            }

            if (donation.DonationNature == DonationNature.Cash && donation.PaymentMethod is null)
            {
                throw new BusinessException("DonationPaymentMethodRequired");
            }

            if (donation.DonationForm == DonationForm.Other && string.IsNullOrWhiteSpace(donation.OtherFormDescription))
            {
                throw new BusinessException("OtherFormDescriptionRequired");
            }

            if (donation.DonationNature == DonationNature.Other && string.IsNullOrWhiteSpace(donation.OtherNatureDescription))
            {
                throw new BusinessException("OtherNatureDescriptionRequired");
            }

            var allocated = (donation.Article200Amount ?? 0m) + (donation.Article978Amount ?? 0m);
            if (allocated > donation.Amount)
            {
                throw new BusinessException("DonationFiscalAllocationExceedsAmount");
            }
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task<int> GetDefaultOrganizationIdAsync(CancellationToken cancellationToken)
        {
            var activeOrganizationId = await _db.BeneficiaryOrganizations
                .Where(x => x.IsActive)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (!activeOrganizationId.HasValue)
            {
                throw new BusinessException("BeneficiaryOrganizationNotFound");
            }

            return activeOrganizationId.Value;
        }
    }
}
