using api.Dtos.TaxProfile;
using api.Models.Enum;

namespace api.Dtos.Product
{
    public class ProductTypeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int? DefaultTaxProfileId { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductFeatureDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string FeatureKey { get; set; } = string.Empty;
        public string? FeatureValue { get; set; }
        public string ValueType { get; set; } = string.Empty;
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }

    public class CreateProductFeatureDto
    {
        public string FeatureKey { get; set; } = string.Empty;
        public string? FeatureValue { get; set; }
        public string ValueType { get; set; } = "TEXT";
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }

    public class UpdateProductFeatureDto : CreateProductFeatureDto { }

    public class ProductTaxOverrideDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ParameterKey { get; set; } = string.Empty;
        public decimal? NumericValue { get; set; }
        public string? JsonValue { get; set; }
        public string? Justification { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }

    public class CreateProductTaxOverrideDto
    {
        public string ParameterKey { get; set; } = string.Empty;
        public decimal? NumericValue { get; set; }
        public string? JsonValue { get; set; }
        public string? Justification { get; set; }
        public DateTime ValidFrom { get; set; } = DateTime.UtcNow.Date;
        public DateTime? ValidTo { get; set; }
    }

    public class UpdateProductTaxOverrideDto : CreateProductTaxOverrideDto { }

    public class ProductTaxGenerationDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string TaxRuleType { get; set; } = string.Empty;
        public string TaxCompartmentType { get; set; } = string.Empty;
        public DateTime EffectiveDateStart { get; set; }
        public DateTime? EffectiveDateEnd { get; set; }
    }

    public class ProductTaxViewDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public ContractFamily? ContractFamily { get; set; }
        public int? ProductTypeId { get; set; }
        public string? ProductTypeCode { get; set; }
        public int? TaxProfileId { get; set; }
        public string TaxProfileSource { get; set; } = "None";
        public TaxProfileDto? BaseProfile { get; set; }
        public List<ProductTaxOverrideDto> Overrides { get; set; } = [];
        public List<ProductTaxGenerationDto> ApplicableGenerations { get; set; } = [];
    }
}
