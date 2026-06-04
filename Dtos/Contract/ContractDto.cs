using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.BeneficiaryClause;
using api.Dtos.Compartment;
using api.Dtos.Document;
using api.Dtos.FinancialSupport;
using api.Dtos.Operation;
using api.Dtos.Person;
using api.Models;
using api.Models.Enum;

namespace api.Dtos.Contract
{
    public class ContractDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractLabel { get; set; } = string.Empty;
        public string ContractType { get; set; } = "Assurance Vie";
        public string Status { get; set; } = "En cours";
        public bool Locked { get; set; }
        public int OperationCount { get; set; }

        public DateTime DateSign { get; set; } = DateTime.Now;
        public DateTime DateEffect { get; set; }
        public DateTime? DateMaturity { get; set; }

        public string PostalAddress { get; set; } = string.Empty;
        public string TaxAddress { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";

        public int? PersonId { get; set; }
        public int? ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public ContractFamily? ContractFamily { get; set; }
        public string? ContractFamilyLabel { get; set; }

        public decimal InitialPremium { get; set; }
        public decimal TotalPaidPremiums { get; set; }
        public decimal PaidExecuted { get; set; }
        public decimal PaidPending { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal WithdrawnExecuted { get; set; }
        public decimal WithdrawnPending { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal FeeExecuted { get; set; }
        public decimal FeePending { get; set; }
        public decimal TotalFees { get; set; }
        public int TotalSwitches { get; set; }
        public decimal TotalSwitchesAmount { get; set; }
        public decimal NetInvested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal? PerformancePercent { get; set; }
        public decimal? RedemptionValue { get; set; }

        public string PaymentMode { get; set; } = "Libre";
        public decimal? ScheduledPayment { get; set; }

        public int? BeneficiaryClauseId { get; set; }
        public BeneficiaryClauseDto? BeneficiaryClause { get; set; }

        public decimal? EntryFeesRate { get; set; }
        public decimal? ManagementFeesRate { get; set; }
        public decimal? ExitFeesRate { get; set; }

        public string? AdvisorComment { get; set; }
        public bool HasAlert { get; set; }

        public string? ExternalReference { get; set; }

        public int? CreatedByUserId { get; set; }
        public int? LastModifiedByUserId { get; set; }

        public PersonDto? Person { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        // 🆕 Liste des assurés (personnes liées au contrat)
        public List<PersonDto> InsuredPersons { get; set; } = new();

        // 🆕 Poches (répartition des actifs)
        public List<CompartmentDto> Compartments { get; set; } = new();

        // 🆕 Supports financiers (répartition UC / ETF / fonds)
        public List<FinancialSupportAllocationDto> Supports { get; set; } = new();

        // 🆕 Options de contrat activées
        public ICollection<ContractOptionDto>? Options { get; set; } = new List<ContractOptionDto>();

        // 🆕 Documents attachés
        public ICollection<DocumentDto> Documents { get; set; } = new List<DocumentDto>();

        // 🆕 Versements cumulés sur le contrat
        public ICollection<PaymentDetailsDto> Payments { get; set; } = new List<PaymentDetailsDto>();

        // 🆕 Retraits cumulés liés au contrat
        public ICollection<WithdrawalDetailsDto> Withdrawals { get; set; } = new List<WithdrawalDetailsDto>();

        // public List<FinancialSupportAllocationDto> ConsolidatedSupports { get; set; } = new();

    }

}