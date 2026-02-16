using api.Data;
using api.Models;

namespace api.Rules.OperationRules
{
    /// <summary>
    /// Empêche d’annuler le versement initial si d’autres opérations existent déjà sur le contrat.
    /// </summary>
    public class InitialPaymentCannotBeCancelledIfOtherOperationsExistRule : IBusinessRule<Operation>
    {
        private readonly ApplicationDBContext _context;

        public InitialPaymentCannotBeCancelledIfOtherOperationsExistRule(ApplicationDBContext context)
        {
            _context = context;
        }

        public string Name => nameof(InitialPaymentCannotBeCancelledIfOtherOperationsExistRule);

        public string ErrorMessage =>
            "Le versement initial ne peut pas être annulé s’il existe d’autres opérations sur le contrat.";

        public bool IsSatisfiedBy(Operation operation)
        {
            // On ne s'applique que si on tente d'annuler un versement initial
            if (operation.Type != OperationType.InitialPayment ||
                operation.Status != OperationStatus.Cancelled)
            {
                return true; // pas concerné
            }

            // Vérifier s'il existe d'autres opérations associées au même contrat
            var otherOpsExist = _context.Operations
                .Any(o => o.ContractId == operation.ContractId && o.Id != operation.Id);

            return !otherOpsExist; // si d'autres ops → règle NON satisfaite → blocage
        }
    }
}
