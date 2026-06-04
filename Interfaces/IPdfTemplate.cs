using api.Dtos.Pdf;

namespace api.Interfaces
{
    public interface IPdfTemplate
    {
        IReadOnlyCollection<PdfDocumentType> SupportedDocumentTypes { get; }

        byte[] Render(
            GeneratePdfRequestDto request,
            byte[]? logoImage,
            byte[]? qrCodeImage,
            byte[]? chartImage,
            IReadOnlyList<PdfResolvedChartDto> charts);
    }
}
