using api.Models.Enum;

namespace api.Models
{
    public class ProductDocumentAssignment
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int LegalDocumentRevisionId { get; set; }
        public ProductDocumentRole Role { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public bool IsCurrent { get; set; }
        public byte[] RowVersion { get; set; } = [];

        public Product Product { get; set; } = null!;
        public LegalDocumentRevision LegalDocumentRevision { get; set; } = null!;
    }
}
