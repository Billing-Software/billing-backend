using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace BillingBackend.Services
{
    /// <summary>
    /// AES-256-CBC encryption for Meta access tokens.
    /// Key is read from configuration: Meta:TokenEncryptionKey (base64-encoded 32-byte key).
    /// </summary>
    public class TokenEncryptionService : ITokenEncryptionService
    {
        private readonly byte[] _key;

        public TokenEncryptionService(IConfiguration configuration)
        {
            var keyBase64 = configuration["Meta:TokenEncryptionKey"];
            if (string.IsNullOrEmpty(keyBase64))
            {
                // Generate a deterministic key from AppSecret if no dedicated key is set
                var appSecret = configuration["Meta:AppSecret"] ?? "default-dev-secret-do-not-use";
                using var sha256 = SHA256.Create();
                _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(appSecret));
            }
            else
            {
                _key = Convert.FromBase64String(keyBase64);
            }

            if (_key.Length != 32)
                throw new ArgumentException("TokenEncryptionKey must be exactly 32 bytes (256 bits).");
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV to cipher text for storage
            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extract IV from the first 16 bytes
            var iv = new byte[16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            var cipherBytes = new byte[fullCipher.Length - iv.Length];
            Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
