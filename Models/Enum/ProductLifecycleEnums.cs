namespace api.Models.Enum
{
    public enum ProductStatus
    {
        Draft = 1,
        UnderReview = 2,
        Approved = 3,
        Active = 4,
        ClosedToNewBusiness = 5,
        ClosedToPayments = 6,
        RunOff = 7,
        Archived = 8
    }

    public enum ProductVersionStatus
    {
        Draft = 1,
        Validated = 2,
        Published = 3,
        Superseded = 4,
        Archived = 5
    }

    public enum ProductOperationType
    {
        InitialPayment = 1,
        AdditionalPayment = 2,
        ScheduledPayment = 3,
        PartialWithdrawal = 4,
        TotalWithdrawal = 5,
        ScheduledWithdrawal = 6,
        Arbitration = 7,
        Advance = 8,
        IncomingTransfer = 9,
        OutgoingTransfer = 10,
        BeneficiaryClauseChange = 11,
        Pledge = 12,
        ManagementModeChange = 13,
        AnnuityConversion = 14
    }

    public enum ProductPaymentType
    {
        Initial = 1,
        Additional = 2,
        Scheduled = 3
    }

    public enum ProductPaymentFrequency
    {
        OneShot = 1,
        Monthly = 2,
        Quarterly = 3,
        SemiAnnual = 4,
        Annual = 5
    }

    public enum ProductFeeCalculationMethod
    {
        Percentage = 1,
        FixedAmount = 2,
        Mixed = 3
    }

    public enum ProductGuaranteeType
    {
        DeathFloor = 1,
        EnhancedDeath = 2,
        CapitalGuarantee = 3,
        AnnuityOption = 4,
        ContributionWaiver = 5,
        Other = 99
    }

    public enum ProductManagementModeType
    {
        FreeManagement = 1,
        AdvisedManagement = 2,
        ManagedPortfolio = 3,
        HorizonManagement = 4,
        ProfiledManagement = 5,
        AutomaticRebalancing = 6,
        SecuredGains = 7,
        StopLoss = 8
    }

    public enum ProductDocumentType
    {
        GeneralConditions = 1,
        StandardSpecialConditions = 2,
        InformationNotice = 3,
        KeyInformationDocument = 4,
        PlanRules = 5,
        FinancialAppendix = 6,
        SupportList = 7,
        FeeSchedule = 8,
        SubscriptionForm = 9,
        Amendment = 10,
        Other = 99
    }

    public enum ProductEligibilityRuleType
    {
        NaturalPersonAllowed = 1,
        LegalPersonAllowed = 2,
        MinimumAge = 3,
        MaximumAge = 4,
        TaxResidenceCountry = 5,
        MarketingCountry = 6,
        RequiresInsuredPerson = 7,
        MaximumHolders = 8,
        MinorSubscriptionAllowed = 9,
        ProtectedAdultSubscriptionAllowed = 10,
        CollectiveContractCondition = 11,
        Other = 99
    }
}
