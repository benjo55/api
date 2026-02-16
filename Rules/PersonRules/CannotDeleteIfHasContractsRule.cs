using api.Models;
using api.Interfaces;

namespace api.Rules.PersonRules
{
    public class CannotDeleteIfHasContractsRule : IBusinessRule<Person>
    {
        private readonly IPersonRepository _personRepository;

        public CannotDeleteIfHasContractsRule(IPersonRepository personRepository)
        {
            _personRepository = personRepository;
        }

        public string Name => "CannotDeleteIfHasContracts";
        public string ErrorMessage => "Impossible de supprimer cette personne car elle est titulaire d’au moins un contrat.";

        public bool IsSatisfiedBy(Person person)
        {
            return !_personRepository.HasContracts(person.Id).Result;
        }
    }
}
