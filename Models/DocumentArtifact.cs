using api.Models.Enum;

namespace api.Models
{
    public class DocumentArtifact
    {
        public int Id { get; set; }
        public DocumentArtifactType Type { get; set; }
        public int? LegalDocumentRevisionId { get; set; }
        public int? ContractDocumentInstanceId { get; set; }
        public string StorageKey { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
        public string FileName { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string? GeneratedBy { get; set; }
        public string? CacheKey { get; set; }

        public LegalDocumentRevision? LegalDocumentRevision { get; set; }
        public ContractDocumentInstance? ContractDocumentInstance { get; set; }
    }
}
