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
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly EntityHistoryService _entityHistoryService;  // Service d'historisation
        private readonly IMapper _mapper; // AutoMapper pour éviter d'affecter manuellement les champs

        public ProductRepository(ApplicationDBContext context, EntityHistoryService entityHistoryService, IMapper mapper)
        {
            _context = context;
            _entityHistoryService = entityHistoryService;
            _mapper = mapper;
        }

        public async Task<Product> CreateAsync(Product productModel)
        {
            await _context.Products.AddAsync(productModel);
            await _context.SaveChangesAsync();
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
            var products = _context.Products.AsQueryable();
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
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product?> UpdateAsync(int id, UpdateProductRequestDto updateProductDto)
        {
            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null) return null;

            // 1️⃣ Cloner l'état initial pour l'historisation
            var originalProduct = new Product();
            _mapper.Map(existingProduct, originalProduct);

            // 2️⃣ Mise à jour automatique avec AutoMapper
            _mapper.Map(updateProductDto, existingProduct);
            existingProduct.UpdatedDate = DateTime.UtcNow;

            // 3️⃣ Historisation des changements
            await _entityHistoryService.TrackChangesAsync(originalProduct, existingProduct, "Admin"); // Remplace "Admin" par l'utilisateur courant

            // 4️⃣ Sauvegarde
            await _context.SaveChangesAsync();
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

            var original = new Product();
            _mapper.Map(product, original);

            product.Locked = locked;
            product.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Historisation
            await _entityHistoryService.TrackChangesAsync(original, product, "Admin");

            return product;
        }
    }
}
