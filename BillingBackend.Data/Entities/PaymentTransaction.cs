using System;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.Data.Entities
{
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        public int? BusinessId { get; set; }

        [MaxLength(100)]
        public string? RazorpayPaymentId { get; set; }

        [MaxLength(100)]
        public string? RazorpayOrderId { get; set; }

        [MaxLength(100)]
        public string? RazorpaySubscriptionId { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = string.Empty; // Captured, Failed, Refunded

        [MaxLength(50)]
        public string? PaymentMethod { get; set; } // upi, card, netbanking

        public string? RawWebhookPayload { get; set; }

        /// <summary>
        /// Reason for payment failure (user_cancelled, signature_invalid, gateway_error, etc.)
        /// </summary>
        [MaxLength(500)]
        public string? FailureReason { get; set; }

        /// <summary>
        /// Number of retry attempts for this transaction.
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Links to the WebhookEventLog that triggered this transaction update.
        /// </summary>
        public int? WebhookEventId { get; set; }

        /// <summary>
        /// Correlation ID for tracing this transaction across services and logs.
        /// </summary>
        [MaxLength(100)]
        public string? CorrelationId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
