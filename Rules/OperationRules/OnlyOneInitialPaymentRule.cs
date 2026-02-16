using api.Data;
using api.Models;

namespace api.Rules.OperationRules
{
    /// <summary>
    /// Empêche la création d’un second versement initial pour un même contrat.
    /// </summary>
    public class OnlyOneInitialPaymentRule : IBusinessRule<Operation>
    {
        private readonly ApplicationDBContext _context;

        public OnlyOneInitialPaymentRule(ApplicationDBContext context)
        {
            _context = context;
        }

        public string Name => "OnlyOneInitialPayment";

        public string ErrorMessage => "Un contrat ne peut avoir qu’un seul versement initial.";

        public bool IsSatisfiedBy(Operation operation)
        {
            // on ne s'applique que pour les versements initiaux
            if (operation.Type != OperationType.InitialPayment)
                return true;

            // Vérifier s’il existe déjà un versement initial pour ce contrat
            return !_context.Operations
                .Any(o => o.ContractId == operation.ContractId
                          && o.Type == OperationType.InitialPayment);
        }
    }
}
