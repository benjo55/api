namespace api.Models.Enum
{
    public enum ManagementFeeRateBase
    {
        Annual,
        SemiAnnual,
        Quarterly,
        Monthly
    }

    public enum ManagementFeeFrequency
    {
        Monthly,
        Quarterly,
        Yearly
    }

    public enum ManagementFeeProrataMethod
    {
        Periodic,
        Actual365,
        ExactPeriodicCompounded
    }

    public enum ManagementFeePostingMode
    {
        UnitCancellation,
        NetServedYield
    }
}