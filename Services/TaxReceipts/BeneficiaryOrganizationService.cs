using api.Data;
using api.Dtos.TaxReceipts;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services.TaxReceipts
{
    public sealed class BeneficiaryOrganizationService : IBeneficiaryOrganizationService
    {
        private readonly ApplicationDBContext _db;

        public BeneficiaryOrganizationService(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<BeneficiaryOrganizationDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var organizations = await _db.BeneficiaryOrganizations
                .AsNoTracking()
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);

            return organizations.Select(x => x.ToDto()).ToList();
        }

        public async Task<BeneficiaryOrganizationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var organization = await _db.BeneficiaryOrganizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            return organization?.ToDto();
        }

        public async Task<BeneficiaryOrganizationDto?> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            var organization = await _db.BeneficiaryOrganizations.AsNoTracking().FirstOrDefaultAsync(x => x.IsActive, cancellationToken);
            return organization?.ToDto();
        }

        public async Task<BeneficiaryOrganizationDto> CreateAsync(SaveBeneficiaryOrganizationDto dto, CancellationToken cancellationToken = default)
        {
            Validate(dto);
            var organization = new BeneficiaryOrganization();
            await ApplyAsync(organization, dto, cancellationToken);
            _db.BeneficiaryOrganizations.Add(organization);
            await _db.SaveChangesAsync(cancellationToken);
            return organization.ToDto();
        }

        public async Task<BeneficiaryOrganizationDto?> UpdateAsync(int id, SaveBeneficiaryOrganizationDto dto, CancellationToken cancellationToken = default)
        {
            Validate(dto);
            var organization = await _db.BeneficiaryOrganizations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (organization is null)
            {
                return null;
            }

            await ApplyAsync(organization, dto, cancellationToken);
            organization.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return organization.ToDto();
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var organization = await _db.BeneficiaryOrganizations
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (organization is null)
            {
                return false;
            }

            if (organization.IsActive)
            {
                throw new BusinessException("ActiveBeneficiaryOrganizationCannotBeDeleted");
            }

            var hasReceipts = await _db.TaxReceipts
                .AnyAsync(x => x.BeneficiaryOrganizationId == id, cancellationToken);
            if (hasReceipts)
            {
                throw new BusinessException("BeneficiaryOrganizationHasTaxReceipts");
            }

            _db.BeneficiaryOrganizations.Remove(organization);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task ApplyAsync(BeneficiaryOrganization organization, SaveBeneficiaryOrganizationDto dto, CancellationToken cancellationToken)
        {
            if (dto.IsActive)
            {
                await _db.BeneficiaryOrganizations
                    .Where(x => x.Id != organization.Id && x.IsActive)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, false), cancellationToken);
            }

            organization.Name = dto.Name.Trim();
            organization.IdentifierType = dto.IdentifierType;
            organization.Identifier = dto.Identifier.Trim();
            organization.StreetNumber = Clean(dto.StreetNumber);
            organization.StreetName = dto.StreetName.Trim();
            organization.AddressGeoJson = Clean(dto.AddressGeoJson);
            organization.AddressLine2 = Clean(dto.AddressLine2);
            organization.PostalCode = dto.PostalCode.Trim();
            organization.City = dto.City.Trim();
            organization.CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? "FR" : dto.CountryCode.Trim().ToUpperInvariant();
            organization.Purpose = dto.Purpose.Trim();
            organization.OrganizationCategory = dto.OrganizationCategory;
            organization.OrganizationSubCategory = dto.OrganizationSubCategory;
            organization.OtherCategoryDescription = Clean(dto.OtherCategoryDescription);
            organization.RecognitionDecreeDate = dto.RecognitionDecreeDate;
            organization.RecognitionOfficialJournalDate = dto.RecognitionOfficialJournalDate;
            organization.ApprovalDate = dto.ApprovalDate;
            organization.IsDonationEnabled = dto.IsDonationEnabled;
            organization.IsEligibleForTaxReceipt = dto.IsEligibleForTaxReceipt;
            organization.HelloAssoOrganizationSlug = Clean(dto.HelloAssoOrganizationSlug);
            organization.IsHelloAssoPaymentEnabled = dto.IsHelloAssoPaymentEnabled;
            organization.IsBankTransferEnabled = dto.IsBankTransferEnabled;
            organization.IsPayPalEnabled = dto.IsPayPalEnabled;
            organization.PayPalMerchantAlias = Clean(dto.PayPalMerchantAlias);
            organization.IsActive = dto.IsActive;
        }

        private static void Validate(SaveBeneficiaryOrganizationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) ||
                string.IsNullOrWhiteSpace(dto.Identifier) ||
                string.IsNullOrWhiteSpace(dto.StreetName) ||
                string.IsNullOrWhiteSpace(dto.PostalCode) ||
                string.IsNullOrWhiteSpace(dto.City) ||
                string.IsNullOrWhiteSpace(dto.CountryCode) ||
                string.IsNullOrWhiteSpace(dto.Purpose))
            {
                throw new BusinessException("BeneficiaryOrganizationIncomplete");
            }
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
