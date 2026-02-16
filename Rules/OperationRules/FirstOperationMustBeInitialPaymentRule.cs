using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Rules.OperationRules
{
    public class FirstOperationMustBeInitialPaymentRule : IBusinessRule<Operation>
    {
        private readonly ApplicationDBContext _context;

        public FirstOperationMustBeInitialPaymentRule(ApplicationDBContext context)
        {
            _context = context;
        }

        public string Name => nameof(FirstOperationMustBeInitialPaymentRule);

        public string ErrorMessage =>
            "La première opération d’un contrat doit obligatoirement être un versement initial.";

        public bool IsSatisfiedBy(Operation operation)
        {
            // ⚠️ Comme l’interface impose du synchrone, on doit forcer en synchrone.
            var hasExistingOps = _context.Operations
                .Any(o => o.ContractId == operation.ContractId);

            // Si aucune opération, alors la première doit être InitialPayment
            if (!hasExistingOps && operation.Type != OperationType.InitialPayment)
            {
                return false; // règle non satisfaite
            }

            return true;
        }
    }
}
