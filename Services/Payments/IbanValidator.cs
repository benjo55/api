using System.Numerics;
using System.Text.RegularExpressions;
using api.Interfaces;

namespace api.Services.Payments
{
    public sealed partial class IbanValidator : IIbanValidator
    {
        public bool TryNormalizeIban(string value, out string normalizedIban)
        {
            normalizedIban = Regex.Replace(value ?? string.Empty, "\\s+", string.Empty).ToUpperInvariant();
            if (normalizedIban.Length < 15 || normalizedIban.Length > 34 || !AlphaNumericRegex().IsMatch(normalizedIban))
            {
                return false;
            }

            var rearranged = normalizedIban[4..] + normalizedIban[..4];
            var numeric = new System.Text.StringBuilder(rearranged.Length * 2);
            foreach (var c in rearranged)
            {
                if (char.IsDigit(c))
                {
                    numeric.Append(c);
                }
                else if (c is >= 'A' and <= 'Z')
                {
                    numeric.Append(c - 'A' + 10);
                }
                else
                {
                    return false;
                }
            }

            var remainder = BigInteger.Zero;
            foreach (var c in numeric.ToString())
            {
                remainder = (remainder * 10 + (c - '0')) % 97;
            }

            return remainder == 1;
        }

        public bool IsValidBic(string value)
        {
            var normalized = Regex.Replace(value ?? string.Empty, "\\s+", string.Empty).ToUpperInvariant();
            return BicRegex().IsMatch(normalized);
        }

        [GeneratedRegex("^[A-Z0-9]+$")]
        private static partial Regex AlphaNumericRegex();

        [GeneratedRegex("^[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?$")]
        private static partial Regex BicRegex();
    }
}
