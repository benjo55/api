using System.Security.Cryptography;
using System.Text;
using api.Configuration;
using api.Interfaces;
using Microsoft.Extensions.Options;

namespace api.Services.Payments
{
    public sealed class BankAccountProtector : IBankAccountProtector
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private readonly byte[] _key;

        public BankAccountProtector(IOptions<PaymentsOptions> options)
        {
            var configuredKey = options.Value.BankEncryptionKey;
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                _key = SHA256.HashData(Encoding.UTF8.GetBytes("life-local-bank-encryption-placeholder"));
                return;
            }

            try
            {
                var decoded = Convert.FromBase64String(configuredKey);
                _key = decoded.Length >= 32 ? decoded[..32] : SHA256.HashData(decoded);
            }
            catch (FormatException)
            {
                _key = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
            }
        }

        public string Protect(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var plaintext = Encoding.UTF8.GetBytes(value);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            return string.Join(
                ".",
                "v1",
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(ciphertext),
                Convert.ToBase64String(tag));
        }

        public string Unprotect(string protectedValue)
        {
            if (string.IsNullOrWhiteSpace(protectedValue))
            {
                return string.Empty;
            }

            var parts = protectedValue.Split('.');
            if (parts.Length != 4 || parts[0] != "v1")
            {
                throw new InvalidOperationException("Coordonnees bancaires protegees invalides.");
            }

            var nonce = Convert.FromBase64String(parts[1]);
            var ciphertext = Convert.FromBase64String(parts[2]);
            var tag = Convert.FromBase64String(parts[3]);
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
    }
}
