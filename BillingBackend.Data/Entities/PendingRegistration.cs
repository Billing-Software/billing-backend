using System;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.Data.Entities
{
    public class PendingRegistration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Token { get; set; } = string.Empty; // Unique GUID string

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        [Required]
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        [Required]
        [MaxLength(200)]
        public string LegalName { get; set; } = string.Empty; // Business Legal Name

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? GstIn { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public int SelectedPlanId { get; set; }

        [MaxLength(100)]
        public string? RazorpayCustomerId { get; set; }

        [MaxLength(100)]
        public string? RazorpaySubscriptionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "PendingPayment"; // PendingPayment, Failed, Completed, Expired

        public bool ReminderEmailSent { get; set; } = false;

        public DateTime? ReminderEmailSentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

        public string? RawRegistrationData { get; set; }
    }
}
