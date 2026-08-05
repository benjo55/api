using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace api.Dtos.Documents
{
    public sealed class GenerateDocumentRequestDto
    {
        [StringLength(120)]
        public string? SubjectId { get; set; }

        public JsonElement? Parameters { get; set; }

        public DocumentDeliveryMode DeliveryMode { get; set; } = DocumentDeliveryMode.Download;
    }

    public enum DocumentDeliveryMode
    {
        Preview,
        Download,
        Archive,
        Email
    }
}
