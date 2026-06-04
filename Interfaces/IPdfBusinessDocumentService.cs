using api.Dtos.Pdf;

namespace api.Interfaces
{
    public interface IPdfBusinessDocumentService
    {
        Task<PdfGeneratedFileDto> GenerateContractSheetAsync(GenerateContractSheetRequestDto request, CancellationToken cancellationToken = default);
        Task<PdfGeneratedFileDto> GenerateClientCaseFileAsync(GenerateClientCaseFileRequestDto request, CancellationToken cancellationToken = default);
    }
}
