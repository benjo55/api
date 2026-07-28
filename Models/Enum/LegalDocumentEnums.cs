namespace api.Models.Enum
{
    public enum LegalDocumentType
    {
        ProductGeneralTerms,
        ContractSpecificTerms,
        RegulatoryDocument
    }

    public enum DocumentRevisionStatus
    {
        Draft,
        InReview,
        Validated,
        Published,
        Rejected,
        Superseded,
        Archived
    }

    public enum DocumentNodeType
    {
        Document,
        Part,
        Title,
        Chapter,
        Section,
        Article,
        Paragraph,
        Clause,
        Table,
        Callout,
        PageBreak
    }

    public enum ProductDocumentRole
    {
        GeneralTerms,
        Notice,
        RegulatoryNotice
    }

    public enum DocumentArtifactType
    {
        PreviewPdf,
        ValidatedPdf,
        IssuedPdf,
        Html
    }

    public enum ContractDocumentStatus
    {
        Draft,
        Issued,
        Cancelled
    }

    public enum VersionBumpType
    {
        Major,
        Minor
    }

    public enum ValidationIssueLevel
    {
        Info,
        Warning,
        Error
    }

    public enum DocumentAuditAction
    {
        Created,
        Updated,
        Moved,
        Deleted,
        Duplicated,
        Submitted,
        Validated,
        Rejected,
        Published,
        VersionCreated,
        PreviewGenerated,
        ContentImported,
        ProductAssigned,
        ContractIssued
    }

    public enum DocumentVariableValueType
    {
        String,
        Date,
        Decimal,
        Integer,
        Boolean,
        Address,
        Currency,
        Percentage
    }
}
