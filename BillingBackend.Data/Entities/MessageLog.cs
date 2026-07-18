using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class MessageLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WhatsAppAccountId { get; set; }

        /// <summary>
        /// The bill this message relates to. Null for non-invoice messages.
        /// </summary>
        public int? BillId { get; set; }

        /// <summary>
        /// Recipient phone number in international format (e.g. "919876543210").
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string RecipientPhone { get; set; } = string.Empty;

        /// <summary>
        /// Message type: text, document, template.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string MessageType { get; set; } = "text";

        /// <summary>
        /// Message ID returned by WhatsApp Cloud API (wamid.*).
        /// </summary>
        [MaxLength(200)]
        public string? MetaMessageId { get; set; }

        /// <summary>
        /// Status: Queued, Sent, Delivered, Read, Failed.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Queued";

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeliveredAt { get; set; }

        public DateTime? ReadAt { get; set; }

        [MaxLength(500)]
        public string? FailedReason { get; set; }

        // Navigation
        [ForeignKey(nameof(WhatsAppAccountId))]
        public WhatsAppAccount WhatsAppAccount { get; set; } = null!;

        [ForeignKey(nameof(BillId))]
        public Bill? Bill { get; set; }
    }
}
