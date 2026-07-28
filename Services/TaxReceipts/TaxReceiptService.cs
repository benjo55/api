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
    public sealed class TaxReceiptService : ITaxReceiptService
    {
        private readonly ApplicationDBContext _db;
        private readonly ITaxReceiptNumberGenerator _numberGenerator;
        private readonly IEnumerable<ITaxReceiptPdfGenerator> _pdfGenerators;
        private readonly IDocumentBinaryStorage _storage;

        public TaxReceiptService(
            ApplicationDBContext db,
            ITaxReceiptNumberGenerator numberGenerator,
            IEnumerable<ITaxReceiptPdfGenerator> pdfGenerators,
            IDocumentBinaryStorage storage)
        {
            _db = db;
            _numberGenerator = numberGenerator;
            _pdfGenerators = pdfGenerators;
            _storage = storage;
        }

        public async Task<PagedResult<TaxReceiptDto>> GetAllAsync(api.Helpers.QueryObject query, CancellationToken cancellationToken = default)
        {
            var receipts = _db.TaxReceipts
                .Include(x => x.Donation)
                    .ThenInclude(x => x.Donor)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<TaxReceiptStatus>(query.Status, true, out var status))
            {
                receipts = receipts.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                receipts = receipts.Where(x =>
                    x.ReceiptNumber.Contains(query.Search) ||
                    x.Donation.Donor.LastName.Contains(query.Search) ||
                    x.Donation.Donor.FirstName.Contains(query.Search));
            }

            receipts = query.SortBy switch
            {
                "receiptNumber" => query.IsDescending ? receipts.OrderByDescending(x => x.ReceiptNumber) : receipts.OrderBy(x => x.ReceiptNumber),
                "donor" => query.IsDescending ? receipts.OrderByDescending(x => x.Donation.Donor.LastName) : receipts.OrderBy(x => x.Donation.Donor.LastName),
                _ => receipts.OrderByDescending(x => x.CreatedAt)
            };

            var totalCount = await receipts.CountAsync(cancellationToken);
            var pageSize = Math.Max(1, query.PageSize);
            var pageNumber = Math.Max(1, query.PageNumber);
            var items = await receipts.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return new PagedResult<TaxReceiptDto>
            {
                Items = items.Select(x => x.ToDto()).ToList(),
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasNextPage = pageNumber * pageSize < totalCount,
                CurrentPage = pageNumber
            };
        }

        public async Task<TaxReceiptDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var receipt = await LoadReceiptQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            return receipt?.ToDto();
        }

        public async Task<TaxReceiptDto> CreateForDonationAsync(int donationId, CreateTaxReceiptDto dto, string? userName, CancellationToken cancellationToken = default)
        {
            var cerfaCode = string.IsNullOrWhiteSpace(dto.CerfaCode) ? "2041-RD" : dto.CerfaCode.Trim();
            var cerfaVersion = string.IsNullOrWhiteSpace(dto.CerfaVersion) ? "11580*05" : dto.CerfaVersion.Trim();
            EnsureGenerator(cerfaCode, cerfaVersion);

            if (!string.IsNullOrWhiteSpace(dto.GenerationRequestKey))
            {
                var existingByKey = await LoadReceiptQuery()
                    .FirstOrDefaultAsync(x => x.GenerationRequestKey == dto.GenerationRequestKey, cancellationToken);
                if (existingByKey is not null)
                {
                    return existingByKey.ToDto();
                }
            }

            var donation = await _db.Donations
                .Include(x => x.Donor)
                .Include(x => x.TaxReceipts)
                .FirstOrDefaultAsync(x => x.Id == donationId, cancellationToken)
                ?? throw new BusinessException("DonationNotFound");

            if (donation.Status is not (DonationStatus.Validated or DonationStatus.Paid or DonationStatus.Completed or DonationStatus.ReceiptGenerated))
            {
                throw new BusinessException("DonationIncomplete");
            }

            if (donation.TaxReceipts.Any(x => x.Status is TaxReceiptStatus.Ready or TaxReceiptStatus.Generated or TaxReceiptStatus.Sent or TaxReceiptStatus.EmailFailed))
            {
                throw new BusinessException("TaxReceiptAlreadyGenerated");
            }

            var organization = await _db.BeneficiaryOrganizations
                .FirstOrDefaultAsync(x => x.Id == dto.BeneficiaryOrganizationId && x.IsActive, cancellationToken)
                ?? throw new BusinessException("BeneficiaryOrganizationNotFound");

            var receipt = new TaxReceipt
            {
                DonationId = donation.Id,
                BeneficiaryOrganizationId = organization.Id,
                CerfaCode = cerfaCode,
                CerfaVersion = cerfaVersion,
                ReceiptNumber = await _numberGenerator.GenerateAsync(cancellationToken),
                Status = TaxReceiptStatus.Ready,
                GenerationRequestKey = string.IsNullOrWhiteSpace(dto.GenerationRequestKey)
                    ? Guid.NewGuid().ToString("N")
                    : dto.GenerationRequestKey.Trim(),
                GeneratedBy = userName
            };

            _db.TaxReceipts.Add(receipt);
            await _db.SaveChangesAsync(cancellationToken);
            var loaded = await LoadReceiptQuery().FirstAsync(x => x.Id == receipt.Id, cancellationToken);
            return loaded.ToDto();
        }

        public async Task<TaxReceiptDto?> ValidateAsync(int id, CancellationToken cancellationToken = default)
        {
            var receipt = await LoadReceiptQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (receipt is null)
            {
                return null;
            }

            ValidateForGeneration(receipt);
            receipt.Status = TaxReceiptStatus.Ready;
            receipt.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return receipt.ToDto();
        }

        public async Task<TaxReceiptGenerationResultDto> GenerateAsync(int id, string? userName, CancellationToken cancellationToken = default)
        {
            var receipt = await LoadReceiptQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new BusinessException("TaxReceiptNotFound");

            if (receipt.Status is TaxReceiptStatus.Sent)
            {
                return new TaxReceiptGenerationResultDto(receipt.ToDto(), $"/api/tax-receipts/{receipt.Id}/pdf");
            }

            ValidateForGeneration(receipt);
            var generator = EnsureGenerator(receipt.CerfaCode, receipt.CerfaVersion);

            try
            {
                receipt.GeneratedAt = DateTime.UtcNow;
                receipt.GeneratedBy = userName;
                var pdf = await generator.GenerateAsync(receipt, cancellationToken);
                if (pdf.Length == 0 || !pdf.Take(4).SequenceEqual("%PDF"u8.ToArray()))
                {
                    throw new BusinessException("PdfGenerationFailed");
                }

                var fileName = BuildFileName(receipt);
                var saved = await _storage.SaveAsync(pdf, ".pdf", cancellationToken);
                var artifact = new DocumentArtifact
                {
                    Type = DocumentArtifactType.IssuedPdf,
                    StorageKey = saved.StorageKey,
                    ContentType = "application/pdf",
                    FileName = fileName,
                    Hash = saved.Hash,
                    Size = saved.Size,
                    GeneratedBy = userName
                };

                _db.DocumentArtifacts.Add(artifact);
                await _db.SaveChangesAsync(cancellationToken);

                receipt.DocumentArtifactId = artifact.Id;
                receipt.GeneratedFileName = fileName;
                receipt.PdfHash = saved.Hash;
                receipt.Status = TaxReceiptStatus.Generated;
                receipt.UpdatedAt = DateTime.UtcNow;
                receipt.Donation.Status = DonationStatus.ReceiptGenerated;
                receipt.Donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);

                return new TaxReceiptGenerationResultDto(receipt.ToDto(), $"/api/tax-receipts/{receipt.Id}/pdf");
            }
            catch
            {
                receipt.Status = TaxReceiptStatus.GenerationFailed;
                receipt.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                throw;
            }
        }

        public async Task<(byte[] Content, string FileName)> GetPdfAsync(int id, CancellationToken cancellationToken = default)
        {
            var receipt = await _db.TaxReceipts
                .Include(x => x.DocumentArtifact)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new BusinessException("TaxReceiptNotFound");

            if (receipt.DocumentArtifact is null || string.IsNullOrWhiteSpace(receipt.PdfHash))
            {
                throw new BusinessException("TaxReceiptPdfNotGenerated");
            }

            var content = await _storage.ReadAsync(receipt.DocumentArtifact.StorageKey, cancellationToken);
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
            if (!string.Equals(hash, receipt.PdfHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException("TaxReceiptPdfHashMismatch");
            }

            return (content, receipt.GeneratedFileName ?? receipt.DocumentArtifact.FileName);
        }

        public async Task<TaxReceiptDto?> CancelAsync(int id, string? reason, CancellationToken cancellationToken = default)
        {
            var receipt = await LoadReceiptQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (receipt is null)
            {
                return null;
            }

            if (receipt.Status is TaxReceiptStatus.Replaced)
            {
                throw new BusinessException("TaxReceiptCannotBeCancelled");
            }

            receipt.Status = TaxReceiptStatus.Cancelled;
            receipt.CancelledAt = DateTime.UtcNow;
            receipt.CancellationReason = reason;
            receipt.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return receipt.ToDto();
        }

        public async Task<TaxReceiptDto> ReplaceAsync(int id, string? userName, CancellationToken cancellationToken = default)
        {
            var original = await LoadReceiptQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new BusinessException("TaxReceiptNotFound");

            if (original.Status is not (TaxReceiptStatus.Generated or TaxReceiptStatus.Sent))
            {
                throw new BusinessException("TaxReceiptCannotBeReplaced");
            }

            original.Status = TaxReceiptStatus.Replaced;
            original.UpdatedAt = DateTime.UtcNow;

            var replacement = new TaxReceipt
            {
                DonationId = original.DonationId,
                BeneficiaryOrganizationId = original.BeneficiaryOrganizationId,
                CerfaCode = original.CerfaCode,
                CerfaVersion = original.CerfaVersion,
                ReceiptNumber = await _numberGenerator.GenerateAsync(cancellationToken),
                Status = TaxReceiptStatus.Ready,
                GenerationRequestKey = Guid.NewGuid().ToString("N"),
                GeneratedBy = userName
            };

            _db.TaxReceipts.Add(replacement);
            await _db.SaveChangesAsync(cancellationToken);
            original.ReplacementReceiptId = replacement.Id;
            await _db.SaveChangesAsync(cancellationToken);

            var loaded = await LoadReceiptQuery().FirstAsync(x => x.Id == replacement.Id, cancellationToken);
            return loaded.ToDto();
        }

        public async Task<IReadOnlyList<TaxReceiptEmailHistoryDto>> GetEmailHistoryAsync(int id, CancellationToken cancellationToken = default)
        {
            return (await _db.TaxReceiptEmailHistory
                    .AsNoTracking()
                    .Where(x => x.TaxReceiptId == id)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync(cancellationToken))
                .Select(x => x.ToDto())
                .ToList();
        }

        private IQueryable<TaxReceipt> LoadReceiptQuery() =>
            _db.TaxReceipts
                .Include(x => x.Donation)
                    .ThenInclude(x => x.Donor)
                .Include(x => x.Donation)
                    .ThenInclude(x => x.DonorSnapshot)
                .Include(x => x.BeneficiaryOrganization)
                .Include(x => x.DocumentArtifact);

        private ITaxReceiptPdfGenerator EnsureGenerator(string cerfaCode, string cerfaVersion)
        {
            var generator = _pdfGenerators.FirstOrDefault(x =>
                string.Equals(x.CerfaCode, cerfaCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.CerfaVersion, cerfaVersion, StringComparison.OrdinalIgnoreCase));
            return generator ?? throw new BusinessException("CerfaTemplateNotFound");
        }

        private static void ValidateForGeneration(TaxReceipt receipt)
        {
            if (receipt.Donation.Status is not (DonationStatus.Validated or DonationStatus.Paid or DonationStatus.Completed or DonationStatus.ReceiptGenerated))
            {
                throw new BusinessException("DonationIncomplete");
            }

            DonationService.ValidateBusinessRules(receipt.Donation);

            var donorSnapshot = receipt.Donation.DonorSnapshot;
            var donor = receipt.Donation.Donor;
            var hasCompleteSnapshot = donorSnapshot is not null
                && !string.IsNullOrWhiteSpace(donorSnapshot.LastName)
                && !string.IsNullOrWhiteSpace(donorSnapshot.FirstName)
                && !string.IsNullOrWhiteSpace(donorSnapshot.AddressLine1)
                && !string.IsNullOrWhiteSpace(donorSnapshot.PostalCode)
                && !string.IsNullOrWhiteSpace(donorSnapshot.City)
                && !string.IsNullOrWhiteSpace(donorSnapshot.Country);
            var hasCompleteDonor = !string.IsNullOrWhiteSpace(donor.LastName) &&
                !string.IsNullOrWhiteSpace(donor.FirstName) &&
                !string.IsNullOrWhiteSpace(donor.StreetName) &&
                !string.IsNullOrWhiteSpace(donor.PostalCode) &&
                !string.IsNullOrWhiteSpace(donor.City) &&
                !string.IsNullOrWhiteSpace(donor.CountryCode);

            if (!hasCompleteSnapshot && !hasCompleteDonor)
            {
                throw new BusinessException("DonorIncomplete");
            }

            var organization = receipt.BeneficiaryOrganization;
            if (!organization.IsActive ||
                string.IsNullOrWhiteSpace(organization.Name) ||
                string.IsNullOrWhiteSpace(organization.Identifier) ||
                string.IsNullOrWhiteSpace(organization.StreetName) ||
                string.IsNullOrWhiteSpace(organization.PostalCode) ||
                string.IsNullOrWhiteSpace(organization.City) ||
                string.IsNullOrWhiteSpace(organization.CountryCode) ||
                string.IsNullOrWhiteSpace(organization.Purpose))
            {
                throw new BusinessException("BeneficiaryOrganizationIncomplete");
            }
        }

        private static string BuildFileName(TaxReceipt receipt)
        {
            var donor = receipt.Donation.Donor;
            var donorName = string.Join("_", new[] { donor.LastName, donor.FirstName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                .Replace(" ", "_");
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                donorName = donorName.Replace(invalid, '_');
            }

            return $"CERFA_{receipt.CerfaCode}_{receipt.ReceiptNumber}_{donorName}.pdf";
        }
    }
}
