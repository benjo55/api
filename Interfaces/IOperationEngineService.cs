// Interfaces/IOperationEngineService.cs
using System.Threading.Tasks;
using api.Models;

namespace api.Interfaces
{
    public interface IOperationEngineService
    {
        Task UpdateValuationsAsync();        // Mise à jour VL (EOD par défaut, Twelve fallback)
        Task ProcessPendingOperationsAsync();// Passage Pending → Executed
        Task ApplyRulesAsync(); // placeholder pour plus tard
        // Task ApplyExecutedOperationAsync(Operation op);
        Task ApplyManagementFeesAsync();
        Task RebuildContractAsync(int contractId);
        Task ApplyOperationAsync(Operation op);

    }
}
