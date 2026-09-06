using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Audit log of every SMS sent through Exotel.
    /// Tracks recipient, sender header, DLT IDs, and delivery status.
    /// </summary>
    public class SmsLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        public int? BillId { get; set; }

        [Required]
        [MaxLength(20)]
        public string RecipientPhone { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string MessageBody { get; set; } = string.Empty;

        [MaxLength(50)]
        public string DltEntityId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string DltTemplateId { get; set; } = string.Empty;

        /// <summary>
        /// Exotel Message SID returned by Exotel API.
        /// </summary>
        [MaxLength(100)]
        public string? ExotelSid { get; set; }

        /// <summary>
        /// Status: Sent, Delivered, Failed.
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Sent";

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;

        [ForeignKey(nameof(BillId))]
        public Bill? Bill { get; set; }
    }
}
