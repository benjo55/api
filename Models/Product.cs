using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using api.Models.Enum;

namespace api.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public int ContractCount { get; set; }
        public int? InsurerId { get; set; }
        [ForeignKey("InsurerId")]
        public Insurer? Insurer { get; set; }
        public bool Locked { get; set; } = false;

        /// <summary>Famille de contrats rattachée à ce produit</summary>
        public ContractFamily? ContractFamily { get; set; }

        public int? ProductTypeId { get; set; }
        [ForeignKey("ProductTypeId")]
        public ProductType? ProductType { get; set; }

        public int? TaxProfileId { get; set; }
        [ForeignKey("TaxProfileId")]
        public TaxProfile? TaxProfile { get; set; }

        public ProductManagementFeePolicy? ManagementFeePolicy { get; set; }
        public ICollection<FeePolicy> FeePolicies { get; set; } = new List<FeePolicy>();

        public List<ProductFeature> Features { get; set; } = [];
        public List<ProductTaxOverride> TaxOverrides { get; set; } = [];

        [NotMapped]
        public decimal? DefaultManagementFeeRate { get; set; }

        [NotMapped]
        public string? DefaultManagementFeeFrequency { get; set; }

        [NotMapped]
        public string? DefaultManagementFeeProrataMethod { get; set; }

        [NotMapped]
        public string? DefaultManagementFeePostingMode { get; set; }

        [NotMapped]
        public DateTime? DefaultManagementFeeEffectiveDate { get; set; }

        [NotMapped]
        public DateTime? DefaultManagementFeeEndDate { get; set; }

        [NotMapped]
        public bool? DefaultManagementFeeIsEnabled { get; set; }
    }
}