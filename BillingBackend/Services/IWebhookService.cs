using BillingBackend.DTOs;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    /// <summary>
    /// Processes incoming Meta webhook events for message status updates.
    /// </summary>
    public interface IWebhookService
    {
        /// <summary>
        /// Validate the X-Hub-Signature-256 header from Meta.
        /// </summary>
        bool ValidateSignature(string payload, string signatureHeader);

        /// <summary>
        /// Process a webhook payload — update message delivery statuses.
        /// </summary>
        Task ProcessWebhookAsync(MetaWebhookPayload payload);
    }
}
