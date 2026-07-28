using api.Models.Enum;

namespace api.Dtos.LegalDocuments
{
    public record LegalDocumentDefinitionListDto(
        int Id,
        string Code,
        string Name,
        string? Description,
        LegalDocumentType Type,
        bool IsLibrary,
        int? CurrentDraftRevisionId,
        int? CurrentPublishedRevisionId,
        bool IsActive,
        string RowVersion);

    public record LegalDocumentDefinitionDto(
        int Id,
        string Code,
        string Name,
        string? Description,
        LegalDocumentType Type,
        bool IsLibrary,
        int? CurrentDraftRevisionId,
        int? CurrentPublishedRevisionId,
        bool IsActive,
        string RowVersion,
        IReadOnlyList<LegalDocumentRevisionSummaryDto> Revisions);

    public record CreateLegalDocumentDefinitionDto(
        string Code,
        string Name,
        string? Description,
        LegalDocumentType Type,
        bool IsLibrary = false);

    public record UpdateLegalDocumentDefinitionDto(
        string Name,
        string? Description,
        bool IsActive,
        string RowVersion);

    public record LegalDocumentRevisionSummaryDto(
        int Id,
        int LegalDocumentDefinitionId,
        int MajorVersion,
        int MinorVersion,
        DocumentRevisionStatus Status,
        string? ChangeSummary,
        DateTime CreatedAt,
        DateTime? ValidatedAt,
        DateTime? PublishedAt,
        string RowVersion);

    public record LegalDocumentRevisionDto(
        int Id,
        int LegalDocumentDefinitionId,
        int? BasedOnRevisionId,
        int MajorVersion,
        int MinorVersion,
        DocumentRevisionStatus Status,
        string? ChangeSummary,
        string? ValidationComment,
        DateTime? EffectiveFrom,
        DateTime? EffectiveTo,
        string? ContentHash,
        string RowVersion,
        IReadOnlyList<LegalDocumentNodeDto> Nodes);

    public record LegalDocumentNodeDto(
        int Id,
        int LegalDocumentRevisionId,
        int? ParentNodeId,
        string StableKey,
        DocumentNodeType Type,
        string? BusinessCode,
        string Title,
        string? EditorJson,
        string? ContentHtml,
        string? PlainText,
        int SortOrder,
        bool IncludeInTableOfContents,
        bool StartOnNewPage,
        bool KeepWithNext,
        string? NumberingStyle,
        bool IsConditional,
        string? DisplayConditionJson,
        string? Number,
        IReadOnlyList<LegalDocumentNodeDto> Children,
        string RowVersion);

    public record CreateLegalDocumentNodeDto(
        int? ParentNodeId,
        DocumentNodeType Type,
        string Title,
        string? BusinessCode,
        string? InsertRelativeToNodeId,
        string? InsertPosition);

    public record UpdateLegalDocumentNodeDto(
        string? BusinessCode,
        string Title,
        string? EditorJson,
        string? ContentHtml,
        string? PlainText,
        bool IncludeInTableOfContents,
        bool StartOnNewPage,
        bool KeepWithNext,
        string? NumberingStyle,
        bool IsConditional,
        string? DisplayConditionJson,
        string RowVersion);

    public record MoveLegalDocumentNodeDto(
        int? NewParentNodeId,
        int? BeforeNodeId,
        int? AfterNodeId,
        bool First,
        bool Last,
        string RowVersion);

    public record ReusableDocumentNodeDto(
        int Id,
        DocumentNodeType Type,
        string Title,
        string? PlainText,
        string? BusinessCode,
        int DescendantCount,
        int SourceRevisionId,
        string SourceDocumentCode,
        string SourceDocumentName,
        int MajorVersion,
        int MinorVersion,
        DocumentRevisionStatus Status);

    public record ImportDocumentNodeDto(
        int SourceNodeId,
        int ParentNodeId);

    public record CreateDocumentVersionDto(
        int SourceRevisionId,
        VersionBumpType BumpType,
        string ChangeSummary);

    public record WorkflowTransitionDto(string RowVersion, string? Comment);

    public record DocumentValidationIssueDto(
        string Code,
        ValidationIssueLevel Level,
        int? NodeId,
        string? StableKey,
        string Message,
        string? Property);

    public record DocumentValidationResultDto(
        bool IsValid,
        IReadOnlyList<DocumentValidationIssueDto> Issues);

    public record DocumentPreviewRequestDto(string RevisionStamp);

    public record DocumentPreviewDto(
        int ArtifactId,
        string FileName,
        string ContentType,
        string Hash,
        string RevisionStamp,
        bool IsCurrent);

    public record DocumentArtifactDto(
        int Id,
        DocumentArtifactType Type,
        string FileName,
        string ContentType,
        string Hash,
        long Size,
        DateTime GeneratedAt);

    public record DocumentAuditEventDto(
        int Id,
        DocumentAuditAction Action,
        int? LegalDocumentRevisionId,
        int? LegalDocumentNodeId,
        string? DetailJson,
        DateTime CreatedAt,
        string? CreatedBy);

    public record RevisionComparisonDto(
        int LeftRevisionId,
        int RightRevisionId,
        IReadOnlyList<string> AddedStableKeys,
        IReadOnlyList<string> RemovedStableKeys,
        IReadOnlyList<string> ChangedStableKeys);

    public record ProductDocumentAssignmentDto(
        int Id,
        int ProductId,
        int LegalDocumentRevisionId,
        string DocumentCode,
        string DocumentName,
        int MajorVersion,
        int MinorVersion,
        ProductDocumentRole Role,
        DateTime ValidFrom,
        DateTime? ValidTo,
        bool IsCurrent,
        int? LatestPdfArtifactId,
        string RowVersion);

    public record PublishedLegalDocumentRevisionDto(
        int Id,
        int LegalDocumentDefinitionId,
        string DocumentCode,
        string DocumentName,
        LegalDocumentType Type,
        int MajorVersion,
        int MinorVersion,
        DateTime? PublishedAt);

    public record CreateProductDocumentAssignmentDto(
        int ProductId,
        int LegalDocumentRevisionId,
        ProductDocumentRole Role,
        DateTime ValidFrom,
        DateTime? ValidTo,
        bool IsCurrent);

    public record DocumentVariableDefinitionDto(
        string Code,
        string Label,
        DocumentVariableValueType Type,
        string Scope,
        bool IsRequired);

    public record SafeDisplayConditionDto(
        string VariableCode,
        string Operator,
        string? Value);
}
