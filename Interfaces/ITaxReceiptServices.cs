using api.Dtos.Generic;
using api.Dtos.TaxReceipts;
using api.Models;

namespace api.Interfaces
{
    public interface IDonorService
    {
        Task<PagedResult<DonorDto>> GetAllAsync(api.Helpers.QueryObject query, CancellationToken cancellationToken = default);
        Task<DonorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<DonorDto> CreateAsync(SaveDonorDto dto, CancellationToken cancellationToken = default);
        Task<DonorDto?> UpdateAsync(int id, SaveDonorDto dto, CancellationToken cancellationToken = default);
        Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DonationDto>> GetDonationsAsync(int donorId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DonorDto>> FindDuplicatesAsync(SaveDonorDto dto, CancellationToken cancellationToken = default);
    }

    public interface IDonationService
    {
        Task<PagedResult<DonationDto>> GetAllAsync(api.Helpers.QueryObject query, CancellationToken cancellationToken = default);
        Task<DonationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<DonationDto> CreateAsync(SaveDonationDto dto, CancellationToken cancellationToken = default);
        Task<DonationDto?> UpdateAsync(int id, SaveDonationDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<DonationDto?> ValidateAsync(int id, CancellationToken cancellationToken = default);
        Task<DonationDto?> CancelAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TaxReceiptDto>> GetReceiptsAsync(int donationId, CancellationToken cancellationToken = default);
    }

    public interface IBeneficiaryOrganizationService
    {
        Task<IReadOnlyList<BeneficiaryOrganizationDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<BeneficiaryOrganizationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<BeneficiaryOrganizationDto?> GetActiveAsync(CancellationToken cancellationToken = default);
        Task<BeneficiaryOrganizationDto> CreateAsync(SaveBeneficiaryOrganizationDto dto, CancellationToken cancellationToken = default);
        Task<BeneficiaryOrganizationDto?> UpdateAsync(int id, SaveBeneficiaryOrganizationDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }

    public interface ITaxReceiptService
    {
        Task<PagedResult<TaxReceiptDto>> GetAllAsync(api.Helpers.QueryObject query, CancellationToken cancellationToken = default);
        Task<TaxReceiptDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<TaxReceiptDto> CreateForDonationAsync(int donationId, CreateTaxReceiptDto dto, string? userName, CancellationToken cancellationToken = default);
        Task<TaxReceiptDto?> ValidateAsync(int id, CancellationToken cancellationToken = default);
        Task<TaxReceiptGenerationResultDto> GenerateAsync(int id, string? userName, CancellationToken cancellationToken = default);
        Task<(byte[] Content, string FileName)> GetPdfAsync(int id, CancellationToken cancellationToken = default);
        Task<TaxReceiptDto?> CancelAsync(int id, string? reason, CancellationToken cancellationToken = default);
        Task<TaxReceiptDto> ReplaceAsync(int id, string? userName, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TaxReceiptEmailHistoryDto>> GetEmailHistoryAsync(int id, CancellationToken cancellationToken = default);
    }

    public interface ITaxReceiptNumberGenerator
    {
        Task<string> GenerateAsync(CancellationToken cancellationToken = default);
    }

    public interface IAmountToWordsService
    {
        string ToFrenchEuros(decimal amount);
    }

    public interface ITaxReceiptPdfGenerator
    {
        string CerfaCode { get; }
        string CerfaVersion { get; }
        Task<byte[]> GenerateAsync(TaxReceipt receipt, CancellationToken cancellationToken = default);
    }

    public interface ITaxReceiptEmailService
    {
        Task<TaxReceiptEmailSendResultDto> SendAsync(
            int taxReceiptId,
            SendTaxReceiptEmailDto dto,
            string? userName,
            CancellationToken cancellationToken = default,
            int? currentUserId = null,
            bool canAccessAllReceipts = false);
    }
}
