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
    public sealed class DonorService : IDonorService
    {
        private readonly ApplicationDBContext _db;

        public DonorService(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<DonorDto>> GetAllAsync(api.Helpers.QueryObject query, CancellationToken cancellationToken = default)
        {
            var donors = _db.Donors.Include(x => x.Donations).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var archived = string.Equals(query.Status, "Archived", StringComparison.OrdinalIgnoreCase);
                donors = donors.Where(x => x.IsArchived == archived);
            }
            else
            {
                donors = donors.Where(x => !x.IsArchived);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                donors = donors.Where(x =>
                    x.LastName.Contains(query.Search) ||
                    x.FirstName.Contains(query.Search) ||
                    (x.Email != null && x.Email.Contains(query.Search)) ||
                    (x.CompanyName != null && x.CompanyName.Contains(query.Search)));
            }

            donors = query.SortBy switch
            {
                "firstName" => query.IsDescending ? donors.OrderByDescending(x => x.FirstName) : donors.OrderBy(x => x.FirstName),
                "email" => query.IsDescending ? donors.OrderByDescending(x => x.Email) : donors.OrderBy(x => x.Email),
                "city" => query.IsDescending ? donors.OrderByDescending(x => x.City) : donors.OrderBy(x => x.City),
                _ => query.IsDescending ? donors.OrderByDescending(x => x.LastName) : donors.OrderBy(x => x.LastName)
            };

            var totalCount = await donors.CountAsync(cancellationToken);
            var pageSize = Math.Max(1, query.PageSize);
            var pageNumber = Math.Max(1, query.PageNumber);
            var items = await donors
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<DonorDto>
            {
                Items = items.Select(x => x.ToDto()).ToList(),
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasNextPage = pageNumber * pageSize < totalCount,
                CurrentPage = pageNumber
            };
        }

        public async Task<DonorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var donor = await _db.Donors
                .Include(x => x.Donations)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            return donor?.ToDto();
        }

        public async Task<DonorDto> CreateAsync(SaveDonorDto dto, CancellationToken cancellationToken = default)
        {
            Validate(dto);
            var donor = new Donor();
            Apply(donor, dto);
            _db.Donors.Add(donor);
            await _db.SaveChangesAsync(cancellationToken);
            return donor.ToDto();
        }

        public async Task<DonorDto?> UpdateAsync(int id, SaveDonorDto dto, CancellationToken cancellationToken = default)
        {
            Validate(dto);
            var donor = await _db.Donors.Include(x => x.Donations).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (donor is null)
            {
                return null;
            }

            Apply(donor, dto);
            donor.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return donor.ToDto();
        }

        public async Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default)
        {
            var donor = await _db.Donors.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (donor is null)
            {
                return false;
            }

            donor.IsArchived = true;
            donor.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IReadOnlyList<DonationDto>> GetDonationsAsync(int donorId, CancellationToken cancellationToken = default)
        {
            var donations = await _db.Donations
                .Include(x => x.Donor)
                .AsNoTracking()
                .Where(x => x.DonorId == donorId)
                .OrderByDescending(x => x.DonationDate)
                .ToListAsync(cancellationToken);

            return donations.Select(x => x.ToDto()).ToList();
        }

        public async Task<IReadOnlyList<DonorDto>> FindDuplicatesAsync(SaveDonorDto dto, CancellationToken cancellationToken = default)
        {
            var lastName = dto.LastName.Trim();
            var firstName = dto.FirstName.Trim();
            var email = dto.Email?.Trim();

            var candidates = await _db.Donors
                .Include(x => x.Donations)
                .AsNoTracking()
                .Where(x =>
                    (!string.IsNullOrEmpty(email) && x.Email == email) ||
                    (x.LastName == lastName && x.FirstName == firstName && x.PostalCode == dto.PostalCode))
                .Take(20)
                .ToListAsync(cancellationToken);

            return candidates.Select(x => x.ToDto()).ToList();
        }

        private static void Apply(Donor donor, SaveDonorDto dto)
        {
            donor.DonorType = dto.DonorType;
            donor.Title = Clean(dto.Title);
            donor.LastName = dto.LastName.Trim();
            donor.FirstName = dto.FirstName.Trim();
            donor.CompanyName = Clean(dto.CompanyName);
            donor.Email = Clean(dto.Email);
            donor.Phone = Clean(dto.Phone);
            donor.AddressLine1 = dto.AddressLine1.Trim();
            donor.AddressGeoJson = Clean(dto.AddressGeoJson);
            donor.AddressLine2 = Clean(dto.AddressLine2);
            donor.StreetNumber = Clean(dto.StreetNumber);
            donor.StreetName = dto.StreetName.Trim();
            donor.PostalCode = dto.PostalCode.Trim();
            donor.City = dto.City.Trim();
            donor.CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? "FR" : dto.CountryCode.Trim().ToUpperInvariant();
            donor.Notes = Clean(dto.Notes);
        }

        private static void Validate(SaveDonorDto dto)
        {
            if (dto.DonorType == DonorType.Company && string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                throw new BusinessException("DonorCompanyNameRequired");
            }

            if (dto.DonorType == DonorType.Individual && (string.IsNullOrWhiteSpace(dto.LastName) || string.IsNullOrWhiteSpace(dto.FirstName)))
            {
                throw new BusinessException("DonorNameRequired");
            }

            if (string.IsNullOrWhiteSpace(dto.AddressLine1) ||
                string.IsNullOrWhiteSpace(dto.StreetName) ||
                string.IsNullOrWhiteSpace(dto.PostalCode) ||
                string.IsNullOrWhiteSpace(dto.City) ||
                string.IsNullOrWhiteSpace(dto.CountryCode))
            {
                throw new BusinessException("DonorIncomplete");
            }
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
