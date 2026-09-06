using System;

namespace BillingBackend.DTOs.WhatsApp
{
    /// <summary>
    /// Safe public representation of a connected WhatsApp Business account.
    /// Exposes no sensitive Twilio credentials (AuthTokens are NEVER sent to the client).
    /// </summary>
    public class WhatsAppConnectionResponseDto
    {
        public int Id { get; set; }
        public bool Connected => Status == "Connected";
        public string Status { get; set; } = "NotConnected";
        public string? DisplayPhoneNumber { get; set; }
        public string? DisplayName { get; set; }
        public string? PhoneNumberId { get; set; }
        public string? WabaId { get; set; }
        public string? TwilioSubaccountSid { get; set; }
        public string? SenderId { get; set; }
        public string Provider { get; set; } = "Twilio";
        public DateTime? ConnectedAt { get; set; }
        public DateTime? DisconnectedAt { get; set; }
    }
}
