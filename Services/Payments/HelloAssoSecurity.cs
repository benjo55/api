using System.Security.Cryptography;
using System.Text;

namespace api.Services.Payments
{
    public static class HelloAssoSecurity
    {
        public static string ComputeSha256(string payload)
        {
            var bytes = Encoding.UTF8.GetBytes(payload);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        public static string ComputeHmacSha256Base64(string payload, string key)
        {
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(payloadBytes);
            return Convert.ToBase64String(hash);
        }

        public static bool ConstantTimeEquals(string expected, string actual)
        {
            var a = Encoding.UTF8.GetBytes(expected);
            var b = Encoding.UTF8.GetBytes(actual);
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
