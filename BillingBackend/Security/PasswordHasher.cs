using System.Security.Cryptography;
using System.Text;

namespace BillingBackend.Security;

/// <summary>
/// Industrial PBKDF2-SHA512 password hasher with legacy HMAC-SHA512 compatibility.
/// New hashes: PBKDF2, 210_000 iterations, 32-byte salt, 64-byte subkey.
/// Stored across existing columns: PasswordSalt = salt, PasswordHash = subkey.
/// Legacy accounts (HMACSHA512 with random key as salt) are verified then upgraded on next login.
/// All comparisons are constant-time.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 32;
    private const int HashSize = 64;

    public static void CreateHash(string password, out byte[] hash, out byte[] salt)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        salt = RandomNumberGenerator.GetBytes(SaltSize);
        hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);
    }

    public static bool Verify(string password, byte[] storedHash, byte[] storedSalt)
    {
        if (string.IsNullOrEmpty(password) || storedHash is null || storedSalt is null)
            return false;
        if (storedHash.Length == 0 || storedSalt.Length == 0)
            return false;

        // New format: 64-byte PBKDF2 subkey + 32-byte salt.
        if (storedHash.Length == HashSize && storedSalt.Length == SaltSize)
        {
            var computed = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                storedSalt,
                Iterations,
                HashAlgorithmName.SHA512,
                HashSize);
            return CryptographicOperations.FixedTimeEquals(computed, storedHash);
        }

        // Legacy format: HMACSHA512(password) keyed with storedSalt (any length).
        // Keep for backward compatibility; caller should re-hash on success.
        try
        {
            using var hmac = new HMACSHA512(storedSalt);
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            if (computed.Length != storedHash.Length)
                return false;
            return CryptographicOperations.FixedTimeEquals(computed, storedHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>True when stored credentials are still legacy HMAC and should be upgraded.</summary>
    public static bool IsLegacyHash(byte[] storedHash, byte[] storedSalt)
        => !(storedHash.Length == HashSize && storedSalt.Length == SaltSize);
}
