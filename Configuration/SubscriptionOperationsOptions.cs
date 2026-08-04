using System.ComponentModel.DataAnnotations;

namespace api.Configuration
{
    public sealed class SubscriptionOperationsOptions
    {
        public const string SectionName = "SubscriptionOperations";

        public MfaOptions Mfa { get; set; } = new();
        public SubscriptionPaymentOptions Payment { get; set; } = new();
        public SubscriptionSignatureOptions Signature { get; set; } = new();
    }

    public sealed class MfaOptions
    {
        public string Provider { get; set; } = "LocalOtp";
        public string PreferredChannel { get; set; } = "Email";
        public TimeSpan ChallengeLifetime { get; set; } = TimeSpan.FromMinutes(10);
        public bool EmailFallbackEnabled { get; set; } = true;
        public TotpOptions Totp { get; set; } = new();
        public GenericHttpSmsOptions Sms { get; set; } = new();
        public TwilioVerifyOptions TwilioVerify { get; set; } = new();
    }

    public sealed class TotpOptions
    {
        public bool Enabled { get; set; } = true;
        public string Issuer { get; set; } = "Financial Life";
        public int Digits { get; set; } = 6;
        public int PeriodSeconds { get; set; } = 30;
        public int AllowedClockDriftSteps { get; set; } = 1;
        public string SecretEncryptionKey { get; set; } = string.Empty;
    }

    public sealed class GenericHttpSmsOptions
    {
        public bool Enabled { get; set; }

        [Url]
        public string EndpointUrl { get; set; } = string.Empty;

        public string BearerToken { get; set; } = string.Empty;
        public string BasicUsername { get; set; } = string.Empty;
        public string BasicPassword { get; set; } = string.Empty;
        public string JsonBodyTemplate { get; set; } = """{"to":"{to}","message":"{message}"}""";
    }

    public sealed class TwilioVerifyOptions
    {
        public bool Enabled { get; set; }
        public string BaseUrl { get; set; } = "https://verify.twilio.com/v2/";
        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string ServiceSid { get; set; } = string.Empty;
        public string Channel { get; set; } = "sms";
    }

    public sealed class SubscriptionPaymentOptions
    {
        public string Provider { get; set; } = "ManualSepa";
        public bool ExecutionEnabled { get; set; }
    }

    public sealed class SubscriptionSignatureOptions
    {
        public string Provider { get; set; } = "InternalPendingProvider";
        public bool ExecutionEnabled { get; set; }
        public DocuSealSignatureOptions DocuSeal { get; set; } = new();
        public YoutrustSignatureOptions Youtrust { get; set; } = new();
    }

    public sealed class DocuSealSignatureOptions
    {
        public bool Enabled { get; set; }
        public string BaseUrl { get; set; } = "https://api.docuseal.com";
        public string ApiKey { get; set; } = string.Empty;
        public bool SendEmail { get; set; } = true;
        public bool SendSms { get; set; }
        public bool RequirePhone2Fa { get; set; } = true;
        public string SignerRole { get; set; } = "Souscripteur";
    }

    public sealed class YoutrustSignatureOptions
    {
        public bool Enabled { get; set; }
        public string BaseUrl { get; set; } = "https://api-sandbox.yousign.app/v3";
        public string ApiKey { get; set; } = string.Empty;
        public string DeliveryMode { get; set; } = "email";
        public string Locale { get; set; } = "fr";
        public string SignatureLevel { get; set; } = "electronic_signature";
        public string AuthenticationMode { get; set; } = "otp_sms";
        public bool AutoActivate { get; set; } = true;
        public int SignaturePage { get; set; } = 1;
        public int SignatureX { get; set; } = 200;
        public int SignatureY { get; set; } = 700;
        public int SignatureWidth { get; set; } = 160;
        public int SignatureHeight { get; set; } = 60;
    }
}
