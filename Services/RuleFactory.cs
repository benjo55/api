using api.Data;
using api.Models;
using api.Rules;
using System.Reflection;

namespace api.Services
{
    public class RuleFactory
    {
        private readonly ApplicationDBContext _context;

        public RuleFactory(ApplicationDBContext context)
        {
            _context = context;
        }

        public IEnumerable<IBusinessRule<T>> GetRulesFor<T>()
        {
            var targetType = typeof(IBusinessRule<T>);

            // 🔍 Récupère toutes les classes de l’assembly courant qui implémentent IBusinessRule<T>
            var ruleTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t =>
                    !t.IsAbstract &&
                    !t.IsInterface &&
                    targetType.IsAssignableFrom(t))
                .ToList();

            // 🏭 Instanciation dynamique
            var instances = new List<IBusinessRule<T>>();
            foreach (var type in ruleTypes)
            {
                var instance = CreateInstance<T>(type);
                if (instance != null)
                    instances.Add(instance);
            }

            return instances;
        }

        private IBusinessRule<T>? CreateInstance<T>(Type ruleType)
        {
            // 1️⃣ Constructeur (ApplicationDBContext)
            var ctorWithContext = ruleType.GetConstructor(new[] { typeof(ApplicationDBContext) });
            if (ctorWithContext != null)
            {
                return (IBusinessRule<T>?)Activator.CreateInstance(ruleType, _context);
            }

            // 2️⃣ Constructeur vide
            var ctorEmpty = ruleType.GetConstructor(Type.EmptyTypes);
            if (ctorEmpty != null)
            {
                return (IBusinessRule<T>?)Activator.CreateInstance(ruleType);
            }

            // 3️⃣ Sinon, impossible d’instancier
            throw new InvalidOperationException(
                $"Impossible d’instancier la règle '{ruleType.Name}'. " +
                $"Aucun constructeur compatible trouvé."
            );
        }
    }
}
