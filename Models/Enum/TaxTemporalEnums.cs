namespace api.Models.Enum
{
    public enum TaxRuleType
    {
        IncomeTax = 0,
        SocialCharges = 1,
        DeathTax = 2,
        Allowance = 3,
        ExitMode = 4,
    }

    public enum TaxCompartmentType
    {
        General = 0,
        PerVoluntaryDeducted = 1,
        PerVoluntaryNonDeducted = 2,
        PerEmployeeSavings = 3,
        PerMandatory = 4,
    }

    public enum SupportNature
    {
        Unknown = 0,
        Euro = 1,
        UnitLinked = 2,
        EuroCroissance = 3,
        Structured = 4,
        Pension = 5,
    }

    public enum TaxEventKind
    {
        PremiumPayment = 0,
        Valuation = 1,
        SocialChargesLevy = 2,
        Arbitrage = 3,
        PartialWithdrawal = 4,
        FullWithdrawal = 5,
        Death = 6,
        AnnuityConversion = 7,
        Correction = 8,
    }
}
