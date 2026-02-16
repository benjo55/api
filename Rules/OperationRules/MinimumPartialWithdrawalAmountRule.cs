using api.Models;

namespace api.Rules.OperationRules
{
    public class MinimumPartialWithdrawalAmountRule : IBusinessRule<Operation>
    {
        public string Name => nameof(MinimumPartialWithdrawalAmountRule);
        public string ErrorMessage => "Le montant minimum d’un rachat partiel est de 500 €.";

        public bool IsSatisfiedBy(Operation operation)
        {
            if (operation.Type == OperationType.PartialWithdrawal)
            {
                return (operation.Amount ?? 0m) >= 500m;
            }

            return true;
        }
    }
}
