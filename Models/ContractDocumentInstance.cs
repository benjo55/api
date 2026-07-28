using api.Models.Enum;

namespace api.Models
{
    public class ContractDocumentInstance
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public int TemplateRevisionId { get; set; }
        public int? ApplicableGeneralTermsRevisionId { get; set; }
        public string DataSnapshotJson { get; set; } = "{}";
        public string ContentHash { get; set; } = string.Empty;
        public int? PdfArtifactId { get; set; }
        public ContractDocumentStatus Status { get; set; } = ContractDocumentStatus.Draft;
        public DateTime? IssuedAt { get; set; }
        public string? IssuedBy { get; set; }
        public byte[] RowVersion { get; set; } = [];

        public Contract Contract { get; set; } = null!;
        public LegalDocumentRevision TemplateRevision { get; set; } = null!;
        public LegalDocumentRevision? ApplicableGeneralTermsRevision { get; set; }
        public DocumentArtifact? PdfArtifact { get; set; }
    }
}
