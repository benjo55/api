using api.Dtos.Generic;
using api.Helpers;
using api.Models;

public interface IOperationRepository
{
    // ==========================================================
    // 🔹 Lecture
    // ==========================================================
    Task<PagedResult<Operation>> GetAllAsync(QueryObject query);
    Task<Operation?> GetByIdAsync(int id);
    Task<IEnumerable<Operation>> GetByContractAsync(int contractId);

    // ==========================================================
    // 🔹 Écriture
    // ==========================================================
    Task<Operation> AddAsync(Operation operation);
    Task<Operation> UpdateAsync(Operation operation);
    Task DeleteAsync(int id);
    Task<Operation?> SuspendScheduleAsync(int operationId);
    Task<Operation?> ResumeScheduleAsync(int operationId);
    Task<Operation?> StopScheduleAsync(int operationId);

    /// <summary>
    /// (Optionnel) Redistribue un retrait global entre poches.
    /// Conservée pour un usage futur.
    /// </summary>
    Task<IEnumerable<Operation>> GetByContractIdAsync(int contractId);
}
