using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public class ProductVersion
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Required, MaxLength(50)]
        public string VersionCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? VersionName { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public ProductVersionStatus Status { get; set; } = ProductVersionStatus.Draft;

        public int? TaxProfileId { get; set; }
        [ForeignKey(nameof(TaxProfileId))]
        public TaxProfile? TaxProfile { get; set; }

        [Required, MaxLength(3)]
        public string CurrencyCode { get; set; } = "EUR";

        [Precision(20, 7)]
        public decimal? MinimumInitialPayment { get; set; }

        [Precision(20, 7)]
        public decimal? MinimumAdditionalPayment { get; set; }

        [Precision(20, 7)]
        public decimal? MinimumScheduledPayment { get; set; }

        [Precision(20, 7)]
        public decimal? MinimumPartialWithdrawal { get; set; }

        [Precision(20, 7)]
        public decimal? MinimumRemainingBalance { get; set; }

        public int? MinimumSubscriptionAge { get; set; }
        public int? MaximumSubscriptionAge { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public List<Contract> Contracts { get; set; } = [];
        public List<ProductEligibilityRule> EligibilityRules { get; set; } = [];
        public List<ProductOperationRule> OperationRules { get; set; } = [];
        public List<ProductPaymentRule> PaymentRules { get; set; } = [];
        public List<ProductFeeRule> FeeRules { get; set; } = [];
        public List<ProductGuarantee> Guarantees { get; set; } = [];
        public List<ProductManagementMode> ManagementModes { get; set; } = [];
        public List<ProductFinancialSupport> FinancialSupports { get; set; } = [];
        public List<ProductDocument> Documents { get; set; } = [];
    }
}
