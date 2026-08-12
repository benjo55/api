using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{
    public enum EuroFundAccrualMethod
    {
        ActualDaysSimpleProrata,
        CivilFortnight,
        GeometricDailyEquivalent,
        Custom
    }

    public enum EuroFundProvisionalRateMethod
    {
        None,
        FixedRate,
        TmePercentage,
        PreviousFinalRatePercentage,
        Custom
    }

    public enum EuroFundEarlyExitRateMethod
    {
        FinalRateIfKnown,
        ProvisionalRate,
        PreviousFinalRatePercentage,
        TmePercentage,
        GuaranteedRate,
        Custom
    }

    public enum EuroFundLotConsumptionMethod
    {
        Fifo,
        Lifo,
        ProRata,
        BonusFirst,
        Custom
    }

    public enum EuroFundRateNature
    {
        NetOfManagementFees,
        GrossBeforeManagementFees
    }

    public enum EuroFundManagementFeeTreatment
    {
        IncludedInServedRate,
        DeductedByFeeOperation
    }

    public enum EuroFundFinancialYearStatus
    {
        Open,
        Provisional,
        Finalized
    }

    public enum ReferenceRateType
    {
        Tme
    }

    public enum EuroFundLotMovementType
    {
        In,
        Out,
        ProfitParticipation,
        Migration,
        Adjustment
    }

    public class EuroFundConfiguration
    {
        public int Id { get; set; }

        public int FinancialSupportId { get; set; }
        public FinancialSupport FinancialSupport { get; set; } = null!;

        public EuroFundAccrualMethod AccrualMethod { get; set; } = EuroFundAccrualMethod.ActualDaysSimpleProrata;
        public int AnnualCreditMonth { get; set; } = 12;
        public int AnnualCreditDay { get; set; } = 31;
        public EuroFundProvisionalRateMethod ProvisionalRateMethod { get; set; } = EuroFundProvisionalRateMethod.TmePercentage;
        [Precision(18, 7)] public decimal? ProvisionalRatePercentage { get; set; } = 70m;
        [Precision(18, 7)] public decimal? FixedProvisionalRate { get; set; }
        [Precision(18, 7)] public decimal? PreviousFinalRatePercentage { get; set; }
        public EuroFundEarlyExitRateMethod EarlyExitRateMethod { get; set; } = EuroFundEarlyExitRateMethod.ProvisionalRate;
        public EuroFundLotConsumptionMethod LotConsumptionMethod { get; set; } = EuroFundLotConsumptionMethod.ProRata;
        public EuroFundRateNature RateNature { get; set; } = EuroFundRateNature.NetOfManagementFees;
        public EuroFundManagementFeeTreatment ManagementFeeTreatment { get; set; } = EuroFundManagementFeeTreatment.IncludedInServedRate;
        [Precision(18, 7)] public decimal? MinimumGuaranteedRate { get; set; }
        [Precision(18, 7)] public decimal? RateFloor { get; set; }
        [Precision(18, 7)] public decimal? RateCap { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }

    public class EuroFundFinancialYear
    {
        public int Id { get; set; }
        public int FinancialSupportId { get; set; }
        public FinancialSupport FinancialSupport { get; set; } = null!;
        public int Year { get; set; }
        [Precision(18, 7)] public decimal? TmeRate { get; set; }
        [Precision(18, 7)] public decimal? AssetYield { get; set; }
        [Precision(20, 7)] public decimal? OpeningPpbReserve { get; set; }
        [Precision(20, 7)] public decimal? PpbAllocation { get; set; }
        [Precision(20, 7)] public decimal? PpbRelease { get; set; }
        [Precision(20, 7)] public decimal? ClosingPpbReserve { get; set; }
        [Precision(18, 7)] public decimal? FinalServedRate { get; set; }
        public EuroFundRateNature RateNature { get; set; } = EuroFundRateNature.NetOfManagementFees;
        public EuroFundFinancialYearStatus Status { get; set; } = EuroFundFinancialYearStatus.Open;
        public DateTime? FinalizedAt { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }

    public class ReferenceRate
    {
        public int Id { get; set; }
        public ReferenceRateType RateType { get; set; }
        public DateTime RateDate { get; set; }
        [Precision(18, 7)] public decimal RateValue { get; set; }
        [MaxLength(120)] public string Source { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class EuroFundLot
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public Contract Contract { get; set; } = null!;
        public int FinancialSupportId { get; set; }
        public FinancialSupport FinancialSupport { get; set; } = null!;
        public int? SourceOperationId { get; set; }
        public Operation? SourceOperation { get; set; }
        [Precision(20, 7)] public decimal InitialAmount { get; set; }
        [Precision(20, 7)] public decimal RemainingAmount { get; set; }
        public DateTime ValueDate { get; set; }
        [MaxLength(80)] public string? BonusRuleId { get; set; }
        [Precision(18, 7)] public decimal? BonusRate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<EuroFundLotMovement> Movements { get; set; } = new List<EuroFundLotMovement>();
    }

    public class EuroFundLotMovement
    {
        public int Id { get; set; }
        public int EuroFundLotId { get; set; }
        public EuroFundLot EuroFundLot { get; set; } = null!;
        public int ContractId { get; set; }
        public int FinancialSupportId { get; set; }
        public int? OperationId { get; set; }
        public Operation? Operation { get; set; }
        public DateTime MovementDate { get; set; }
        [Precision(20, 7)] public decimal Amount { get; set; }
        public EuroFundLotMovementType MovementType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class EuroFundRevaluation
    {
        public int Id { get; set; }
        public int OperationId { get; set; }
        public Operation Operation { get; set; } = null!;
        public int ContractId { get; set; }
        public Contract Contract { get; set; } = null!;
        public int FinancialSupportId { get; set; }
        public FinancialSupport FinancialSupport { get; set; } = null!;
        public int FinancialYear { get; set; }
        [Precision(18, 7)] public decimal FinalServedRate { get; set; }
        [Precision(20, 7)] public decimal BookValueBeforeCredit { get; set; }
        [Precision(20, 7)] public decimal WeightedExposure { get; set; }
        [Precision(20, 7)] public decimal InterestAmount { get; set; }
        public int YearBasis { get; set; }
        public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
        public ICollection<EuroFundRevaluationDetail> Details { get; set; } = new List<EuroFundRevaluationDetail>();
    }

    public class EuroFundRevaluationDetail
    {
        public int Id { get; set; }
        public int EuroFundRevaluationId { get; set; }
        public EuroFundRevaluation EuroFundRevaluation { get; set; } = null!;
        public int EuroFundLotId { get; set; }
        public EuroFundLot EuroFundLot { get; set; } = null!;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        [Precision(20, 7)] public decimal OpeningAmount { get; set; }
        [Precision(18, 7)] public decimal BaseRate { get; set; }
        [Precision(18, 7)] public decimal BonusRate { get; set; }
        [Precision(18, 7)] public decimal ApplicableRate { get; set; }
        public int DayCount { get; set; }
        public int YearBasis { get; set; }
        [Precision(20, 7)] public decimal InterestAmount { get; set; }
    }
}
