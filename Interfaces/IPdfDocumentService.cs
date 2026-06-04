using api.Dtos.Pdf;

namespace api.Interfaces
{
    public interface IPdfDocumentService
    {
        Task<PdfGeneratedFileDto> GenerateAsync(GeneratePdfRequestDto request, CancellationToken cancellationToken = default);
        Task<PdfGeneratedFileDto> MergeAsync(MergePdfRequestDto request, CancellationToken cancellationToken = default);
    }
}
