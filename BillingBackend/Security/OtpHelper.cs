using System.Security.Cryptography;
using System.Text;

namespace BillingBackend.Security;

/// <summary>Helpers for one-time codes: CSPRNG generation + SHA-256 hash storage (never plaintext).</summary>
public static class OtpHelper
{
    public static string GenerateNumericCode(int digits = 6)
    {
        var max = (int)Math.Pow(10, digits);
        var min = (int)Math.Pow(10, digits - 1);
        return RandomNumberGenerator.GetInt32(min, max).ToString($"D{digits}");
    }

    public static string Hash(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"billcom-otp-v1:{code}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool Verify(string code, string? expectedHash)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(expectedHash))
            return false;
        var actual = Hash(code.Trim());
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actual),
            Encoding.UTF8.GetBytes(expectedHash.Trim().ToLowerInvariant()));
    }
}
