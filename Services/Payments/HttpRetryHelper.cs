using System.Net;
using System.Net.Http.Headers;

namespace api.Services.Payments
{
    internal static class HttpRetryHelper
    {
        public static bool IsTransient(HttpStatusCode statusCode)
        {
            var code = (int)statusCode;
            return code == 408 || code == 429 || code >= 500;
        }

        public static async Task DelayWithBackoffAsync(int attempt, RetryConditionHeaderValue? retryAfter, CancellationToken cancellationToken)
        {
            if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
            {
                await Task.Delay(delta, cancellationToken);
                return;
            }

            var baseDelayMs = Math.Min(3000, 200 * Math.Pow(2, attempt - 1));
            var jitterMs = Random.Shared.Next(30, 180);
            await Task.Delay(TimeSpan.FromMilliseconds(baseDelayMs + jitterMs), cancellationToken);
        }
    }
}
