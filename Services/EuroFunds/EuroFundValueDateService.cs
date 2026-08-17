using api.Interfaces;
using api.Models;

namespace api.Services.EuroFunds
{
    public sealed class EuroFundValueDateService : IEuroFundValueDateService
    {
        public DateTime ComputeValueDate(DateTime operationDate, EuroFundConfiguration? settings)
        {
            var date = operationDate.Date;
            var rule = settings?.ValueDateRule ?? EuroFundValueDateRule.NextBusinessDay;

            return rule switch
            {
                EuroFundValueDateRule.SameDay => date,
                EuroFundValueDateRule.NextCalendarDay => date.AddDays(1),
                EuroFundValueDateRule.FixedDelay => date.AddDays(Math.Max(0, settings?.ValueDateDelayDays ?? 1)),
                EuroFundValueDateRule.NextBusinessDay => NextBusinessDay(date),
                _ => NextBusinessDay(date),
            };
        }

        private static DateTime NextBusinessDay(DateTime date) => AddBusinessDays(date, 1);

        private static DateTime AddBusinessDays(DateTime date, int days)
        {
            var cursor = date;
            var remaining = days;

            while (remaining > 0)
            {
                cursor = cursor.AddDays(1);
                if (cursor.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                remaining--;
            }

            return cursor;
        }
    }
}
