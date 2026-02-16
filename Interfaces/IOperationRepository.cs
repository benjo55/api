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
    Task<IEnumerable<Operation>> GetByCompartmentAsync(int contractId, int compartmentId);

    // ==========================================================
    // 🔹 Écriture
    // ==========================================================
    Task<Operation> AddAsync(Operation operation);
    Task<Operation> UpdateAsync(Operation operation);
    Task DeleteAsync(int id);

    /// <summary>
    /// (Optionnel) Redistribue un retrait global entre compartiments.
    /// Conservée pour un usage futur.
    /// </summary>
    Task<IEnumerable<Operation>> GetByContractIdAsync(int contractId);
}
