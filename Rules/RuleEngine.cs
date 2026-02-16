using api.Exceptions;

namespace api.Rules
{
    public class RuleEngine<T>
    {
        private readonly IEnumerable<IBusinessRule<T>> _rules;

        public RuleEngine(IEnumerable<IBusinessRule<T>> rules)
        {
            _rules = rules;
        }

        public List<string> Validate(T entity)
        {
            return _rules
                .Where(rule => !rule.IsSatisfiedBy(entity))
                .Select(rule => rule.ErrorMessage)
                .ToList();
        }

        public bool IsValid(T entity) => !_rules.Any(rule => !rule.IsSatisfiedBy(entity));

        /// <summary>
        /// Valide les règles et lève une BusinessException si au moins une est violée.
        /// </summary>
        /// <param name="entity">Entité à valider</param>
        /// <param name="aggregateErrors">Si true, concatène toutes les erreurs dans l'exception</param>
        public void Enforce(T entity, bool aggregateErrors = false)
        {
            var errors = Validate(entity);
            if (errors.Any())
            {
                if (aggregateErrors)
                {
                    // ⚡ Toutes les erreurs dans un seul message
                    throw new BusinessException(string.Join("; ", errors));
                }
                else
                {
                    // ⚡ Seulement la première erreur
                    throw new BusinessException(errors.First());
                }
            }
        }
    }
}
