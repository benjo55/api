using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.FinancialSupport;
using api.Dtos.Yahoo;

public interface IYahooFinanceProvider
{
    Task<YahooETFDto> GetEtfDataAsync(string ticker);
    Task<CreateFinancialSupportRequestDto?> GetBasicInfoByIsinAsync(string isin);
}
