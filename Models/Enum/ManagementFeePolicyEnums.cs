namespace api.Models.Enum
{
    public enum ManagementFeeFrequency
    {
        Monthly,
        Quarterly,
        Yearly
    }

    public enum ManagementFeeProrataMethod
    {
        Periodic,
        Actual365
    }

    public enum ManagementFeePostingMode
    {
        UnitCancellation,
        NetServedYield
    }
}