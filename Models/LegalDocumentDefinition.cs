using api.Models.Enum;

namespace api.Models
{
    public class LegalDocumentDefinition
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public LegalDocumentType Type { get; set; }
        public bool IsLibrary { get; set; }
        public int? CurrentDraftRevisionId { get; set; }
        public int? CurrentPublishedRevisionId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public byte[] RowVersion { get; set; } = [];

        public LegalDocumentRevision? CurrentDraftRevision { get; set; }
        public LegalDocumentRevision? CurrentPublishedRevision { get; set; }
        public ICollection<LegalDocumentRevision> Revisions { get; set; } = new List<LegalDocumentRevision>();
    }
}
