using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Generic;
using api.Dtos.Product;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Services;
using Mapster;
using Microsoft.AspNetCore.Http.HttpResults;
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
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return null;

            HydrateManagementFeeSettings(product);
            return product;
        }

        public async Task<Product?> UpdateAsync(int id, UpdateProductRequestDto updateProductDto)
        {
            var existingProduct = await _context.Products.FindAsync(id);
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
    }
}
