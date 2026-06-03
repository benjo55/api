using api.Data;
using api.Models;
using api.Rules;

namespace api.Rules.ContractRules
{
    public class CannotDeleteLockedContractRule : IBusinessRule<Contract>
    {
        private readonly ApplicationDBContext _context;

        public CannotDeleteLockedContractRule(ApplicationDBContext context)
        {
            _context = context;
        }

        public string Name => "CannotDeleteLockedContract";
        public string ErrorMessage => "Impossible de supprimer ce contrat car il est verrouillé.";

        public bool IsSatisfiedBy(Contract contract)
        {
            var isLocked = _context.Contracts
                .Where(c => c.Id == contract.Id)
                .Select(c => c.Locked)
                .FirstOrDefault();

            return !isLocked;
        }
    }
}
