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
        Task<MetaSendMessageResponse> SendTemplateMessageAsync(string phoneNumberId, string accessToken, string to, string templateName, System.Collections.Generic.Dictionary<string, string>? parameters);
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
}
