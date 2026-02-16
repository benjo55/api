using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class Contract
    {
        public int Id { get; set; }

        // 📄 Données générales
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractLabel { get; set; } = string.Empty;
        public string ContractType { get; set; } = "Assurance Vie"; // Ex : PER, Capitalisation…
        public string Status { get; set; } = "En cours";
        public DateTime DateSign { get; set; } = DateTime.Now;
        public DateTime DateEffect { get; set; } = DateTime.Now;
        public DateTime? DateMaturity { get; set; }

        public string Currency { get; set; } = "EUR";

        // 🏠 Adresses
        public string PostalAddress { get; set; } = string.Empty;
        public string TaxAddress { get; set; } = string.Empty;

        // 👤 Titulaire
        public int? PersonId { get; set; }
        [ForeignKey("PersonId")]
        public Person? Person { get; set; }
        public ICollection<ContractInsuredPerson> InsuredLinks { get; set; } = new List<ContractInsuredPerson>();

        // 👤 Assurés (optionnel)
        public bool IsJointContract { get; set; } = false;

        // 💰 Données financières
        public decimal InitialPremium { get; set; }
        public decimal TotalPaidPremiums { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal? RedemptionValue { get; set; }


        [Column(TypeName = "decimal(18,5)")]
        public decimal TotalPayments { get; set; }

        [Column(TypeName = "decimal(18,5)")]
        public decimal TotalWithdrawals { get; set; }

        [Column(TypeName = "decimal(18,5)")]
        public decimal NetInvested { get; set; }

        [Column(TypeName = "decimal(18,5)")]
        public decimal? PerformancePercent { get; set; }

        // 🔁 Paiement
        public string PaymentMode { get; set; } = "Libre";
        public decimal? ScheduledPayment { get; set; }

        // 🧾 Frais
        public decimal? EntryFeesRate { get; set; }
        public decimal? ManagementFeesRate { get; set; }
        public decimal? ExitFeesRate { get; set; }

        // 🧠 Suivi & options
        public string? AdvisorComment { get; set; }
        public bool HasAlert { get; set; } = false;

        public string? ExternalReference { get; set; }

        public int? CreatedByUserId { get; set; }
        public int? LastModifiedByUserId { get; set; }

        // 📎 Liens
        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int? BeneficiaryClauseId { get; set; }
        public BeneficiaryClause? BeneficiaryClause { get; set; }

        // 🧾 Documents
        public ICollection<Document> Documents { get; set; } = new List<Document>();

        // 🧾 Compartiments
        public ICollection<Compartment> Compartments { get; set; } = new List<Compartment>();

        // 📊 Répartition supports UC / ETF
        public ICollection<FinancialSupportAllocation> Supports { get; set; } = new List<FinancialSupportAllocation>();

        public ICollection<ContractOption> Options { get; set; } = new List<ContractOption>();

        public ICollection<Operation> Operations { get; set; } = new List<Operation>();
        public ICollection<ContractSupportHolding> ContractSupportHoldings { get; set; } = new List<ContractSupportHolding>();

        [NotMapped]
        public List<FinancialSupportAllocation> ConsolidatedSupports { get; set; } = new();

        // 📅 Audit
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        public bool Locked { get; set; } = false;

        [NotMapped]
        public decimal WithdrawnExecuted { get; set; }

        [NotMapped]
        public decimal WithdrawnPending { get; set; }

        [NotMapped]
        public decimal PaidExecuted { get; set; }

        [NotMapped]
        public decimal PaidPending { get; set; }

    }
}
