using System.Security.Claims;
using api.Data;
using api.Interfaces;
using api.Models;
using api.Security;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public sealed class CurrentUserAccessService : ICurrentUserAccessService
    {
        private static readonly OperationType[] SelfCareOperationTypes =
        [
            OperationType.InitialPayment,
            OperationType.ScheduledPayment,
            OperationType.FreePayment,
            OperationType.PartialWithdrawal,
            OperationType.ScheduledWithdrawal,
            OperationType.TotalWithdrawal,
            OperationType.Arbitrage,
            OperationType.ScheduledArbitrage,
            OperationType.Advance,
            OperationType.AdvanceRepayment,
            OperationType.BeneficiaryChange
        ];

        private readonly ApplicationDBContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserAccessService(
            ApplicationDBContext db,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CurrentUserAccessScope> GetScopeAsync(CancellationToken cancellationToken = default)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return new CurrentUserAccessScope(false, false, null, null);
            }

            var userId = CurrentUserId(user);
            var linkedPersonId = userId.HasValue
                ? await _db.Persons
                    .AsNoTracking()
                    .Where(p => p.UserId == userId.Value)
                    .Select(p => (int?)p.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            return new CurrentUserAccessScope(
                true,
                IsBackOfficeUser(user),
                userId,
                linkedPersonId);
        }

        public async Task<bool> CanReadContractAsync(int contractId, CancellationToken cancellationToken = default)
        {
            var scope = await GetScopeAsync(cancellationToken);
            if (scope.IsBackOffice) return true;
            if (!scope.LinkedPersonId.HasValue) return false;

            return await _db.Contracts
                .AsNoTracking()
                .AnyAsync(
                    c => c.Id == contractId && c.PersonId == scope.LinkedPersonId.Value,
                    cancellationToken);
        }

        public async Task<bool> CanReadOperationAsync(int operationId, CancellationToken cancellationToken = default)
        {
            var scope = await GetScopeAsync(cancellationToken);
            if (scope.IsBackOffice) return true;
            if (!scope.LinkedPersonId.HasValue) return false;

            return await _db.Operations
                .AsNoTracking()
                .AnyAsync(
                    o => o.Id == operationId && o.Contract.PersonId == scope.LinkedPersonId.Value,
                    cancellationToken);
        }

        public async Task<bool> CanCreateOperationAsync(
            OperationType operationType,
            int contractId,
            CancellationToken cancellationToken = default)
        {
            var scope = await GetScopeAsync(cancellationToken);
            if (scope.IsBackOffice) return true;
            if (!scope.LinkedPersonId.HasValue) return false;
            if (!SelfCareOperationTypes.Contains(operationType)) return false;

            return await _db.Contracts
                .AsNoTracking()
                .AnyAsync(
                    c => c.Id == contractId && c.PersonId == scope.LinkedPersonId.Value,
                    cancellationToken);
        }

        public IQueryable<Contract> ScopeContracts(IQueryable<Contract> query, CurrentUserAccessScope scope)
        {
            if (scope.IsBackOffice) return query;
            if (!scope.LinkedPersonId.HasValue) return query.Where(c => false);

            return query.Where(c => c.PersonId == scope.LinkedPersonId.Value);
        }

        public IQueryable<Operation> ScopeOperations(IQueryable<Operation> query, CurrentUserAccessScope scope)
        {
            if (scope.IsBackOffice) return query;
            if (!scope.LinkedPersonId.HasValue) return query.Where(o => false);

            return query.Where(o => o.Contract.PersonId == scope.LinkedPersonId.Value);
        }

        private static int? CurrentUserId(ClaimsPrincipal user)
        {
            var rawUserId = user.FindFirst("userId")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(rawUserId, out var userId) ? userId : null;
        }

        private static bool IsBackOfficeUser(ClaimsPrincipal user)
        {
            return user.IsInRole(SystemRoles.LegacyAdmin)
                || user.IsInRole(SystemRoles.Administrator)
                || user.IsInRole(SystemRoles.SuperAdministrator)
                || user.IsInRole(SystemRoles.UserManager);
        }
    }
}
