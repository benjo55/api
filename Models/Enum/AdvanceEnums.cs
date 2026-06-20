namespace api.Models.Enum
{
    public enum AdvanceStatus
    {
        Requested,
        Approved,
        Disbursed,
        Active,
        Renewed,
        Settled,
        Cancelled
    }

    public enum AdvanceTransactionType
    {
        Grant,
        Disbursement,
        PartialRepayment,
        TotalRepayment,
        InterestPayment,
        InterestCapitalization,
        Renewal
    }
}
