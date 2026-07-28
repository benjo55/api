using api.Interfaces;

namespace api.Services.TaxReceipts
{
    public sealed class AmountToWordsService : IAmountToWordsService
    {
        public string ToFrenchEuros(decimal amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Le montant doit etre positif.");
            }

            var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            var euros = (long)Math.Truncate(rounded);
            var cents = (int)((rounded - euros) * 100);

            var result = $"{NumberToWords(euros)} {(euros > 1 ? "EUROS" : "EURO")}";
            if (cents > 0)
            {
                result += $" ET {NumberToWords(cents)} {(cents > 1 ? "CENTIMES" : "CENTIME")}";
            }

            return result;
        }

        private static string NumberToWords(long number)
        {
            if (number == 0)
            {
                return "ZERO";
            }

            return Words(number).Trim().ToUpperInvariant();
        }

        private static string Words(long number)
        {
            if (number < 17)
            {
                return new[]
                {
                    "zero", "un", "deux", "trois", "quatre", "cinq", "six", "sept",
                    "huit", "neuf", "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize"
                }[number];
            }

            if (number < 20)
            {
                return "dix-" + Words(number - 10);
            }

            if (number < 70)
            {
                var tens = new Dictionary<long, string>
                {
                    [20] = "vingt",
                    [30] = "trente",
                    [40] = "quarante",
                    [50] = "cinquante",
                    [60] = "soixante"
                };
                var ten = number / 10 * 10;
                var unit = number % 10;
                if (unit == 0)
                {
                    return tens[ten];
                }

                return unit == 1 ? $"{tens[ten]} et un" : $"{tens[ten]}-{Words(unit)}";
            }

            if (number < 80)
            {
                return number == 71 ? "soixante et onze" : "soixante-" + Words(number - 60);
            }

            if (number < 100)
            {
                if (number == 80)
                {
                    return "quatre-vingts";
                }

                return "quatre-vingt-" + Words(number - 80);
            }

            if (number < 1000)
            {
                var hundreds = number / 100;
                var rest = number % 100;
                var prefix = hundreds == 1 ? "cent" : $"{Words(hundreds)} cent";
                if (rest == 0)
                {
                    return hundreds > 1 ? prefix + "s" : prefix;
                }

                return $"{prefix} {Words(rest)}";
            }

            if (number < 1_000_000)
            {
                var thousands = number / 1000;
                var rest = number % 1000;
                var prefix = thousands == 1 ? "mille" : $"{Words(thousands)} mille";
                return rest == 0 ? prefix : $"{prefix} {Words(rest)}";
            }

            var millions = number / 1_000_000;
            var remainder = number % 1_000_000;
            var millionText = millions == 1 ? "un million" : $"{Words(millions)} millions";
            return remainder == 0 ? millionText : $"{millionText} {Words(remainder)}";
        }
    }
}
