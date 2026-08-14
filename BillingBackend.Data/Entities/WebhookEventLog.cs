using System;
using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Logs every incoming webhook event for deduplication, recovery, and debugging.
    /// Every webhook is persisted BEFORE processing to prevent data loss on failure.
    /// </summary>
    public class WebhookEventLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Source of the webhook: Razorpay, Meta, etc.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// External event ID from the webhook provider (used for deduplication).
        /// For Razorpay: the event.id field. For Meta: the message ID.
        /// </summary>
        [MaxLength(200)]
        public string? ExternalEventId { get; set; }

        /// <summary>
        /// The event type: subscription.charged, payment.failed, subscription.cancelled, etc.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Full raw JSON payload of the webhook for forensic analysis and replay.
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? RawPayload { get; set; }

        /// <summary>
        /// Processing status: Received, Processing, Completed, Failed
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string ProcessingStatus { get; set; } = "Received";

        /// <summary>
        /// If processing failed, the reason/exception message.
        /// </summary>
        [MaxLength(2000)]
        public string? FailureReason { get; set; }

        /// <summary>
        /// Number of times this event has been retried for processing.
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// When the event was received from the provider.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the event was successfully processed (null if still pending/failed).
        /// </summary>
        public DateTime? ProcessedAt { get; set; }
    }
}
