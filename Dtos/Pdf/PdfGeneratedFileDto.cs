namespace api.Dtos.Pdf
{
    public sealed class PdfGeneratedFileDto
    {
        public string FileName { get; set; } = "document";
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
