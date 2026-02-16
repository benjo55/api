using api.Data;
using api.Models;

namespace api.Rules.OperationRules
{
    /// <summary>
    /// Empêche la suppression du versement initial si d’autres opérations existent.
    /// </summary>
    public class InitialPaymentCannotBeDeletedIfOtherOperationsExistRule : IBusinessRule<Operation>
    {
        private readonly ApplicationDBContext _context;

        public InitialPaymentCannotBeDeletedIfOtherOperationsExistRule(ApplicationDBContext context)
        {
            _context = context;
        }

        public string Name => nameof(InitialPaymentCannotBeDeletedIfOtherOperationsExistRule);

        public string ErrorMessage =>
            "Le versement initial ne peut pas être supprimé s’il existe d’autres opérations sur le contrat.";

        public bool IsSatisfiedBy(Operation operation)
        {
            // Règle concernée uniquement par la suppression d'un versement initial
            if (operation.Type != OperationType.InitialPayment)
                return true;

            // S'il existe d'autres opérations que celle-ci → suppression impossible
            var otherOpsExist = _context.Operations
                .Any(o => o.ContractId == operation.ContractId && o.Id != operation.Id);

            return !otherOpsExist;
        }
    }
}
