using System.ComponentModel.DataAnnotations;

namespace BillingBackend.DTOs.WhatsApp
{
    /// <summary>
    /// Data payload returned from the Meta WhatsApp Embedded Signup flow.
    /// Used by BillCom backend to create an isolated Twilio Subaccount and
    /// register the WhatsApp sender via Twilio Senders API.
    /// </summary>
    public class WhatsAppOnboardingDto
    {
        /// <summary>
        /// Customer's WhatsApp Business Account (WABA) ID created or selected during Embedded Signup.
        /// </summary>
        [Required]
        public string? WabaId { get; set; }

        /// <summary>
        /// Phone Number ID registered during Embedded Signup.
        /// </summary>
        public string? PhoneNumberId { get; set; }

        /// <summary>
        /// Customer's verified business phone number in E.164 format (e.g. +919876543210).
        /// </summary>
        [Required]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Formatted human-readable phone number (e.g. "+91 98765 43210").
        /// </summary>
        public string? DisplayPhoneNumber { get; set; }

        /// <summary>
        /// Verified display name for the WhatsApp business profile.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Optional OAuth authorization code returned by Embedded Signup if exchanging server-side.
        /// </summary>
        public string? Code { get; set; }
    }

    /// <summary>
    /// Metadata required by the frontend to initiate Meta WhatsApp Embedded Signup.
    /// </summary>
    public class WhatsAppOnboardingConfigDto
    {
        public string Provider { get; set; } = "Twilio";
        public string? AppId { get; set; }
        public string? ConfigId { get; set; }
        public string? PartnerSolutionId { get; set; }
        public string? WebhookBaseUrl { get; set; }
    }
}
