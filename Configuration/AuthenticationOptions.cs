namespace api.Configuration
{
    public sealed class AuthenticationOptions
    {
        public string FrontendBaseUrl { get; init; } = "http://localhost:5173";

        public TimeSpan EmailConfirmationTokenLifetime { get; init; } = TimeSpan.FromHours(24);

        public TimeSpan PasswordResetTokenLifetime { get; init; } = TimeSpan.FromMinutes(30);

        public TimeSpan MinimumEmailResendInterval { get; init; } = TimeSpan.FromMinutes(2);

        public int PasswordMinLength { get; init; } = 10;

        public string[] DuplicateTestEmailAliases { get; init; } =
        [
            "p_benhamou@hotmail.com"
        ];
    }
}
