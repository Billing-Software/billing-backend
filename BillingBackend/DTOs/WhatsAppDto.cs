using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs
{
    // ===== Connection DTOs =====

    /// <summary>
    /// Received from Meta Embedded Signup — the authorization code to exchange for a token.
    /// </summary>
    public class WhatsAppConnectCallbackDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// Returned after successful connection or status query.
    /// </summary>
    public class WhatsAppAccountDto
    {
        public int Id { get; set; }
        public string? DisplayPhoneNumber { get; set; }
        public string? WabaId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public DateTime? DisconnectedAt { get; set; }
    }

    // ===== Messaging DTOs =====

    public class SendTextMessageDto
    {
        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [MaxLength(4096)]
        public string Message { get; set; } = string.Empty;
    }

    public class SendDocumentDto
    {
        [Required]
        public int BillId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Caption { get; set; }
    }

    public class SendTemplateDto
    {
        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// Template parameter values keyed by parameter name.
        /// </summary>
        public Dictionary<string, string>? Parameters { get; set; }
    }

    // ===== Message Log DTO =====

    public class MessageLogDto
    {
        public int Id { get; set; }
        public int? BillId { get; set; }
        public string RecipientPhone { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string? MetaMessageId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? FailedReason { get; set; }
    }

    // ===== Meta Webhook Payload DTOs =====
    // These model the JSON structure that Meta POSTs to our webhook endpoint.

    public class MetaWebhookPayload
    {
        public string Object { get; set; } = string.Empty;
        public List<MetaWebhookEntry> Entry { get; set; } = new();
    }

    public class MetaWebhookEntry
    {
        public string Id { get; set; } = string.Empty;
        public List<MetaWebhookChange> Changes { get; set; } = new();
    }

    public class MetaWebhookChange
    {
        public MetaWebhookValue Value { get; set; } = new();
        public string Field { get; set; } = string.Empty;
    }

    public class MetaWebhookValue
    {
        public string? MessagingProduct { get; set; }
        public MetaWebhookMetadata? Metadata { get; set; }
        public List<MetaWebhookStatus>? Statuses { get; set; }
        public List<MetaWebhookMessage>? Messages { get; set; }
    }

    public class MetaWebhookMetadata
    {
        public string? DisplayPhoneNumber { get; set; }
        public string? PhoneNumberId { get; set; }
    }

    public class MetaWebhookStatus
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public string? Timestamp { get; set; }
        public string? RecipientId { get; set; }
        public MetaWebhookError[]? Errors { get; set; }
    }

    public class MetaWebhookMessage
    {
        public string? From { get; set; }
        public string? Id { get; set; }
        public string? Timestamp { get; set; }
        public string? Type { get; set; }
        public MetaWebhookTextBody? Text { get; set; }
    }

    public class MetaWebhookTextBody
    {
        public string? Body { get; set; }
    }

    public class MetaWebhookError
    {
        public int Code { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
    }
}
