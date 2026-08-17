namespace api.Configuration
{
    public sealed class MailSettings
    {
        public string Provider { get; set; } = "Brevo";
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string FromAddress { get; set; } = "no-reply@life.local";
        public string FromEmail
        {
            get => FromAddress;
            set => FromAddress = value;
        }
        public string FromName { get; set; } = "Life";
    }
}
