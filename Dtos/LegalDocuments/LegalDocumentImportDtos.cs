using api.Models.Enum;

namespace api.Dtos.LegalDocuments
{
    public sealed class LegalDocumentImportFile
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public LegalDocumentType Type { get; set; } = LegalDocumentType.ProductGeneralTerms;
        public string? ChangeSummary { get; set; }
        public List<LegalDocumentImportNode> Nodes { get; set; } = [];
    }

    public sealed class LegalDocumentImportNode
    {
        public DocumentNodeType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? BusinessCode { get; set; }
        public string? ContentHtml { get; set; }
        public string? PlainText { get; set; }
        public bool IncludeInTableOfContents { get; set; } = true;
        public bool StartOnNewPage { get; set; }
        public bool KeepWithNext { get; set; }
        public string? NumberingStyle { get; set; }
        public List<LegalDocumentImportNode> Children { get; set; } = [];
    }

    public sealed record LegalDocumentImportResult(
        int DefinitionId,
        int RevisionId,
        int NodeCount,
        bool Imported);
}
