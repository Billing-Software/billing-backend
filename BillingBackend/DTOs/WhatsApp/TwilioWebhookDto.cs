namespace BillingBackend.DTOs.WhatsApp
{
    /// <summary>
    /// Payload sent by Twilio for WhatsApp message delivery status callbacks.
    /// </summary>
    public class TwilioStatusCallbackDto
    {
        public string? MessageSid { get; set; }
        public string? MessageStatus { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? To { get; set; }
        public string? From { get; set; }
        public string? AccountSid { get; set; }
    }

    /// <summary>
    /// Payload sent by Twilio when a WhatsApp user replies or sends an inbound message.
    /// </summary>
    public class TwilioIncomingMessageDto
    {
        public string? MessageSid { get; set; }
        public string? SmsSid { get; set; }
        public string? AccountSid { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? Body { get; set; }
        public int? NumMedia { get; set; }
        public string? MediaUrl0 { get; set; }
    }
}
