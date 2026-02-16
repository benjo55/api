using api.Interfaces;
using api.Rules;

public class ValidationService<T> : IValidationService<T>
{
    private readonly IEnumerable<IBusinessRule<T>> _rules;

    public ValidationService(IEnumerable<IBusinessRule<T>> rules)
    {
        _rules = rules;
    }

    public List<string> Validate(T entity)
    {
        var engine = new RuleEngine<T>(_rules);
        return engine.Validate(entity);
    }

    public bool IsValid(T entity)
    {
        var engine = new RuleEngine<T>(_rules);
        return engine.IsValid(entity);
    }
}
