using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingBackend.Data.Entities
{
    public class WhatsAppAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessId { get; set; }

        /// <summary>
        /// Meta's Business ID returned from Embedded Signup.
        /// </summary>
        [MaxLength(100)]
        public string? MetaBusinessId { get; set; }

        /// <summary>
        /// WhatsApp Business Account ID.
        /// </summary>
        [MaxLength(100)]
        public string? WabaId { get; set; }

        /// <summary>
        /// Phone Number ID used for sending messages via Cloud API.
        /// </summary>
        [MaxLength(100)]
        public string? PhoneNumberId { get; set; }

        /// <summary>
        /// Human-readable display phone number (e.g. "+91 98765 43210").
        /// </summary>
        [MaxLength(30)]
        public string? DisplayPhoneNumber { get; set; }

        /// <summary>
        /// Business Integration System User Token — stored AES-256 encrypted.
        /// </summary>
        [MaxLength(1000)]
        public string? AccessToken { get; set; }

        /// <summary>
        /// When the access token expires. Null if the token does not expire.
        /// </summary>
        public DateTime? TokenExpiry { get; set; }

        /// <summary>
        /// Account status: Pending, Connected, Disconnected, Error.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Provider type: Twilio (or Meta)
        /// </summary>
        [MaxLength(50)]
        public string Provider { get; set; } = "Twilio";

        /// <summary>
        /// Dedicated Twilio Customer Subaccount SID provisioned under BillCom parent account.
        /// </summary>
        [MaxLength(100)]
        public string? TwilioSubaccountSid { get; set; }

        /// <summary>
        /// Subaccount Auth Token — stored AES-256 encrypted.
        /// </summary>
        [MaxLength(1000)]
        public string? TwilioSubaccountAuthToken { get; set; }

        /// <summary>
        /// Twilio WhatsApp Sender ID / SID registered under customer subaccount.
        /// </summary>
        [MaxLength(100)]
        public string? SenderId { get; set; }

        /// <summary>
        /// Business Display Name shown on WhatsApp profile.
        /// </summary>
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DisconnectedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(BusinessId))]
        public Business Business { get; set; } = null!;

        public ICollection<MessageLog> MessageLogs { get; set; } = new List<MessageLog>();
        public ICollection<WhatsAppTemplate> WhatsAppTemplates { get; set; } = new List<WhatsAppTemplate>();
    }
}
