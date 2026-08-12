using api.Dtos.EuroFund;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Interfaces
{
    public interface IEuroFundLotService
    {
        Task ApplyOperationAsync(Operation operation, DbContext context, CancellationToken cancellationToken = default);
    }

    public interface IEuroFundValuationService
    {
        Task<EuroFundValuationDto> GetValuationAsync(
            int contractId,
            int financialSupportId,
            DateTime valuationDate,
            CancellationToken cancellationToken = default);
    }

    public interface IEuroFundRevaluationService
    {
        Task<List<EuroFundSummaryDto>> GetEuroFundsAsync(CancellationToken cancellationToken = default);
        Task<EuroFundSummaryDto?> GetEuroFundAsync(int financialSupportId, CancellationToken cancellationToken = default);
        Task<EuroFundConfigurationDto> UpsertConfigurationAsync(int financialSupportId, EuroFundConfigurationDto dto, CancellationToken cancellationToken = default);
        Task<List<EuroFundFinancialYearDto>> GetFinancialYearsAsync(int financialSupportId, CancellationToken cancellationToken = default);
        Task<EuroFundFinancialYearDto> UpsertFinancialYearAsync(int financialSupportId, int year, EuroFundFinancialYearDto dto, CancellationToken cancellationToken = default);
        Task<ReferenceRateDto> AddReferenceRateAsync(ReferenceRateDto dto, CancellationToken cancellationToken = default);
        Task<EuroFundPreviewDto> PreviewAsync(int financialSupportId, int year, DateTime? asOf = null, CancellationToken cancellationToken = default);
        Task<EuroFundPreviewDto> FinalizeAsync(int financialSupportId, int year, CancellationToken cancellationToken = default);
    }
}
