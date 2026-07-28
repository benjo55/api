namespace api.Models.Enum
{
    public enum DonorType
    {
        Individual,
        Company
    }

    public enum DonationForm
    {
        AuthenticDeed,
        PrivateDeed,
        ManualGiftDeclaration,
        Other
    }

    public enum DonationNature
    {
        Cash,
        ListedSecurities,
        ExpressWaiverOfIncome,
        VolunteerExpensesWaived,
        InKind,
        Other
    }

    public enum DonationPaymentMethod
    {
        Cash,
        Cheque,
        BankTransfer,
        DirectDebit,
        BankCard,
        Other
    }

    public enum DonationTaxRegime
    {
        Article200,
        Article978,
        Article200And978
    }

    public enum DonationStatus
    {
        Draft,
        Validated,
        AwaitingPayment,
        PaymentPending,
        Paid,
        ReceiptGenerated,
        Completed,
        Failed,
        Cancelled
    }

    public enum PaymentStatus
    {
        Created,
        RedirectRequired,
        RedirectReady,
        Pending,
        Processing,
        Authorized,
        Succeeded,
        Failed,
        Refused,
        Cancelled,
        Expired,
        Refunded,
        PartiallyRefunded,
        Contested,
        Unknown
    }

    public enum PaymentProvider
    {
        HelloAsso,
        BankTransfer,
        PayPal,
        CardProvider
    }

    public enum WebhookProcessingStatus
    {
        Pending,
        Processed,
        Failed
    }

    public enum BeneficiaryIdentifierType
    {
        Siren,
        Rna,
        AlsaceMoselleRegistration
    }

    public enum BeneficiaryOrganizationCategory
    {
        GeneralInterestOrganization,
        CultAssociationAlsaceMoselle,
        EndowmentFund,
        PressPluralismAssociation,
        HigherEducationInstitution,
        ConsularHigherEducationInstitution,
        SmeSupportOrganization,
        PerformingArtsOrganization,
        HeritageFoundation,
        CulturalPropertySafeguardOrganization,
        ResearchOrganization,
        WorkIntegrationCompany,
        IntermediateAssociation,
        WorkIntegrationWorkshop,
        AdaptedCompany,
        NationalResearchAgency,
        EmployerIntegrationGroup,
        EntrepreneurshipSupportAssociation,
        EuropeanEquivalentOrganization,
        Other
    }

    public enum BeneficiaryOrganizationSubCategory
    {
        None,
        Association1901,
        PublicUtilityAssociationOrFoundation,
        UniversityOrPartnershipFoundation,
        CorporateFoundation,
        MuseumOfFrance,
        FoodMedicalHousingAidOrganization,
        ForestryOrganization,
        Other
    }

    public enum TaxReceiptStatus
    {
        Draft,
        Pending,
        Ready,
        Generated,
        Sent,
        Cancelled,
        Replaced,
        GenerationFailed,
        EmailFailed
    }

    public enum TaxReceiptEmailStatus
    {
        Pending,
        Sent,
        Failed
    }

}
