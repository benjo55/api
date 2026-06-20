using System.Text.Json.Serialization;
using api.Models.Enum;

namespace api.Dtos.Operation
{
    /* =========================================================
       POLYMORPHISME JSON — discriminant : "kind"
    ========================================================= */

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(PaymentDetailsDto), "payment")]
    [JsonDerivedType(typeof(WithdrawalDetailsDto), "withdrawal")]
    [JsonDerivedType(typeof(ArbitrageDetailsDto), "arbitrage")]
    [JsonDerivedType(typeof(AdvanceDetailsDto), "advance")]
    public abstract class OperationDetailsDto { }

    /* =========================================================
       PAYMENT
    ========================================================= */

    public class PaymentDetailsDto : OperationDetailsDto
    {
        /// <summary>transfer | check | sepa</summary>
        public string Mode { get; set; } = "transfer";

        /// <summary>recurring | manual</summary>
        public string? PlanningMode { get; set; }

        /// <summary>Liste de dates fixes en mode manuel</summary>
        public List<DateTime>? FixedDates { get; set; }

        // --- transfer ---
        public string? SourceOfFunds { get; set; }
        public string? BankReference { get; set; }

        // --- check ---
        public string? CheckNumber { get; set; }
        public string? IssuerName { get; set; }
        public DateTime? DepositDate { get; set; }

        // --- sepa ---
        public string? MandateId { get; set; }
        public string? IbanMasked { get; set; }
        public string? CreditorId { get; set; }

        /// <summary>OOFF | FRST | RCUR | FNAL</summary>
        public string? SequenceType { get; set; }

        // --- transfer + sepa ---
        public string? Frequency { get; set; }
        public DateTime? StartDate { get; set; }

        // --- scheduled lifecycle ---
        /// <summary>active | suspended | stopped</summary>
        public string? ScheduleStatus { get; set; }
        public string? ScheduleGroupId { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public DateTime? StoppedAt { get; set; }
    }

    /* =========================================================
       WITHDRAWAL
    ========================================================= */

    public class WithdrawalDetailsDto : OperationDetailsDto
    {
        /// <summary>oneShot | scheduled</summary>
        public string Mode { get; set; } = "oneShot";

        /// <summary>recurring | manual</summary>
        public string? PlanningMode { get; set; }

        /// <summary>Liste de dates fixes en mode manuel</summary>
        public List<DateTime>? FixedDates { get; set; }

        public string? ScheduleGroupId { get; set; }

        public decimal? GrossAmount { get; set; }

        /// <summary>PFU | IR</summary>
        public string? TaxOption { get; set; }

        public string? Reason { get; set; }

        /// <summary>bySupport | byCompartment | byPercentage</summary>
        public string? AllocationMode { get; set; }

        // --- scheduled only ---
        /// <summary>monthly | quarterly | yearly</summary>
        public string? Frequency { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? RevaluationRate { get; set; }
    }

    /* =========================================================
       ARBITRAGE
    ========================================================= */

    public class ArbitrageDetailsDto : OperationDetailsDto
    {
        /// <summary>manual | auto</summary>
        public string? Mode { get; set; }

        /// <summary>recurring | manual</summary>
        public string? PlanningMode { get; set; }

        /// <summary>Liste de dates fixes en mode manuel</summary>
        public List<DateTime>? FixedDates { get; set; }

        public string? ScheduleGroupId { get; set; }

        /// <summary>monthly | quarterly | yearly | manual</summary>
        public string? Frequency { get; set; }

        public DateTime? StartDate { get; set; }

        public string? Motive { get; set; }

        /// <summary>none | equalizeTargets</summary>
        public string? RebalancePolicy { get; set; }
    }

    /* =========================================================
       ADVANCE
    ========================================================= */

    public class AdvanceDetailsDto : OperationDetailsDto
    {
        /// <summary>grant | repayment</summary>
        public string Mode { get; set; } = "grant";
        public int? AdvanceId { get; set; }
        public string? AdvanceNumber { get; set; }
        public AdvanceTransactionType? TransactionType { get; set; }
        public decimal? InterestRate { get; set; }
        public DateTime? MaturityDate { get; set; }
        public string? Comment { get; set; }
    }
}
