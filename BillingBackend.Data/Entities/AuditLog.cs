using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Immutable audit trail for tracking all critical business operations.
    /// Records WHO did WHAT, WHEN, and from WHERE for accountability and dispute resolution.
    /// </summary>
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public int? BusinessId { get; set; }

        /// <summary>
        /// The type of entity affected: Bill, Payment, Customer, Subscription, etc.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The primary key of the affected entity.
        /// </summary>
        public int? EntityId { get; set; }

        /// <summary>
        /// The action performed: Created, Updated, Deleted, StatusChanged, PaymentCaptured, PaymentFailed, etc.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// JSON snapshot of old values before the change (null for Create actions).
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// JSON snapshot of new values after the change (null for Delete actions).
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// User ID or Staff ID who performed the action.
        /// </summary>
        [MaxLength(100)]
        public string? PerformedBy { get; set; }

        /// <summary>
        /// IP address of the request origin.
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Browser user agent string for forensic analysis.
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Correlation ID to link related operations across services.
        /// </summary>
        [MaxLength(100)]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Additional context or notes about the operation.
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
