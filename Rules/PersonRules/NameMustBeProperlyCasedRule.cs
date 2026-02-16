using api.Models;
using api.Rules;
using System.Globalization;
using System.Text;

namespace api.Rules.PersonRules
{
    public class NameMustBeProperlyCasedRule : IBusinessRule<Person>
    {
        public string Name => "NameMustBeProperlyCased";
        public string ErrorMessage => string.Empty; // pas d'erreur à afficher

        public bool IsSatisfiedBy(Person person)
        {
            person.FirstName = ToProperCase(person.FirstName);
            person.LastName = ToProperCase(person.LastName);
            return true;
        }

        private string ToProperCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            var lowerCaseParticles = new HashSet<string>(new[]
            {
                "de", "du", "des", "la", "le", "les",
                "van", "von", "den", "der",
                "di", "da", "della", "del",
                "dos", "das", "y",
                "l’", "l'", "d’", "d'"
            }, StringComparer.OrdinalIgnoreCase);

            var parts = input.ToLower()
                .Split(new[] { ' ', '-', '\'', '’' })
                .Select((part, i) =>
                    (i > 0 && lowerCaseParticles.Contains(part)) ? part : Capitalize(part)
                );

            return string.Join(" ", parts);
        }

        private string Capitalize(string s) =>
            string.IsNullOrWhiteSpace(s)
                ? s
                : char.ToUpper(s[0]) + s.Substring(1);
    }
}
