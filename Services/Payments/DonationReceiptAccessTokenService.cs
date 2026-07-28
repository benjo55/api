using System.Security.Cryptography;
using System.Text;

namespace api.Services.Payments
{
    public interface IDonationReceiptAccessTokenService
    {
        string Create(string publicDonationId, TimeSpan lifetime);
        bool Validate(string publicDonationId, string token);
    }

    public sealed class DonationReceiptAccessTokenService : IDonationReceiptAccessTokenService
    {
        private readonly string _secret;

        public DonationReceiptAccessTokenService(IConfiguration configuration)
        {
            _secret = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key manquant");
        }

        public string Create(string publicDonationId, TimeSpan lifetime)
        {
            var exp = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
            var payload = $"{publicDonationId}|{exp}";
            var payloadB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
            var signature = ComputeSignature(payloadB64);
            return $"{payloadB64}.{signature}";
        }

        public bool Validate(string publicDonationId, string token)
        {
            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            var payloadB64 = parts[0];
            var signature = parts[1];
            var expected = ComputeSignature(payloadB64);
            if (!HelloAssoSecurity.ConstantTimeEquals(expected, signature))
            {
                return false;
            }

            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(payloadB64));
            var payloadParts = payload.Split('|');
            if (payloadParts.Length != 2)
            {
                return false;
            }

            if (!string.Equals(payloadParts[0], publicDonationId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!long.TryParse(payloadParts[1], out var expUnix))
            {
                return false;
            }

            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= expUnix;
        }

        private string ComputeSignature(string payloadB64)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64));
            return Convert.ToBase64String(hash);
        }
    }
}
