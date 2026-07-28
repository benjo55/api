using api.Dtos.Product;
using api.Models;

namespace api.Interfaces
{
    public interface IProductCatalogRepository
    {
        Task<List<ProductCategoryDto>> GetCategoriesAsync();
        Task<ProductCategoryDto?> GetCategoryAsync(int id);
        Task<ProductCategoryDto> CreateCategoryAsync(UpsertProductCategoryDto dto);
        Task<ProductCategoryDto?> UpdateCategoryAsync(int id, UpsertProductCategoryDto dto);

        Task<List<LegalNatureDto>> GetLegalNaturesAsync();
        Task<LegalNatureDto?> GetLegalNatureAsync(int id);
        Task<LegalNatureDto> CreateLegalNatureAsync(UpsertLegalNatureDto dto);
        Task<LegalNatureDto?> UpdateLegalNatureAsync(int id, UpsertLegalNatureDto dto);

        Task<List<ProductEnvelopeDto>> GetEnvelopesAsync();
        Task<ProductEnvelopeDto?> GetEnvelopeAsync(int id);
        Task<ProductEnvelopeDto> CreateEnvelopeAsync(UpsertProductEnvelopeDto dto);
        Task<ProductEnvelopeDto?> UpdateEnvelopeAsync(int id, UpsertProductEnvelopeDto dto);

        Task<List<ProductVersionDto>> GetVersionsByProductAsync(int productId);
        Task<ProductVersionDto?> GetVersionAsync(int id);
        Task<ProductVersionDto?> CreateVersionAsync(int productId, UpsertProductVersionDto dto);
        Task<ProductVersionDto?> UpdateVersionAsync(int id, UpsertProductVersionDto dto);
        Task<ProductVersion?> ResolveApplicableVersionAsync(int productId, DateTime effectiveDate);
    }
}
