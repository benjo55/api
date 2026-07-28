using api.Models.Enum;

namespace api.Models
{
    public class DocumentAuditEvent
    {
        public int Id { get; set; }
        public int? LegalDocumentDefinitionId { get; set; }
        public int? LegalDocumentRevisionId { get; set; }
        public int? LegalDocumentNodeId { get; set; }
        public DocumentAuditAction Action { get; set; }
        public string? DetailJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public LegalDocumentDefinition? LegalDocumentDefinition { get; set; }
        public LegalDocumentRevision? LegalDocumentRevision { get; set; }
        public LegalDocumentNode? LegalDocumentNode { get; set; }
    }
}
