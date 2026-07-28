using api.Models.Enum;

namespace api.Dtos.Product
{
    public class ProductCategoryDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpsertProductCategoryDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class LegalNatureDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpsertLegalNatureDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ProductEnvelopeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCategoryId { get; set; }
        public string? ProductCategoryCode { get; set; }
        public string? ProductCategoryName { get; set; }
        public int LegalNatureId { get; set; }
        public string? LegalNatureCode { get; set; }
        public string? LegalNatureName { get; set; }
        public int? DefaultTaxProfileId { get; set; }
        public bool IsIndividual { get; set; }
        public bool IsCollective { get; set; }
        public bool AllowsMultipleHolders { get; set; }
        public bool RequiresInsuredPerson { get; set; }
        public bool SupportsBeneficiaryClause { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpsertProductEnvelopeDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCategoryId { get; set; }
        public int LegalNatureId { get; set; }
        public int? DefaultTaxProfileId { get; set; }
        public bool IsIndividual { get; set; } = true;
        public bool IsCollective { get; set; }
        public bool AllowsMultipleHolders { get; set; }
        public bool RequiresInsuredPerson { get; set; }
        public bool SupportsBeneficiaryClause { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class ProductVersionDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string VersionCode { get; set; } = string.Empty;
        public string? VersionName { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public ProductVersionStatus Status { get; set; }
        public int? TaxProfileId { get; set; }
        public string CurrencyCode { get; set; } = "EUR";
        public decimal? MinimumInitialPayment { get; set; }
        public decimal? MinimumAdditionalPayment { get; set; }
        public decimal? MinimumScheduledPayment { get; set; }
        public decimal? MinimumPartialWithdrawal { get; set; }
        public decimal? MinimumRemainingBalance { get; set; }
        public int? MinimumSubscriptionAge { get; set; }
        public int? MaximumSubscriptionAge { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class UpsertProductVersionDto
    {
        public string VersionCode { get; set; } = string.Empty;
        public string? VersionName { get; set; }
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;
        public DateTime? EffectiveTo { get; set; }
        public ProductVersionStatus Status { get; set; } = ProductVersionStatus.Draft;
        public int? TaxProfileId { get; set; }
        public string CurrencyCode { get; set; } = "EUR";
        public decimal? MinimumInitialPayment { get; set; }
        public decimal? MinimumAdditionalPayment { get; set; }
        public decimal? MinimumScheduledPayment { get; set; }
        public decimal? MinimumPartialWithdrawal { get; set; }
        public decimal? MinimumRemainingBalance { get; set; }
        public int? MinimumSubscriptionAge { get; set; }
        public int? MaximumSubscriptionAge { get; set; }
    }
}
