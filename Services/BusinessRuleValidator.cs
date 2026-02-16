using api.Rules;

namespace api.Services
{
    public class BusinessRuleValidator
    {
        private readonly RuleFactory _ruleFactory;

        public BusinessRuleValidator(RuleFactory ruleFactory)
        {
            _ruleFactory = ruleFactory;
        }

        public void Validate<T>(T entity, bool aggregateErrors = false)
        {
            var rules = _ruleFactory.GetRulesFor<T>();
            var engine = new RuleEngine<T>(rules);

            engine.Enforce(entity, aggregateErrors);
        }
    }
}
