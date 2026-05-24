using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Generic;
using api.Dtos.Product;
using api.Dtos.TaxProfile;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using api.Services;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly EntityHistoryService _entityHistoryService;  // Service d'historisation
        public ProductRepository(ApplicationDBContext context, EntityHistoryService entityHistoryService)
        {
            _context = context;
            _entityHistoryService = entityHistoryService;
        }

        public async Task<Product> CreateAsync(Product productModel)
        {
            SyncManagementFeePolicy(productModel, null, _context);
            await _context.Products.AddAsync(productModel);
            await _context.SaveChangesAsync();
            HydrateManagementFeeSettings(productModel);
            return productModel;
        }

        public async Task<Product?> DeleteAsync(int id)
        {
            var productModel = await _context.Products.FirstOrDefaultAsync(c => c.Id == id);
            if (productModel == null) return null;
            _context.Products.Remove(productModel);
            await _context.SaveChangesAsync();
            return productModel;
        }

        public async Task<PagedResult<Product>> GetAllAsync(QueryObject query)
        {
            // Filter products based on search criteria
            var products = _context.Products
                .Include(p => p.ManagementFeePolicy)
                .Include(p => p.ProductType)
                .Include(p => p.TaxProfile)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                products = products.Where(p =>
                    p.ProductCode.Contains(query.Search) || p.ProductName.Contains(query.Search));
            }

            products = products.OrderByDescending(p => p.CreatedDate);

            // Calculate total count before pagination
            var totalCount = await products.CountAsync();

            // Apply pagination
            var pagedProducts = await products
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            // Enrich each product with the contract count
            foreach (var product in pagedProducts)
            {
                product.ContractCount = await _context.Contracts
                    .Where(c => c.ProductId == product.Id)
                    .CountAsync();

                HydrateManagementFeeSettings(product);
            }

            // Calculate total pages
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            // Return paged results
            return new PagedResult<Product>
            {
                Items = pagedProducts,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = query.PageNumber < totalPages,
                CurrentPage = query.PageNumber
            };
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.ManagementFeePolicy)
                .Include(p => p.ProductType)
                .Include(p => p.TaxProfile)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return null;

            HydrateManagementFeeSettings(product);
            return product;
        }

        public async Task<Product?> UpdateAsync(int id, UpdateProductRequestDto updateProductDto)
        {
            var existingProduct = await _context.Products
                .Include(p => p.ManagementFeePolicy)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (existingProduct == null) return null;

            // 1️⃣ Cloner l'état initial pour l'historisation
            var originalProduct = (Product)_context.Entry(existingProduct).CurrentValues.ToObject();

            // 2️⃣ Mise à jour avec Mapster
            updateProductDto.Adapt(existingProduct);
            existingProduct.UpdatedDate = DateTime.UtcNow;
            SyncManagementFeePolicy(existingProduct, updateProductDto, _context);

            // 3️⃣ Historisation des changements
            await _entityHistoryService.TrackChangesAsync(originalProduct, existingProduct, "Admin"); // Remplace "Admin" par l'utilisateur courant

            // 4️⃣ Sauvegarde
            await _context.SaveChangesAsync();
            HydrateManagementFeeSettings(existingProduct);
            return existingProduct;
        }

        public async Task<ProductTaxViewDto?> GetTaxViewByProductIdAsync(int productId, DateTime? asOfDate = null)
        {
            var targetDate = (asOfDate ?? DateTime.UtcNow).Date;

            var product = await _context.Products
                .Include(p => p.ProductType)
                .Include(p => p.TaxProfile)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return null;
            }

            var (taxProfile, source) = await ResolveTaxProfileWithSourceAsync(product);

            var overrides = await _context.ProductTaxOverrides
                .Where(o => o.ProductId == productId)
                .Where(o => !o.ValidTo.HasValue || o.ValidTo.Value.Date >= targetDate)
                .Where(o => o.ValidFrom.Date <= targetDate)
                .OrderByDescending(o => o.ValidFrom)
                .ToListAsync();

            var applicableGenerations = await _context.TaxGenerations
                .Where(g => product.ContractFamily.HasValue && g.ProductType == product.ContractFamily.Value)
                .Where(g => g.EffectiveDateStart.Date <= targetDate)
                .Where(g => !g.EffectiveDateEnd.HasValue || g.EffectiveDateEnd.Value.Date >= targetDate)
                .OrderByDescending(g => g.EffectiveDateStart)
                .ToListAsync();

            return new ProductTaxViewDto
            {
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                ContractFamily = product.ContractFamily,
                ProductTypeId = product.ProductTypeId,
                ProductTypeCode = product.ProductType?.Code,
                TaxProfileId = taxProfile?.Id,
                TaxProfileSource = source,
                BaseProfile = taxProfile is null ? null : ToTaxProfileDto(taxProfile),
                Overrides = overrides.Select(ToProductTaxOverrideDto).ToList(),
                ApplicableGenerations = applicableGenerations.Select(g => new ProductTaxGenerationDto
                {
                    Id = g.Id,
                    Code = g.Code,
                    Label = g.Label,
                    TaxRuleType = g.TaxRuleType.ToString(),
                    TaxCompartmentType = g.TaxCompartmentType.ToString(),
                    EffectiveDateStart = g.EffectiveDateStart,
                    EffectiveDateEnd = g.EffectiveDateEnd,
                }).ToList(),
            };
        }

        public async Task<List<ProductFeatureDto>> GetFeaturesByProductIdAsync(int productId, DateTime? asOfDate = null)
        {
            var targetDate = (asOfDate ?? DateTime.UtcNow).Date;

            return await _context.ProductFeatures
                .Where(f => f.ProductId == productId)
                .Where(f => !f.ValidTo.HasValue || f.ValidTo.Value.Date >= targetDate)
                .Where(f => !f.ValidFrom.HasValue || f.ValidFrom.Value.Date <= targetDate)
                .OrderBy(f => f.FeatureKey)
                .ThenByDescending(f => f.ValidFrom)
                .Select(f => new ProductFeatureDto
                {
                    Id = f.Id,
                    ProductId = f.ProductId,
                    FeatureKey = f.FeatureKey,
                    FeatureValue = f.FeatureValue,
                    ValueType = f.ValueType,
                    ValidFrom = f.ValidFrom,
                    ValidTo = f.ValidTo,
                })
                .ToListAsync();
        }

        public async Task<ProductFeatureDto?> AddFeatureAsync(int productId, CreateProductFeatureDto dto)
        {
            var exists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!exists)
            {
                return null;
            }

            var feature = new ProductFeature
            {
                ProductId = productId,
                FeatureKey = dto.FeatureKey.Trim(),
                FeatureValue = dto.FeatureValue,
                ValueType = string.IsNullOrWhiteSpace(dto.ValueType) ? "TEXT" : dto.ValueType.Trim().ToUpperInvariant(),
                ValidFrom = dto.ValidFrom?.Date,
                ValidTo = dto.ValidTo?.Date,
                CreatedDate = DateTime.UtcNow,
            };

            await _context.ProductFeatures.AddAsync(feature);
            await _context.SaveChangesAsync();

            return new ProductFeatureDto
            {
                Id = feature.Id,
                ProductId = feature.ProductId,
                FeatureKey = feature.FeatureKey,
                FeatureValue = feature.FeatureValue,
                ValueType = feature.ValueType,
                ValidFrom = feature.ValidFrom,
                ValidTo = feature.ValidTo,
            };
        }

        public async Task<ProductFeatureDto?> UpdateFeatureAsync(int productId, int featureId, UpdateProductFeatureDto dto)
        {
            var feature = await _context.ProductFeatures
                .FirstOrDefaultAsync(f => f.Id == featureId && f.ProductId == productId);

            if (feature == null)
            {
                return null;
            }

            feature.FeatureKey = dto.FeatureKey.Trim();
            feature.FeatureValue = dto.FeatureValue;
            feature.ValueType = string.IsNullOrWhiteSpace(dto.ValueType) ? "TEXT" : dto.ValueType.Trim().ToUpperInvariant();
            feature.ValidFrom = dto.ValidFrom?.Date;
            feature.ValidTo = dto.ValidTo?.Date;
            feature.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ProductFeatureDto
            {
                Id = feature.Id,
                ProductId = feature.ProductId,
                FeatureKey = feature.FeatureKey,
                FeatureValue = feature.FeatureValue,
                ValueType = feature.ValueType,
                ValidFrom = feature.ValidFrom,
                ValidTo = feature.ValidTo,
            };
        }

        public async Task<List<ProductTaxOverrideDto>> GetTaxOverridesByProductIdAsync(int productId, DateTime? asOfDate = null)
        {
            var targetDate = (asOfDate ?? DateTime.UtcNow).Date;

            var overrides = await _context.ProductTaxOverrides
                .Where(o => o.ProductId == productId)
                .Where(o => !o.ValidTo.HasValue || o.ValidTo.Value.Date >= targetDate)
                .Where(o => o.ValidFrom.Date <= targetDate)
                .OrderBy(o => o.ParameterKey)
                .ThenByDescending(o => o.ValidFrom)
                .ToListAsync();

            return overrides.Select(ToProductTaxOverrideDto).ToList();
        }

        public async Task<ProductTaxOverrideDto?> AddTaxOverrideAsync(int productId, CreateProductTaxOverrideDto dto)
        {
            var exists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!exists)
            {
                return null;
            }

            var taxOverride = new ProductTaxOverride
            {
                ProductId = productId,
                ParameterKey = dto.ParameterKey.Trim(),
                NumericValue = dto.NumericValue,
                JsonValue = dto.JsonValue,
                Justification = dto.Justification,
                ValidFrom = dto.ValidFrom.Date,
                ValidTo = dto.ValidTo?.Date,
                CreatedDate = DateTime.UtcNow,
            };

            await _context.ProductTaxOverrides.AddAsync(taxOverride);
            await _context.SaveChangesAsync();

            return ToProductTaxOverrideDto(taxOverride);
        }

        public async Task<ProductTaxOverrideDto?> UpdateTaxOverrideAsync(int productId, int taxOverrideId, UpdateProductTaxOverrideDto dto)
        {
            var taxOverride = await _context.ProductTaxOverrides
                .FirstOrDefaultAsync(o => o.Id == taxOverrideId && o.ProductId == productId);

            if (taxOverride == null)
            {
                return null;
            }

            taxOverride.ParameterKey = dto.ParameterKey.Trim();
            taxOverride.NumericValue = dto.NumericValue;
            taxOverride.JsonValue = dto.JsonValue;
            taxOverride.Justification = dto.Justification;
            taxOverride.ValidFrom = dto.ValidFrom.Date;
            taxOverride.ValidTo = dto.ValidTo?.Date;
            taxOverride.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ToProductTaxOverrideDto(taxOverride);
        }

        public async Task<List<ProductTypeDto>> GetProductTypesAsync()
        {
            return await _context.ProductTypes
                .OrderBy(t => t.Label)
                .Select(t => new ProductTypeDto
                {
                    Id = t.Id,
                    Code = t.Code,
                    Label = t.Label,
                    Category = t.Category,
                    DefaultTaxProfileId = t.DefaultTaxProfileId,
                    IsActive = t.IsActive,
                })
                .ToListAsync();
        }

        public async Task<int> CountContractsByProductIdAsync(int productId)
        {
            return await _context.Contracts
                                 .Where(c => c.ProductId == productId)
                                 .CountAsync();
        }

        public async Task<Product?> PatchLockedAsync(int id, bool locked)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return null;

            var original = (Product)_context.Entry(product).CurrentValues.ToObject();

            product.Locked = locked;
            product.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Historisation
            await _entityHistoryService.TrackChangesAsync(original, product, "Admin");

            return product;
        }

        private static void HydrateManagementFeeSettings(Product product)
        {
            var policy = product.ManagementFeePolicy;
            product.DefaultManagementFeeRate = policy?.AnnualRate;
            product.DefaultManagementFeeFrequency = policy?.Frequency.ToString();
            product.DefaultManagementFeeProrataMethod = policy?.ProrataMethod.ToString();
            product.DefaultManagementFeePostingMode = policy?.PostingMode.ToString();
            product.DefaultManagementFeeEffectiveDate = policy?.EffectiveDate;
            product.DefaultManagementFeeEndDate = policy?.EndDate;
            product.DefaultManagementFeeIsEnabled = policy?.IsEnabled;
        }

        private static void SyncManagementFeePolicy(
            Product product,
            UpdateProductRequestDto? dto,
            ApplicationDBContext context)
        {
            var rate = dto?.DefaultManagementFeeRate ?? product.DefaultManagementFeeRate;
            var frequency = dto?.DefaultManagementFeeFrequency;
            var prorataMethod = dto?.DefaultManagementFeeProrataMethod;
            var postingMode = dto?.DefaultManagementFeePostingMode;
            var effectiveDate = dto?.DefaultManagementFeeEffectiveDate ?? product.DefaultManagementFeeEffectiveDate;
            var endDate = dto?.DefaultManagementFeeEndDate ?? product.DefaultManagementFeeEndDate;
            var isEnabled = dto?.DefaultManagementFeeIsEnabled ?? product.DefaultManagementFeeIsEnabled ?? true;

            if (rate == null || rate <= 0m)
            {
                if (product.ManagementFeePolicy != null)
                {
                    context.ProductManagementFeePolicies.Remove(product.ManagementFeePolicy);
                }

                product.ManagementFeePolicy = null;
                return;
            }

            var policy = product.ManagementFeePolicy ?? new ProductManagementFeePolicy();
            policy.AnnualRate = rate.Value;
            policy.Frequency = frequency ?? api.Models.Enum.ManagementFeeFrequency.Monthly;
            policy.ProrataMethod = prorataMethod ?? api.Models.Enum.ManagementFeeProrataMethod.Periodic;
            policy.PostingMode = postingMode ?? api.Models.Enum.ManagementFeePostingMode.UnitCancellation;
            policy.EffectiveDate = (effectiveDate ?? DateTime.UtcNow).Date;
            policy.EndDate = endDate?.Date;
            policy.IsEnabled = isEnabled;
            policy.UpdatedDate = DateTime.UtcNow;
            policy.CreatedDate = policy.CreatedDate == default ? DateTime.UtcNow : policy.CreatedDate;

            product.ManagementFeePolicy = policy;
        }

        private async Task<(TaxProfile? Profile, string Source)> ResolveTaxProfileWithSourceAsync(Product product)
        {
            if (product.TaxProfileId.HasValue)
            {
                var directProfile = await _context.TaxProfiles
                    .FirstOrDefaultAsync(p => p.Id == product.TaxProfileId.Value);

                if (directProfile != null)
                {
                    return (directProfile, "Product");
                }
            }

            if (product.ProductTypeId.HasValue)
            {
                var typeWithProfile = await _context.ProductTypes
                    .Include(t => t.DefaultTaxProfile)
                    .FirstOrDefaultAsync(t => t.Id == product.ProductTypeId.Value);

                if (typeWithProfile?.DefaultTaxProfile != null)
                {
                    return (typeWithProfile.DefaultTaxProfile, "ProductType");
                }
            }

            if (product.ContractFamily.HasValue)
            {
                var familyProfile = await _context.TaxProfiles
                    .Where(tp => tp.ContractFamily == product.ContractFamily.Value)
                    .OrderByDescending(tp => tp.Locked)
                    .ThenBy(tp => tp.Id)
                    .FirstOrDefaultAsync();

                if (familyProfile != null)
                {
                    return (familyProfile, "ContractFamily");
                }
            }

            return (null, "None");
        }

        private static ProductTaxOverrideDto ToProductTaxOverrideDto(ProductTaxOverride item)
        {
            return new ProductTaxOverrideDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ParameterKey = item.ParameterKey,
                NumericValue = item.NumericValue,
                JsonValue = item.JsonValue,
                Justification = item.Justification,
                ValidFrom = item.ValidFrom,
                ValidTo = item.ValidTo,
            };
        }

        private static TaxProfileDto ToTaxProfileDto(TaxProfile profile)
        {
            return new TaxProfileDto
            {
                Id = profile.Id,
                ContractFamily = profile.ContractFamily,
                ContractFamilyLabel = profile.ContractFamily.ToLabel(),
                Label = profile.Label,
                Description = profile.Description,
                EntryDeductible = profile.EntryDeductible,
                EntryDeductionCap = profile.EntryDeductionCap,
                DurationThresholdYears = profile.DurationThresholdYears,
                IrRateBeforeThreshold = profile.IrRateBeforeThreshold,
                IrRateAfterThreshold = profile.IrRateAfterThreshold,
                ContributionCapForReducedRate = profile.ContributionCapForReducedRate,
                IrRateAboveContributionCap = profile.IrRateAboveContributionCap,
                SocialChargesRate = profile.SocialChargesRate,
                GainAllowanceSingle = profile.GainAllowanceSingle,
                GainAllowanceCouple = profile.GainAllowanceCouple,
                IrExemptAfterThreshold = profile.IrExemptAfterThreshold,
                SocialChargesExemptAfterThreshold = profile.SocialChargesExemptAfterThreshold,
                HasDeathTaxArticle990I = profile.HasDeathTaxArticle990I,
                Death990I_AllowancePerBeneficiary = profile.Death990I_AllowancePerBeneficiary,
                Death990I_Rate1 = profile.Death990I_Rate1,
                Death990I_Rate1Threshold = profile.Death990I_Rate1Threshold,
                Death990I_Rate2 = profile.Death990I_Rate2,
                HasDeathTaxArticle757B = profile.HasDeathTaxArticle757B,
                Death757B_GlobalAllowance = profile.Death757B_GlobalAllowance,
                ExitMode = profile.ExitMode,
                RenteTaxedAsPension = profile.RenteTaxedAsPension,
                RentePartImposable = profile.RentePartImposable,
                CanChooseBareme = profile.CanChooseBareme,
                HasSuccessionBenefit = profile.HasSuccessionBenefit,
                Locked = profile.Locked,
                CreatedDate = profile.CreatedDate,
                UpdatedDate = profile.UpdatedDate,
            };
        }
    }
}
