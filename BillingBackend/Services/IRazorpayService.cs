using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IRazorpayService
    {
        Task<string?> CreateCustomerAsync(string name, string email, string phone);
        Task<string?> CreateSubscriptionAsync(string planId, string customerId);
        bool VerifyPaymentSignature(string subscriptionId, string paymentId, string signature);
        bool VerifyWebhookSignature(string payload, string signature);
    }
}
