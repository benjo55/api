using api.Data;
using api.Models;

namespace api.Rules.OperationRules
{
    public class MinimumContractBalanceAfterWithdrawalRule : IBusinessRule<Operation>
    {
        private readonly ApplicationDBContext _context;

        public MinimumContractBalanceAfterWithdrawalRule(ApplicationDBContext context)
        {
            _context = context;
        }

        public string Name => nameof(MinimumContractBalanceAfterWithdrawalRule);
        public string ErrorMessage => "Le solde minimum à laisser sur le contrat après un rachat partiel est de 1 000 €.";

        public bool IsSatisfiedBy(Operation operation)
        {
            if (operation.Type != OperationType.PartialWithdrawal)
                return true;

            var contract = _context.Contracts.FirstOrDefault(c => c.Id == operation.ContractId);
            if (contract == null) return true;

            var remaining = contract.CurrentValue - (operation.Amount ?? 0m);

            return remaining >= 1000m;
        }
    }
}
