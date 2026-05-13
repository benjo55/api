using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models.Enum;

namespace api.Dtos.Product
{
    public class CreateProductRequestDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int InsurerId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public ContractFamily? ContractFamily { get; set; }
        public decimal? DefaultManagementFeeRate { get; set; }
        public ManagementFeeFrequency? DefaultManagementFeeFrequency { get; set; }
        public ManagementFeeProrataMethod? DefaultManagementFeeProrataMethod { get; set; }
        public ManagementFeePostingMode? DefaultManagementFeePostingMode { get; set; }
        public DateTime? DefaultManagementFeeEffectiveDate { get; set; }
        public DateTime? DefaultManagementFeeEndDate { get; set; }
        public bool? DefaultManagementFeeIsEnabled { get; set; }
    }
}