using api.Data;
using api.Models;
using api.Rules;

namespace api.Rules.ContractRules
{
    public class CannotDeleteContractWithOperationsRule : IBusinessRule<Contract>
    {
        private readonly ApplicationDBContext _context;

        public CannotDeleteContractWithOperationsRule(ApplicationDBContext context)
        {
            _context = context;
        }

        public string Name => "CannotDeleteContractWithOperations";
        public string ErrorMessage => "Impossible de supprimer ce contrat car au moins une opération existe sur celui-ci.";

        public bool IsSatisfiedBy(Contract contract)
        {
            var hasOperations = _context.Operations.Any(o => o.ContractId == contract.Id);
            return !hasOperations;
        }
    }
}
