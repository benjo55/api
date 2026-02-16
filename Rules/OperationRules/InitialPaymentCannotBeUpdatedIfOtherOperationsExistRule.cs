using api.Data;
using api.Models;

namespace api.Rules.OperationRules
{
    /// <summary>
    /// Empêche la modification du versement initial si d’autres opérations existent sur le contrat.
    /// </summary>
    public class InitialPaymentCannotBeUpdatedIfOtherOperationsExistRule : IBusinessRule<Operation>
    {
        private readonly ApplicationDBContext _context;

        public InitialPaymentCannotBeUpdatedIfOtherOperationsExistRule(ApplicationDBContext context)
        {
            _context = context;
        }

        public string Name => nameof(InitialPaymentCannotBeUpdatedIfOtherOperationsExistRule);

        public string ErrorMessage =>
            "Le versement initial ne peut pas être modifié s’il existe d’autres opérations sur le contrat.";

        public bool IsSatisfiedBy(Operation operation)
        {
            // Ne concerne que le versement initial
            if (operation.Type != OperationType.InitialPayment)
                return true;

            // Si le versement initial est la seule opération → modification autorisée
            var otherOpsExist = _context.Operations
                .Any(o => o.ContractId == operation.ContractId && o.Id != operation.Id);

            return !otherOpsExist;
        }
    }
}
