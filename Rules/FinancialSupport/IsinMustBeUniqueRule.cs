using api.Data;
using api.Models;
using api.Rules;

namespace api.Rules.FinancialSupportRules
{
    public class IsinMustBeUniqueRule : IBusinessRule<FinancialSupport>
    {
        private readonly ApplicationDBContext _context;

        public IsinMustBeUniqueRule(ApplicationDBContext context)
        {
            _context = context;
        }

        public string Name => "IsinMustBeUnique";

        public string ErrorMessage => "Un support avec cet ISIN existe déjà.";

        public bool IsSatisfiedBy(FinancialSupport support)
        {
            if (string.IsNullOrWhiteSpace(support.ISIN))
                return true;

            var normalized = support.ISIN.Trim().ToUpperInvariant();
            return !_context.FinancialSupports
                .Any(s => s.ISIN != null && s.ISIN.ToUpper() == normalized && s.Id != support.Id);
        }
    }
}
