using System;

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

        // 🔹 Détail du support
        public FinancialSupportDto? Support { get; set; }
    }
}
