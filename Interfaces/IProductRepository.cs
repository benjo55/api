using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Generic;
using api.Dtos.Product;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IProductRepository
    {
        Task<PagedResult<Product>> GetAllAsync(QueryObject query);
        Task<Product?> GetByIdAsync(int id);
        Task<Product> CreateAsync(Product productModel);
        Task<Product?> UpdateAsync(int id, UpdateProductRequestDto productDto);
        Task<Product?> DeleteAsync(int id);
        Task<int> CountContractsByProductIdAsync(int productId);
        Task<Product?> PatchLockedAsync(int id, bool locked);
        Task<ProductTaxViewDto?> GetTaxViewByProductIdAsync(int productId, DateTime? asOfDate = null);
        Task<List<ProductFeatureDto>> GetFeaturesByProductIdAsync(int productId, DateTime? asOfDate = null);
        Task<ProductFeatureDto?> AddFeatureAsync(int productId, CreateProductFeatureDto dto);
        Task<ProductFeatureDto?> UpdateFeatureAsync(int productId, int featureId, UpdateProductFeatureDto dto);
        Task<List<ProductTaxOverrideDto>> GetTaxOverridesByProductIdAsync(int productId, DateTime? asOfDate = null);
        Task<ProductTaxOverrideDto?> AddTaxOverrideAsync(int productId, CreateProductTaxOverrideDto dto);
        Task<ProductTaxOverrideDto?> UpdateTaxOverrideAsync(int productId, int taxOverrideId, UpdateProductTaxOverrideDto dto);
        Task<List<ProductTypeDto>> GetProductTypesAsync();
    }

    public class ProductWithContractCount
    {
        public required Product Product { get; set; }
        public int ContractCount { get; set; }
    }
}
