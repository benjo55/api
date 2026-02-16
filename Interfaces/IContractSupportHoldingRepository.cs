using System.Threading.Tasks;
using api.Models;

namespace api.Interfaces
{
    public interface IContractSupportHoldingRepository
    {
        Task<ContractSupportHolding?> GetAsync(int contractId, int supportId);
    }
}
