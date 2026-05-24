using api.Models;

namespace api.Rules.OperationRules
{
    public class MinimumContractDepositRule : IBusinessRule<Operation>
    {
        public string Name => nameof(MinimumContractDepositRule);
        public string ErrorMessage => "Le montant minimum d’un versement sur le contrat est de 100 €.";

        public bool IsSatisfiedBy(Operation operation)
        {
            if (operation.Type is OperationType.InitialPayment or OperationType.FreePayment or OperationType.ScheduledPayment)
            {
                return (operation.Amount ?? 0m) >= 100m;
            }

            return true; // non applicable
        }
    }
}
