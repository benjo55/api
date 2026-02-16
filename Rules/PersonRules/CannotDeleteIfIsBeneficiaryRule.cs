using api.Models;
using api.Rules;
using api.Interfaces;

namespace api.Rules.PersonRules
{
    public class CannotDeleteIfIsBeneficiaryRule : IBusinessRule<Person>
    {
        private readonly IPersonRepository _personRepository;

        public CannotDeleteIfIsBeneficiaryRule(IPersonRepository personRepository)
        {
            _personRepository = personRepository;
        }

        public string Name => "CannotDeleteIfIsBeneficiary";
        public string ErrorMessage => "Impossible de supprimer cette personne car elle est bénéficiaire d'au moins un contrat.";

        public bool IsSatisfiedBy(Person person)
        {
            return !_personRepository.IsPersonBeneficiary(person.Id).Result;
        }
    }
}
