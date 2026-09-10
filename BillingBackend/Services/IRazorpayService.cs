using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IRazorpayService
    {
        string GetKeyId();
        bool IsConfigured { get; }
        Task<(bool Success, string? OrderId, long Amount, string? Currency, string? ErrorMessage, int StatusCode)> CreateOrderAsync(
            long amountInPaise, 
            string currency = "INR", 
            string? receipt = null,
            Dictionary<string, string>? notes = null);
        bool VerifyOrderPaymentSignature(string orderId, string paymentId, string signature);
        Task<(bool Success, string? Status, long Amount, string? OrderId, string? Method, string? ErrorMessage)> FetchPaymentAsync(string paymentId);
        Task<string?> CreateCustomerAsync(string name, string email, string phone);
        Task<string?> CreateSubscriptionAsync(string planId, string customerId);
        bool VerifyPaymentSignature(string subscriptionId, string paymentId, string signature);
        bool VerifyWebhookSignature(string payload, string signature);
    }
}
