using api.Models.Enum;

namespace api.Models
{
    public class LegalDocumentRevision
    {
        public int Id { get; set; }
        public int LegalDocumentDefinitionId { get; set; }
        public int? BasedOnRevisionId { get; set; }
        public int MajorVersion { get; set; } = 1;
        public int MinorVersion { get; set; }
        public DocumentRevisionStatus Status { get; set; } = DocumentRevisionStatus.Draft;
        public string? ChangeSummary { get; set; }
        public string? ValidationComment { get; set; }
        public int? DocumentLayoutTemplateId { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? ContentHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? ValidatedAt { get; set; }
        public string? ValidatedBy { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? PublishedBy { get; set; }
        public byte[] RowVersion { get; set; } = [];

        public LegalDocumentDefinition LegalDocumentDefinition { get; set; } = null!;
        public LegalDocumentRevision? BasedOnRevision { get; set; }
        public DocumentLayoutTemplate? DocumentLayoutTemplate { get; set; }
        public ICollection<LegalDocumentNode> Nodes { get; set; } = new List<LegalDocumentNode>();
        public ICollection<DocumentArtifact> Artifacts { get; set; } = new List<DocumentArtifact>();
    }
}
