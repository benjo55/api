using api.Data;
using api.Dtos.Product;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class ProductCatalogRepository : IProductCatalogRepository
    {
        private readonly ApplicationDBContext _context;

        public ProductCatalogRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<List<ProductCategoryDto>> GetCategoriesAsync() =>
            await _context.ProductCategories
                .OrderBy(x => x.Name)
                .Select(x => ToDto(x))
                .ToListAsync();

        public async Task<ProductCategoryDto?> GetCategoryAsync(int id) =>
            await _context.ProductCategories
                .Where(x => x.Id == id)
                .Select(x => ToDto(x))
                .FirstOrDefaultAsync();

        public async Task<ProductCategoryDto> CreateCategoryAsync(UpsertProductCategoryDto dto)
        {
            var entity = new ProductCategory
            {
                Code = dto.Code.Trim().ToUpperInvariant(),
                Name = dto.Name.Trim(),
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedDate = DateTime.UtcNow,
            };
            _context.ProductCategories.Add(entity);
            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ProductCategoryDto?> UpdateCategoryAsync(int id, UpsertProductCategoryDto dto)
        {
            var entity = await _context.ProductCategories.FindAsync(id);
            if (entity is null) return null;
            entity.Code = dto.Code.Trim().ToUpperInvariant();
            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description;
            entity.IsActive = dto.IsActive;
            entity.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<List<LegalNatureDto>> GetLegalNaturesAsync() =>
            await _context.LegalNatures
                .OrderBy(x => x.Name)
                .Select(x => ToDto(x))
                .ToListAsync();

        public async Task<LegalNatureDto?> GetLegalNatureAsync(int id) =>
            await _context.LegalNatures
                .Where(x => x.Id == id)
                .Select(x => ToDto(x))
                .FirstOrDefaultAsync();

        public async Task<LegalNatureDto> CreateLegalNatureAsync(UpsertLegalNatureDto dto)
        {
            var entity = new LegalNature
            {
                Code = dto.Code.Trim().ToUpperInvariant(),
                Name = dto.Name.Trim(),
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedDate = DateTime.UtcNow,
            };
            _context.LegalNatures.Add(entity);
            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<LegalNatureDto?> UpdateLegalNatureAsync(int id, UpsertLegalNatureDto dto)
        {
            var entity = await _context.LegalNatures.FindAsync(id);
            if (entity is null) return null;
            entity.Code = dto.Code.Trim().ToUpperInvariant();
            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description;
            entity.IsActive = dto.IsActive;
            entity.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<List<ProductEnvelopeDto>> GetEnvelopesAsync() =>
            await _context.ProductEnvelopes
                .Include(x => x.ProductCategory)
                .Include(x => x.LegalNature)
                .OrderBy(x => x.Name)
                .Select(x => ToDto(x))
                .ToListAsync();

        public async Task<ProductEnvelopeDto?> GetEnvelopeAsync(int id) =>
            await _context.ProductEnvelopes
                .Include(x => x.ProductCategory)
                .Include(x => x.LegalNature)
                .Where(x => x.Id == id)
                .Select(x => ToDto(x))
                .FirstOrDefaultAsync();

        public async Task<ProductEnvelopeDto> CreateEnvelopeAsync(UpsertProductEnvelopeDto dto)
        {
            var entity = new ProductEnvelope();
            Apply(dto, entity, isCreate: true);
            _context.ProductEnvelopes.Add(entity);
            await _context.SaveChangesAsync();
            await _context.Entry(entity).Reference(x => x.ProductCategory).LoadAsync();
            await _context.Entry(entity).Reference(x => x.LegalNature).LoadAsync();
            return ToDto(entity);
        }

        public async Task<ProductEnvelopeDto?> UpdateEnvelopeAsync(int id, UpsertProductEnvelopeDto dto)
        {
            var entity = await _context.ProductEnvelopes
                .Include(x => x.ProductCategory)
                .Include(x => x.LegalNature)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return null;
            Apply(dto, entity, isCreate: false);
            await _context.SaveChangesAsync();
            await _context.Entry(entity).Reference(x => x.ProductCategory).LoadAsync();
            await _context.Entry(entity).Reference(x => x.LegalNature).LoadAsync();
            return ToDto(entity);
        }

        public async Task<List<ProductVersionDto>> GetVersionsByProductAsync(int productId) =>
            await _context.ProductVersions
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.EffectiveFrom)
                .Select(x => ToDto(x))
                .ToListAsync();

        public async Task<ProductVersionDto?> GetVersionAsync(int id) =>
            await _context.ProductVersions
                .Where(x => x.Id == id)
                .Select(x => ToDto(x))
                .FirstOrDefaultAsync();

        public async Task<ProductVersionDto?> CreateVersionAsync(int productId, UpsertProductVersionDto dto)
        {
            var productExists = await _context.Products.AnyAsync(x => x.Id == productId);
            if (!productExists) return null;

            var entity = new ProductVersion { ProductId = productId };
            Apply(dto, entity, isCreate: true);
            _context.ProductVersions.Add(entity);
            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ProductVersionDto?> UpdateVersionAsync(int id, UpsertProductVersionDto dto)
        {
            var entity = await _context.ProductVersions.FindAsync(id);
            if (entity is null) return null;
            if (entity.Status == ProductVersionStatus.Published)
            {
                throw new InvalidOperationException("Une version publiée ne peut pas être modifiée. Créez une nouvelle version.");
            }
            Apply(dto, entity, isCreate: false);
            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ProductVersion?> ResolveApplicableVersionAsync(int productId, DateTime effectiveDate)
        {
            var targetDate = effectiveDate == default ? DateTime.UtcNow.Date : effectiveDate.Date;
            return await _context.ProductVersions
                .Where(v => v.ProductId == productId)
                .Where(v => v.Status == ProductVersionStatus.Published || v.Status == ProductVersionStatus.Validated)
                .Where(v => v.EffectiveFrom.Date <= targetDate)
                .Where(v => !v.EffectiveTo.HasValue || v.EffectiveTo.Value.Date >= targetDate)
                .OrderByDescending(v => v.Status == ProductVersionStatus.Published)
                .ThenByDescending(v => v.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        private static ProductCategoryDto ToDto(ProductCategory x) => new()
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            IsActive = x.IsActive,
        };

        private static LegalNatureDto ToDto(LegalNature x) => new()
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            IsActive = x.IsActive,
        };

        private static ProductEnvelopeDto ToDto(ProductEnvelope x) => new()
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            ProductCategoryId = x.ProductCategoryId,
            ProductCategoryCode = x.ProductCategory?.Code,
            ProductCategoryName = x.ProductCategory?.Name,
            LegalNatureId = x.LegalNatureId,
            LegalNatureCode = x.LegalNature?.Code,
            LegalNatureName = x.LegalNature?.Name,
            DefaultTaxProfileId = x.DefaultTaxProfileId,
            IsIndividual = x.IsIndividual,
            IsCollective = x.IsCollective,
            AllowsMultipleHolders = x.AllowsMultipleHolders,
            RequiresInsuredPerson = x.RequiresInsuredPerson,
            SupportsBeneficiaryClause = x.SupportsBeneficiaryClause,
            IsActive = x.IsActive,
        };

        private static ProductVersionDto ToDto(ProductVersion x) => new()
        {
            Id = x.Id,
            ProductId = x.ProductId,
            VersionCode = x.VersionCode,
            VersionName = x.VersionName,
            EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo,
            Status = x.Status,
            TaxProfileId = x.TaxProfileId,
            CurrencyCode = x.CurrencyCode,
            MinimumInitialPayment = x.MinimumInitialPayment,
            MinimumAdditionalPayment = x.MinimumAdditionalPayment,
            MinimumScheduledPayment = x.MinimumScheduledPayment,
            MinimumPartialWithdrawal = x.MinimumPartialWithdrawal,
            MinimumRemainingBalance = x.MinimumRemainingBalance,
            MinimumSubscriptionAge = x.MinimumSubscriptionAge,
            MaximumSubscriptionAge = x.MaximumSubscriptionAge,
            CreatedDate = x.CreatedDate,
            UpdatedDate = x.UpdatedDate,
        };

        private static void Apply(UpsertProductEnvelopeDto dto, ProductEnvelope entity, bool isCreate)
        {
            entity.Code = dto.Code.Trim().ToUpperInvariant();
            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description;
            entity.ProductCategoryId = dto.ProductCategoryId;
            entity.LegalNatureId = dto.LegalNatureId;
            entity.DefaultTaxProfileId = dto.DefaultTaxProfileId;
            entity.IsIndividual = dto.IsIndividual;
            entity.IsCollective = dto.IsCollective;
            entity.AllowsMultipleHolders = dto.AllowsMultipleHolders;
            entity.RequiresInsuredPerson = dto.RequiresInsuredPerson;
            entity.SupportsBeneficiaryClause = dto.SupportsBeneficiaryClause;
            entity.IsActive = dto.IsActive;
            if (isCreate) entity.CreatedDate = DateTime.UtcNow;
            else entity.UpdatedDate = DateTime.UtcNow;
        }

        private static void Apply(UpsertProductVersionDto dto, ProductVersion entity, bool isCreate)
        {
            entity.VersionCode = dto.VersionCode.Trim().ToUpperInvariant();
            entity.VersionName = dto.VersionName;
            entity.EffectiveFrom = dto.EffectiveFrom.Date;
            entity.EffectiveTo = dto.EffectiveTo?.Date;
            entity.Status = dto.Status;
            entity.TaxProfileId = dto.TaxProfileId;
            entity.CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? "EUR" : dto.CurrencyCode.Trim().ToUpperInvariant();
            entity.MinimumInitialPayment = dto.MinimumInitialPayment;
            entity.MinimumAdditionalPayment = dto.MinimumAdditionalPayment;
            entity.MinimumScheduledPayment = dto.MinimumScheduledPayment;
            entity.MinimumPartialWithdrawal = dto.MinimumPartialWithdrawal;
            entity.MinimumRemainingBalance = dto.MinimumRemainingBalance;
            entity.MinimumSubscriptionAge = dto.MinimumSubscriptionAge;
            entity.MaximumSubscriptionAge = dto.MaximumSubscriptionAge;
            if (isCreate) entity.CreatedDate = DateTime.UtcNow;
            else entity.UpdatedDate = DateTime.UtcNow;
        }
    }
}
