using BillingBackend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    /// <summary>
    /// Core WhatsApp integration service — handles connection, messaging, and status queries.
    /// </summary>
    public interface IWhatsAppService
    {
        /// <summary>
        /// Exchange the OAuth authorization code from Embedded Signup, store credentials, and connect the account.
        /// </summary>
        Task<WhatsAppAccountDto> ConnectAsync(int businessId, WhatsAppConnectCallbackDto dto);

        /// <summary>
        /// Get the current WhatsApp connection status for a business.
        /// </summary>
        Task<WhatsAppAccountDto?> GetStatusAsync(int businessId);

        /// <summary>
        /// Disconnect and revoke the WhatsApp account for a business.
        /// </summary>
        Task<bool> DisconnectAsync(int businessId);

        /// <summary>
        /// Send a text message to a phone number.
        /// </summary>
        Task<MessageLogDto> SendTextAsync(int businessId, SendTextMessageDto dto);

        /// <summary>
        /// Send an invoice PDF document to a phone number.
        /// </summary>
        Task<MessageLogDto> SendDocumentAsync(int businessId, SendDocumentDto dto);

        /// <summary>
        /// Send a template message to a phone number.
        /// </summary>
        Task<MessageLogDto> SendTemplateAsync(int businessId, SendTemplateDto dto);

        /// <summary>
        /// Get message logs, optionally filtered by bill ID.
        /// </summary>
        Task<IEnumerable<MessageLogDto>> GetMessageLogsAsync(int businessId, int? billId = null);

        /// <summary>
        /// Get all WhatsApp message templates managed for this business WABA.
        /// </summary>
        Task<IEnumerable<WhatsAppTemplateDto>> GetTemplatesAsync(int businessId);

        /// <summary>
        /// Programmatically create/ensure the default BillCom invoice template on the customer WABA.
        /// </summary>
        Task<WhatsAppTemplateDto> EnsureDefaultInvoiceTemplateAsync(int businessId);

        /// <summary>
        /// Send an invoice using the default approved WhatsApp invoice template.
        /// </summary>
        Task<MessageLogDto> SendInvoiceTemplateAsync(int businessId, SendInvoiceTemplateRequestDto dto);
    }
}
