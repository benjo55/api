
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public enum TypeOperation
    {
        InvestissementUnitaire, InvestissementPeriodique, Arbitrage, DesinvestissementUnitaire, DesinvestissementPeriodique, DesinvestissementTotal, Avance
    }
    public class Operation
    {
        public int Id { get; set; }

        // 🔹 Toujours lié à un contrat
        public int ContractId { get; set; }
        public Contract Contract { get; set; } = null!;

        // 🔹 Optionnellement lié à un compartiment
        public int? CompartmentId { get; set; }
        public Compartment? Compartment { get; set; }

        // Type & statut
        public OperationType Type { get; set; }
        public OperationStatus Status { get; set; } = OperationStatus.Pending;

        // Données génériques
        public DateTime OperationDate { get; set; }
        [Column(TypeName = "decimal(20,7)")]
        public decimal? Amount { get; set; }
        public string Currency { get; set; } = "EUR";

        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // 🔹 Relations 1–1 vers les détails spécifiques
        public WithdrawalDetail? WithdrawalDetail { get; set; }
        public ArbitrageDetail? ArbitrageDetail { get; set; }
        public AdvanceDetail? AdvanceDetail { get; set; }
        public PaymentDetail? PaymentDetail { get; set; }

        // 🔹 Répartition sur supports
        public ICollection<OperationSupportAllocation> Allocations { get; set; } = new List<OperationSupportAllocation>();

    }

}