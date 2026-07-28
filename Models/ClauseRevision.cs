using api.Models.Enum;

namespace api.Models
{
    public class ClauseRevision
    {
        public int Id { get; set; }
        public int ClauseDefinitionId { get; set; }
        public int MajorVersion { get; set; } = 1;
        public int MinorVersion { get; set; }
        public DocumentRevisionStatus Status { get; set; } = DocumentRevisionStatus.Draft;
        public string? EditorJson { get; set; }
        public string? ContentHtml { get; set; }
        public string? PlainText { get; set; }
        public string? ContentHash { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public byte[] RowVersion { get; set; } = [];

        public ClauseDefinition ClauseDefinition { get; set; } = null!;
    }
}
