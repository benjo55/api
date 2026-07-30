using api.Models;

namespace api.Interfaces
{
    public sealed record CurrentUserAccessScope(
        bool IsAuthenticated,
        bool IsBackOffice,
        int? UserId,
        int? LinkedPersonId);

    public interface ICurrentUserAccessService
    {
        Task<CurrentUserAccessScope> GetScopeAsync(CancellationToken cancellationToken = default);
        Task<bool> CanReadContractAsync(int contractId, CancellationToken cancellationToken = default);
        Task<bool> CanReadOperationAsync(int operationId, CancellationToken cancellationToken = default);
        Task<bool> CanCreateOperationAsync(OperationType operationType, int contractId, CancellationToken cancellationToken = default);
        IQueryable<Contract> ScopeContracts(IQueryable<Contract> query, CurrentUserAccessScope scope);
        IQueryable<Operation> ScopeOperations(IQueryable<Operation> query, CurrentUserAccessScope scope);
    }
}
