namespace api.Services.Payments
{
    public static class HelloAssoAmountConverter
    {
        public static int EuroToCents(decimal amount)
        {
            var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            if (rounded != amount)
            {
                throw new ArgumentException("Le montant ne peut pas contenir plus de deux decimales.", nameof(amount));
            }

            return checked((int)(rounded * 100m));
        }

        public static decimal CentsToEuro(int amountInCents)
        {
            return amountInCents / 100m;
        }
    }
}
