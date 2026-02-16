using System;
using System.Collections.Generic;
using api.Dtos.Compartment;
using api.Dtos.Document;
using api.Dtos.FinancialSupport;

namespace api.Dtos.Contract
{
    public class UpdateContractRequestDto
    {
        public int Id { get; set; }  // Id du contrat à mettre à jour

        public string ContractNumber { get; set; } = string.Empty;
        public string ContractLabel { get; set; } = string.Empty;
        public string ContractType { get; set; } = "Assurance Vie";
        public string Status { get; set; } = "Actif";
        public bool Locked { get; set; }

        public DateTime DateSign { get; set; }
        public DateTime DateEffect { get; set; }
        public DateTime? DateMaturity { get; set; }

        public string PostalAddress { get; set; } = string.Empty;
        public string TaxAddress { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";

        public int? PersonId { get; set; }

        public decimal InitialPremium { get; set; }
        public decimal TotalPaidPremiums { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal? RedemptionValue { get; set; }

        public string PaymentMode { get; set; } = string.Empty;
        public decimal? ScheduledPayment { get; set; }

        public int? BeneficiaryClauseId { get; set; }

        public decimal? EntryFeesRate { get; set; }
        public decimal? ManagementFeesRate { get; set; }
        public decimal? ExitFeesRate { get; set; }

        public string? AdvisorComment { get; set; }
        public bool HasAlert { get; set; }

        public string? ExternalReference { get; set; }

        public int? LastModifiedByUserId { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Assurés
        public List<int> InsuredPersonIds { get; set; } = new();

        // Options
        public List<ContractOptionDto> Options { get; set; } = new();

        // Supports au niveau contrat
        public List<UpdateFinancialSupportAllocationDto> Supports { get; set; } = new();

        // Compartiments
        public List<UpdateCompartmentRequestDto> Compartments { get; set; } = new();

        // Documents
        public List<DocumentDto> Documents { get; set; } = new();
    }
}
