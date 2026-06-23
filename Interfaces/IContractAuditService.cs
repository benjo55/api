using api.Dtos.Contract;

namespace api.Interfaces
{
    public interface IContractAuditService
    {
        Task LogContractIntegrityAsync(int contractId);
        Task<ContractReconciliationDto> GetReconciliationAsync(int contractId);
    }
}
