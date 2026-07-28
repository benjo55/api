using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models.Enum;

namespace api.Dtos.Product
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? CommercialName { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int? InsurerId { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int ContractCount { get; set; }
        public bool Locked { get; set; }
        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public bool IsOpenToNewBusiness { get; set; } = true;
        public bool IsOpenToNewPayments { get; set; } = true;
        public DateTime? MarketingStartDate { get; set; }
        public DateTime? MarketingEndDate { get; set; }
        public int? ProductEnvelopeId { get; set; }
        public string? ProductEnvelopeCode { get; set; }
        public string? ProductEnvelopeName { get; set; }
        public ContractFamily? ContractFamily { get; set; }
        public string? ContractFamilyLabel { get; set; }
        public int? ProductTypeId { get; set; }
        public int? TaxProfileId { get; set; }
        public decimal? DefaultManagementFeeRate { get; set; }
        public ManagementFeeFrequency? DefaultManagementFeeFrequency { get; set; }
        public ManagementFeeProrataMethod? DefaultManagementFeeProrataMethod { get; set; }
        public ManagementFeePostingMode? DefaultManagementFeePostingMode { get; set; }
        public DateTime? DefaultManagementFeeEffectiveDate { get; set; }
        public DateTime? DefaultManagementFeeEndDate { get; set; }
        public bool? DefaultManagementFeeIsEnabled { get; set; }
    }
}
