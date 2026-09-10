using System;
using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "varbinary(max)")]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        [Required]
        [Column(TypeName = "varbinary(max)")]
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Owner";

        [MaxLength(100)]
        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpiry { get; set; }

        // Brute-force / OTP abuse guards (added for industrial hardening; nullable-safe defaults).
        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEnd { get; set; }

        public int PasswordResetAttemptCount { get; set; } = 0;

        public DateTime? PasswordResetRequestedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation: 1:1 with Business (this user owns a business)
        public Business? Business { get; set; }
    }
}
