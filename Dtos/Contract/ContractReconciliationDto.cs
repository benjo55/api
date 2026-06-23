namespace api.Dtos.Contract
{
    public sealed class ContractReconciliationDto
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public ContractReconciliationMetricsDto Metrics { get; set; } = new();
        public List<ReconciliationCheckDto> Checks { get; set; } = [];
        public List<SupportReconciliationDto> Supports { get; set; } = [];
        public List<ArbitrageReconciliationDto> Arbitrages { get; set; } = [];
        public AdvanceReconciliationDto Advances { get; set; } = new();
        public bool IsConsistent => Checks.All(c => c.IsValid);
    }

    public sealed class ContractReconciliationMetricsDto
    {
        public decimal ContributionsExecuted { get; set; }
        public decimal ContributionsPending { get; set; }
        public decimal WithdrawalsExecuted { get; set; }
        public decimal WithdrawalsPending { get; set; }
        public decimal FeesExecuted { get; set; }
        public decimal NetExternalCash { get; set; }
        public decimal SettledMovementBalance { get; set; }
        public decimal CurrentValueStored { get; set; }
        public decimal CurrentValueFromPositions { get; set; }
        public decimal RemainingCostBasis { get; set; }
        public decimal SimpleGain { get; set; }
        public decimal SimplePerformancePercent { get; set; }
    }

    public sealed class ReconciliationCheckDto
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal Expected { get; set; }
        public decimal Actual { get; set; }
        public decimal Delta { get; set; }
        public decimal Tolerance { get; set; }
        public bool IsValid { get; set; }
    }

    public sealed class SupportReconciliationDto
    {
        public int SupportId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Isin { get; set; } = string.Empty;
        public decimal Shares { get; set; }
        public decimal RemainingCostBasis { get; set; }
        public decimal Pru { get; set; }
        public decimal Nav { get; set; }
        public DateTime? NavDate { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal RecalculatedValue { get; set; }
        public decimal UnrealizedGain { get; set; }
        public decimal PricePerformancePercent { get; set; }
    }

    public sealed class ArbitrageReconciliationDto
    {
        public int OperationId { get; set; }
        public DateTime OperationDate { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal ExecutedAmount { get; set; }
        public decimal SourceAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal BalanceDelta { get; set; }
    }

    public sealed class AdvanceReconciliationDto
    {
        public decimal Requested { get; set; }
        public decimal Approved { get; set; }
        public decimal Disbursed { get; set; }
        public decimal PrincipalRepaid { get; set; }
        public decimal OutstandingPrincipal { get; set; }
        public decimal AccruedInterest { get; set; }
    }
}
