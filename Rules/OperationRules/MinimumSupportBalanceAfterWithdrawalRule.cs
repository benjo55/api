using api.Data;
using api.Models;

namespace api.Rules.OperationRules
{
    public class MinimumSupportBalanceAfterWithdrawalRule : IBusinessRule<Operation>
    {
        private readonly ApplicationDBContext _context;

        public MinimumSupportBalanceAfterWithdrawalRule(ApplicationDBContext context)
        {
            _context = context;
        }

        public string Name => nameof(MinimumSupportBalanceAfterWithdrawalRule);
        public string ErrorMessage =>
            "Le solde minimum à laisser sur chaque support après un rachat partiel est de 50 €, sauf désinvestissement total.";

        public bool IsSatisfiedBy(Operation operation)
        {
            if (operation.Type != OperationType.PartialWithdrawal)
                return true;

            foreach (var alloc in operation.Allocations)
            {
                var support = _context.FinancialSupportAllocations
                    .FirstOrDefault(fsa => fsa.ContractId == operation.ContractId && fsa.SupportId == alloc.SupportId);

                if (support == null) continue;

                var remaining = (support.CurrentAmount - (alloc.Amount ?? 0m));

                // Si le support n’est pas totalement désinvesti, on vérifie le minimum restant
                if (remaining > 0 && remaining < 50m)
                    return false;
            }

            return true;
        }
    }
}

