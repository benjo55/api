namespace api.Services.PersonalDashboard
{
    internal static class ExternalUrlGuard
    {
        public static string? SafeHttpUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
                && uri.Scheme is "https" or "http"
                ? uri.ToString()
                : null;
        }
    }
}
