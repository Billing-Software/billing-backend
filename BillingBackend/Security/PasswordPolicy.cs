using System.Text.RegularExpressions;

namespace BillingBackend.Security;

/// <summary>Central password + OTP policy. Single source of truth for all registration/reset flows.</summary>
public static partial class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 100;

    // Require upper, lower, digit. Special char recommended but not mandatory to avoid UX lockout;
    // entropy is enforced primarily via length + PBKDF2.
    [GeneratedRegex(@"[A-Z]")]
    private static partial Regex Upper();
    [GeneratedRegex(@"[a-z]")]
    private static partial Regex Lower();
    [GeneratedRegex(@"\d")]
    private static partial Regex Digit();

    public static (bool Ok, string? Error) Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password is required.");
        if (password.Length < MinLength)
            return (false, $"Password must be at least {MinLength} characters.");
        if (password.Length > MaxLength)
            return (false, $"Password must be at most {MaxLength} characters.");
        if (!Upper().IsMatch(password) || !Lower().IsMatch(password) || !Digit().IsMatch(password))
            return (false, "Password must contain an uppercase letter, a lowercase letter, and a digit.");
        if (IsCommonPassword(password))
            return (false, "Password is too common. Choose a stronger password.");
        return (true, null);
    }

    private static bool IsCommonPassword(string password)
    {
        // Small denylist for the most abused passwords (case-insensitive).
        return password.Equals("123456", StringComparison.Ordinal)
            || password.Equals("12345678", StringComparison.Ordinal)
            || password.Equals("password", StringComparison.OrdinalIgnoreCase)
            || password.Equals("qwerty", StringComparison.OrdinalIgnoreCase)
            || password.Equals("billcom123", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Roles a self-service caller may request. Everything else is server-assigned.</summary>
    public static readonly HashSet<string> SelfServiceRoles = new(StringComparer.OrdinalIgnoreCase) { "Owner" };

    /// <summary>Roles assignable to staff members by a business owner. Never Owner/SuperAdmin.</summary>
    public static readonly HashSet<string> StaffRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Staff", "Manager", "Cashier"
    };
}
