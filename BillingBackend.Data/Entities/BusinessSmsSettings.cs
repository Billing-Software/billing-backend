using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    /// <summary>
    /// Per-tenant SMS configuration for India DLT compliance with Exotel.
    /// Stores the business's approved 6-character Sender ID, DLT Principal Entity ID,
    /// and DLT Content Template ID for invoice dispatch.
    /// Exotel API credentials stay only on the BillCom parent server.
    /// </summary>
    public class BusinessSmsSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = "Exotel";

        /// <summary>
        /// 6-character approved DLT Sender ID / Header (e.g. SRILAX, KUMARE, ABCSTO).
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// Principal Entity (PE) ID approved on Indian telecom DLT portal (Jio, Airtel, Vil, PingConnect).
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string DltEntityId { get; set; } = string.Empty;

        /// <summary>
        /// Content Template ID approved on DLT portal for sending invoice / transaction notifications.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string InvoiceTemplateId { get; set; } = string.Empty;

        /// <summary>
        /// Optional template body preview / format (e.g. "Dear {#var#}, your invoice {#var#} from {#var#} for Rs.{#var#} is ready. View: {#var#}").
        /// </summary>
        [MaxLength(500)]
        public string? TemplateBody { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;
    }
}
