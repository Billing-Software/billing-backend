namespace BillingBackend.Services
{
    /// <summary>
    /// AES-256 encryption/decryption for sensitive tokens before database storage.
    /// </summary>
    public interface ITokenEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
}
