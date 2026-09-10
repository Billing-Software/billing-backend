using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BillingBackend.DTOs
{
    public class CreateOrderRequestDto
    {
        /// <summary>Plan selected by the signed-in business. The server resolves its price.</summary>
        [Required]
        [JsonPropertyName("planId")]
        public int PlanId { get; set; }

        /// <summary>
        /// Billing cycle when planId is specified: "monthly" or "yearly"
        /// </summary>
        [RegularExpression("^(monthly|yearly)$", ErrorMessage = "BillingCycle must be monthly or yearly.")]
        [JsonPropertyName("billingCycle")]
        public string? BillingCycle { get; set; }

        /// <summary>
    }

    public class CreateOrderResponseDto
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "INR";

        [JsonPropertyName("key_id")]
        public string? KeyId { get; set; }

        [JsonPropertyName("plan_id")]
        public int PlanId { get; set; }

        [JsonPropertyName("plan_name")]
        public string? PlanName { get; set; }
    }

    public class VerifyPaymentRequestDto
    {
        [JsonPropertyName("razorpay_order_id")]
        public string? RazorpayOrderId { get; set; }

        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }

        [JsonPropertyName("razorpay_payment_id")]
        public string? RazorpayPaymentId { get; set; }

        [JsonPropertyName("payment_id")]
        public string? PaymentId { get; set; }

        [JsonPropertyName("razorpay_signature")]
        public string? RazorpaySignature { get; set; }

        [JsonPropertyName("signature")]
        public string? Signature { get; set; }

        [JsonIgnore]
        public string? EffectiveOrderId => !string.IsNullOrWhiteSpace(RazorpayOrderId) ? RazorpayOrderId : OrderId;

        [JsonIgnore]
        public string? EffectivePaymentId => !string.IsNullOrWhiteSpace(RazorpayPaymentId) ? RazorpayPaymentId : PaymentId;

        [JsonIgnore]
        public string? EffectiveSignature => !string.IsNullOrWhiteSpace(RazorpaySignature) ? RazorpaySignature : Signature;
    }

    public class VerifyPaymentResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }

        [JsonPropertyName("payment_id")]
        public string? PaymentId { get; set; }

        [JsonPropertyName("pending_capture")]
        public bool PendingCapture { get; set; }

        [JsonPropertyName("subscription_status")]
        public string? SubscriptionStatus { get; set; }

        [JsonPropertyName("plan_name")]
        public string? PlanName { get; set; }
    }
}
