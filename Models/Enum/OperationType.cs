public enum OperationType
{
    // Versements
    InitialPayment,
    ScheduledPayment,
    FreePayment,
    ParticipationBenefit,
    InterestPayment,
    CouponDetachment,

    // Rachats
    PartialWithdrawal,
    ScheduledWithdrawal,
    TotalWithdrawal,
    ManagementFee,
    OperationFee,

    // Gestion financière
    Arbitrage,
    ScheduledArbitrage,
    Advance,

    // Transmission
    Succession,
    Donation,

    // Administratif
    BeneficiaryChange,
    Pledge,
    ConversionToAnnuity,
    AdvanceRepayment
}

public enum OperationStatus
{
    Pending,
    Executed,
    Cancelled,
    Failed
}
