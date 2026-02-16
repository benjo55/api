using api.Models;

namespace api.Rules.OperationRules
{
    public class MinimumSupportDepositRule : IBusinessRule<Operation>
    {
        public string Name => nameof(MinimumSupportDepositRule);
        public string ErrorMessage => "Le montant minimum d’un versement sur chaque support est de 50 €.";

        public bool IsSatisfiedBy(Operation operation)
        {
            if (operation.Allocations == null || operation.Allocations.Count == 0)
                return true;

            if (operation.Type is OperationType.InitialPayment or OperationType.FreePayment or OperationType.ScheduledPayment)
            {
                return operation.Allocations.All(a => (a.Amount ?? 0m) >= 50m);
            }

            return true;
        }
    }
}
