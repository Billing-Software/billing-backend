using System;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    public class BusinessSmsSettingsDto
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public string Provider { get; set; } = "Exotel";
        public string SenderId { get; set; } = string.Empty;
        public string DltEntityId { get; set; } = string.Empty;
        public string InvoiceTemplateId { get; set; } = string.Empty;
        public string? TemplateBody { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateSmsSettingsDto
    {
        /// <summary>
        /// 6-character approved DLT Sender ID / Header (e.g. SRILAX).
        /// </summary>
        [Required(ErrorMessage = "Sender ID is required.")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "Sender ID must be 3-10 characters.")]
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// Principal Entity ID registered with DLT telecom portal in India.
        /// </summary>
        [Required(ErrorMessage = "DLT Principal Entity ID is required.")]
        [MaxLength(50)]
        public string DltEntityId { get; set; } = string.Empty;

        /// <summary>
        /// Approved DLT Content Template ID for invoice SMS.
        /// </summary>
        [Required(ErrorMessage = "DLT Invoice Template ID is required.")]
        [MaxLength(50)]
        public string InvoiceTemplateId { get; set; } = string.Empty;

        /// <summary>
        /// Approved DLT template text with placeholders.
        /// Example: "Dear {#var#}, your invoice {#var#} from {#var#} for Rs.{#var#} is ready. View: {#var#}"
        /// </summary>
        [MaxLength(500)]
        public string? TemplateBody { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class SendTestSmsDto
    {
        [Required(ErrorMessage = "Phone number is required.")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Message { get; set; }
    }

    public class SendInvoiceSmsDto
    {
        [Required]
        public int BillId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;
    }

    public class SmsLogDto
    {
        public int Id { get; set; }
        public int? BillId { get; set; }
        public string RecipientPhone { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string MessageBody { get; set; } = string.Empty;
        public string? DltEntityId { get; set; }
        public string? DltTemplateId { get; set; }
        public string? ExotelSid { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class SmsResult
    {
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }
    }
}
