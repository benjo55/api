using System;
using api.Models.Enum;

namespace api.Dtos.FinancialSupport
{
    public class FinancialSupportAllocationDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public int SupportId { get; set; }
        public int? CompartmentId { get; set; }

        public decimal? AllocationPercentage { get; set; }

        // 🔹 Données de valorisation (FSA + Holdings)
        public decimal CurrentShares { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal InvestedAmount { get; set; }

        // 🔹 PRU consolidé (ContractSupportHoldings)
        public decimal? Pru { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public decimal? ResolvedManagementFeeRate { get; set; }
        public ManagementFeeFrequency? ResolvedManagementFeeFrequency { get; set; }
        public ManagementFeeProrataMethod? ResolvedManagementFeeProrataMethod { get; set; }
        public ManagementFeePostingMode? ResolvedManagementFeePostingMode { get; set; }
        public DateTime? ResolvedManagementFeeEffectiveDate { get; set; }
        public DateTime? ResolvedManagementFeeEndDate { get; set; }
        public string? ResolvedManagementFeeSource { get; set; }

        // 🔹 Détail du support
        public FinancialSupportDto? Support { get; set; }
    }
}
