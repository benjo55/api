using api.Models.Enum;

namespace api.Models
{
    public class LegalDocumentNode
    {
        public int Id { get; set; }
        public int LegalDocumentRevisionId { get; set; }
        public int? ParentNodeId { get; set; }
        public string StableKey { get; set; } = Guid.NewGuid().ToString("N");
        public DocumentNodeType Type { get; set; }
        public string? BusinessCode { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? EditorJson { get; set; }
        public string? ContentHtml { get; set; }
        public string? PlainText { get; set; }
        public int SortOrder { get; set; }
        public bool IncludeInTableOfContents { get; set; } = true;
        public bool StartOnNewPage { get; set; }
        public bool KeepWithNext { get; set; }
        public string? NumberingStyle { get; set; }
        public bool IsConditional { get; set; }
        public string? DisplayConditionJson { get; set; }
        public int? SourceClauseRevisionId { get; set; }
        public byte[] RowVersion { get; set; } = [];

        public LegalDocumentRevision LegalDocumentRevision { get; set; } = null!;
        public LegalDocumentNode? ParentNode { get; set; }
        public ICollection<LegalDocumentNode> Children { get; set; } = new List<LegalDocumentNode>();
        public ClauseRevision? SourceClauseRevision { get; set; }
    }
}
