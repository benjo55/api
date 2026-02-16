using api.Models;
using api.Rules;
using System.Text.RegularExpressions;

namespace api.Rules.PersonRules
{
    public class EmailMustBeValidRule : IBusinessRule<Person>
    {
        public string Name => "EmailMustBeValid";
        public string ErrorMessage => "Les adresses email ne sont pas valides.";

        public bool IsSatisfiedBy(Person person)
        {
            return IsValidEmail(person.Email1, required: true) &&
                   IsValidEmail(person.Email2, required: false);
        }

        private bool IsValidEmail(string email, bool required)
        {
            if (string.IsNullOrWhiteSpace(email))
                return !required;

            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }
    }
}
