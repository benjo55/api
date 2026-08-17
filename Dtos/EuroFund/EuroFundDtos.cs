using api.Models;

namespace api.Dtos.EuroFund
{
    public class EuroFundConfigurationDto
    {
        public int FinancialSupportId { get; set; }
        public EuroFundAccrualMethod AccrualMethod { get; set; } = EuroFundAccrualMethod.ActualDaysSimpleProrata;
        public int AnnualCreditMonth { get; set; } = 12;
        public int AnnualCreditDay { get; set; } = 31;
        public EuroFundProvisionalRateMethod ProvisionalRateMethod { get; set; } = EuroFundProvisionalRateMethod.TmePercentage;
        public decimal? ProvisionalRatePercentage { get; set; } = 70m;
        public decimal? FixedProvisionalRate { get; set; }
        public decimal? PreviousFinalRatePercentage { get; set; }
        public EuroFundEarlyExitRateMethod EarlyExitRateMethod { get; set; } = EuroFundEarlyExitRateMethod.ProvisionalRate;
        public EuroFundLotConsumptionMethod LotConsumptionMethod { get; set; } = EuroFundLotConsumptionMethod.ProRata;
        public EuroFundValueDateRule ValueDateRule { get; set; } = EuroFundValueDateRule.NextBusinessDay;
        public int ValueDateDelayDays { get; set; } = 1;
        public EuroFundRateNature RateNature { get; set; } = EuroFundRateNature.NetOfManagementFees;
        public EuroFundManagementFeeTreatment ManagementFeeTreatment { get; set; } = EuroFundManagementFeeTreatment.IncludedInServedRate;
        public decimal? MinimumGuaranteedRate { get; set; }
        public decimal? RateFloor { get; set; }
        public decimal? RateCap { get; set; }
    }

    public class EuroFundSummaryDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public EuroFundConfigurationDto Configuration { get; set; } = new();
    }

    public class EuroFundFinancialYearDto
    {
        public int FinancialSupportId { get; set; }
        public int Year { get; set; }
        public decimal? TmeRate { get; set; }
        public decimal? AssetYield { get; set; }
        public decimal? OpeningPpbReserve { get; set; }
        public decimal? PpbAllocation { get; set; }
        public decimal? PpbRelease { get; set; }
        public decimal? ClosingPpbReserve { get; set; }
        public decimal? FinalServedRate { get; set; }
        public EuroFundRateNature RateNature { get; set; } = EuroFundRateNature.NetOfManagementFees;
        public EuroFundFinancialYearStatus Status { get; set; } = EuroFundFinancialYearStatus.Open;
    }

    public class ReferenceRateDto
    {
        public ReferenceRateType RateType { get; set; } = ReferenceRateType.Tme;
        public DateTime RateDate { get; set; }
        public decimal RateValue { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    public class EuroFundValuationDto
    {
        public int ContractId { get; set; }
        public int FinancialSupportId { get; set; }
        public DateTime ValuationDate { get; set; }
        public decimal BookValue { get; set; }
        public decimal EstimatedAccruedInterest { get; set; }
        public decimal EstimatedValue { get; set; }
        public decimal ProvisionalRate { get; set; }
        public string ProvisionalRateLabel { get; set; } = string.Empty;
        public decimal? LastParticipationBenefit { get; set; }
        public int? LastParticipationBenefitYear { get; set; }
        public decimal? PreviousFinalServedRate { get; set; }
    }

    public class EuroFundPreviewDto
    {
        public int FinancialSupportId { get; set; }
        public int FinancialYear { get; set; }
        public int ContractCount { get; set; }
        public decimal TotalBookValue { get; set; }
        public decimal TotalParticipationBenefit { get; set; }
        public decimal AverageParticipationBenefit { get; set; }
        public decimal AppliedRate { get; set; }
        public bool UsesFinalRate { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<EuroFundContractPreviewDto> Contracts { get; set; } = new();
    }

    public class EuroFundContractPreviewDto
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public decimal BookValue { get; set; }
        public decimal WeightedExposure { get; set; }
        public decimal ParticipationBenefit { get; set; }
        public List<EuroFundRevaluationDetailDto> Details { get; set; } = new();
    }

    public class EuroFundRevaluationDetailDto
    {
        public int LotId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal OpeningAmount { get; set; }
        public decimal BaseRate { get; set; }
        public decimal BonusRate { get; set; }
        public decimal ApplicableRate { get; set; }
        public int DayCount { get; set; }
        public int YearBasis { get; set; }
        public decimal InterestAmount { get; set; }
    }
}
