using System.IO;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    /// <summary>
    /// HTTP client wrapper for Meta Graph API / WhatsApp Cloud API operations.
    /// </summary>
    public interface IMetaApiClient
    {
        /// <summary>
        /// Exchange the OAuth authorization code from Embedded Signup for an access token.
        /// </summary>
        Task<MetaTokenResponse> ExchangeCodeForTokenAsync(string code);

        /// <summary>
        /// Get shared WABA information (WABA ID, phone numbers) using the access token.
        /// </summary>
        Task<MetaBusinessInfoResponse> GetSharedWabaInfoAsync(string accessToken);

        /// <summary>
        /// Subscribe the BillCom App to receive webhooks from a client's WABA.
        /// POST /{waba_id}/subscribed_apps
        /// </summary>
        Task<bool> SubscribeWabaToAppAsync(string wabaId, string accessToken);

        /// <summary>
        /// Get phone numbers associated with a WABA.
        /// GET /{waba_id}/phone_numbers
        /// </summary>
        Task<List<MetaPhoneNumberInfo>> GetWabaPhoneNumbersAsync(string wabaId, string accessToken);

        /// <summary>
        /// Register a WhatsApp Cloud API phone number.
        /// POST /{phone_number_id}/register
        /// </summary>
        Task<bool> RegisterPhoneNumberAsync(string phoneNumberId, string accessToken, string pin = "654321");

        /// <summary>
        /// Create a message template in a client's WABA via Meta Graph API.
        /// POST /{waba_id}/message_templates
        /// </summary>
        Task<MetaTemplateCreateResponse> CreateWabaTemplateAsync(string wabaId, string accessToken, string templateName, string category, string language, string bodyText);

        /// <summary>
        /// Fetch all message templates under a WABA from Meta Graph API.
        /// GET /{waba_id}/message_templates
        /// </summary>
        Task<List<MetaTemplateInfo>> GetWabaTemplatesAsync(string wabaId, string accessToken);

        /// <summary>
        /// Send a text message via Cloud API.
        /// </summary>
        Task<MetaSendMessageResponse> SendTextMessageAsync(string phoneNumberId, string accessToken, string to, string text);

        /// <summary>
        /// Send a document message via Cloud API using a media URL.
        /// </summary>
        Task<MetaSendMessageResponse> SendDocumentMessageAsync(string phoneNumberId, string accessToken, string to, string documentUrl, string? caption, string? filename);

        /// <summary>
        /// Send a template message via Cloud API.
        /// </summary>
        Task<MetaSendMessageResponse> SendTemplateMessageAsync(string phoneNumberId, string accessToken, string to, string templateName, System.Collections.Generic.Dictionary<string, string>? parameters, string languageCode = "en_US");
    }

    // Response models for Meta API calls

    public class MetaTokenResponse
    {
        public string? AccessToken { get; set; }
        public string? TokenType { get; set; }
        public long? ExpiresIn { get; set; }
        public string? Error { get; set; }
    }

    public class MetaBusinessInfoResponse
    {
        public string? WabaId { get; set; }
        public string? BusinessId { get; set; }
        public string? PhoneNumberId { get; set; }
        public string? DisplayPhoneNumber { get; set; }
        public string? Error { get; set; }
    }

    public class MetaSendMessageResponse
    {
        public string? MessageId { get; set; }
        public string? Error { get; set; }
        public bool Success => !string.IsNullOrEmpty(MessageId) && string.IsNullOrEmpty(Error);
    }

    public class MetaPhoneNumberInfo
    {
        public string? Id { get; set; }
        public string? DisplayPhoneNumber { get; set; }
        public string? VerifiedName { get; set; }
        public string? QualityRating { get; set; }
        public string? Status { get; set; }
    }

    public class MetaTemplateCreateResponse
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public string? Error { get; set; }
        public bool Success => !string.IsNullOrEmpty(Id) || Status == "APPROVED" || Status == "PENDING";
    }

    public class MetaTemplateInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Language { get; set; }
        public string? Status { get; set; }
        public string? BodyText { get; set; }
    }
}
