using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Services
{
    public class RazorpayService : IRazorpayService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RazorpayService> _logger;
        private readonly string _keyId;
        private readonly string _keySecret;
        private readonly string _webhookSecret;

        public RazorpayService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<RazorpayService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var rzpSection = _configuration.GetSection("Razorpay");
            _keyId = Environment.GetEnvironmentVariable("RAZORPAY_KEY_ID") 
                     ?? _configuration["RAZORPAY_KEY_ID"] 
                     ?? rzpSection["KeyId"] 
                     ?? string.Empty;
            _keySecret = Environment.GetEnvironmentVariable("RAZORPAY_KEY_SECRET") 
                         ?? _configuration["RAZORPAY_KEY_SECRET"] 
                         ?? rzpSection["KeySecret"] 
                         ?? string.Empty;
            _webhookSecret = Environment.GetEnvironmentVariable("RAZORPAY_WEBHOOK_SECRET") 
                             ?? _configuration["RAZORPAY_WEBHOOK_SECRET"] 
                             ?? rzpSection["WebhookSecret"] 
                             ?? string.Empty;

            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_keyId}:{_keySecret}"));
            _httpClient.BaseAddress = new Uri("https://api.razorpay.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        }

        public string GetKeyId() => _keyId;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_keyId) &&
                                    !string.IsNullOrWhiteSpace(_keySecret) &&
                                    !string.IsNullOrWhiteSpace(_webhookSecret);

        public async Task<(bool Success, string? OrderId, long Amount, string? Currency, string? ErrorMessage, int StatusCode)> CreateOrderAsync(
            long amountInPaise, 
            string currency = "INR", 
            string? receipt = null,
            Dictionary<string, string>? notes = null)
        {
            try
            {
                if (!IsConfigured)
                {
                    _logger.LogError("[RazorpayService] Razorpay credentials are not configured.");
                    return (false, null, amountInPaise, currency, "Payment gateway is not configured.", 503);
                }
                if (amountInPaise < 100)
                {
                    return (false, null, amountInPaise, currency, "Amount must be at least 100 paise (₹1.00).", 400);
                }

                var payload = new Dictionary<string, object>
                {
                    { "amount", amountInPaise },
                    { "currency", string.IsNullOrWhiteSpace(currency) ? "INR" : currency.ToUpperInvariant() },
                    { "receipt", string.IsNullOrWhiteSpace(receipt) ? $"rcpt_{Guid.NewGuid():N}".Substring(0, 20) : receipt }
                };

                if (notes != null && notes.Count > 0)
                {
                    payload["notes"] = notes;
                }

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("orders", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("[RazorpayService] Razorpay authentication failed (401 Unauthorized). Verify RAZORPAY_KEY_ID and RAZORPAY_KEY_SECRET.");
                    return (false, null, amountInPaise, currency, "Razorpay authentication failed. Invalid Key ID or Key Secret.", 401);
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[RazorpayService] CreateOrder failed with status {StatusCode}: {ErrorBody}", response.StatusCode, responseBody);
                    string errorDesc = "Failed to create order on Razorpay.";
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(responseBody);
                        if (errorDoc.RootElement.TryGetProperty("error", out var errObj) &&
                            errObj.TryGetProperty("description", out var descProp))
                        {
                            errorDesc = descProp.GetString() ?? errorDesc;
                        }
                    }
                    catch { }

                    return (false, null, amountInPaise, currency, errorDesc, 500);
                }

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                string? orderId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                long returnedAmount = root.TryGetProperty("amount", out var amtProp) ? amtProp.GetInt64() : amountInPaise;
                string returnedCurrency = root.TryGetProperty("currency", out var currProp) ? currProp.GetString() ?? currency : currency;

                if (string.IsNullOrEmpty(orderId))
                {
                    return (false, null, amountInPaise, currency, "Razorpay response did not include a valid order ID.", 500);
                }

                _logger.LogInformation("[RazorpayService] Created Razorpay order {OrderId} for amount {Amount} {Currency}", orderId, returnedAmount, returnedCurrency);
                return (true, orderId, returnedAmount, returnedCurrency, null, 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RazorpayService] Exception occurred in CreateOrderAsync");
                return (false, null, amountInPaise, currency, ex.Message, 500);
            }
        }

        public bool VerifyOrderPaymentSignature(string orderId, string paymentId, string signature)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(signature))
                {
                    return false;
                }

                var payload = $"{orderId}|{paymentId}";
                var secretBytes = Encoding.UTF8.GetBytes(_keySecret);
                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                using (var hmac = new HMACSHA256(secretBytes))
                {
                    var hashBytes = hmac.ComputeHash(payloadBytes);
                    var computedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();

                    return CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(computedSignature),
                        Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant())
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RazorpayService] Exception occurred in VerifyOrderPaymentSignature");
                return false;
            }
        }

        public async Task<(bool Success, string? Status, long Amount, string? OrderId, string? Method, string? ErrorMessage)> FetchPaymentAsync(string paymentId)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(paymentId))
                return (false, null, 0, null, null, "Payment gateway is not configured or payment ID is invalid.");

            try
            {
                var response = await _httpClient.GetAsync($"payments/{Uri.EscapeDataString(paymentId)}");
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[RazorpayService] Fetch payment {PaymentId} failed with {StatusCode}.", paymentId, response.StatusCode);
                    return (false, null, 0, null, null, "Unable to confirm payment status with Razorpay.");
                }

                using var document = JsonDocument.Parse(body);
                var payment = document.RootElement;
                return (true,
                    payment.TryGetProperty("status", out var status) ? status.GetString() : null,
                    payment.TryGetProperty("amount", out var amount) ? amount.GetInt64() : 0,
                    payment.TryGetProperty("order_id", out var order) ? order.GetString() : null,
                    payment.TryGetProperty("method", out var method) ? method.GetString() : null,
                    null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RazorpayService] Fetch payment failed for {PaymentId}.", paymentId);
                return (false, null, 0, null, null, "Unable to confirm payment status with Razorpay.");
            }
        }

        public async Task<string?> CreateCustomerAsync(string name, string email, string phone)
        {
            try
            {
                var payload = new Dictionary<string, object>
                {
                    { "name", name },
                    { "email", email }
                };
                if (!string.IsNullOrWhiteSpace(phone) && phone.Trim().Length >= 10)
                {
                    payload["contact"] = phone.Trim();
                }

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("customers", content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[RazorpayService] CreateCustomer failed with {Status}.", response.StatusCode);
                    return null;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(responseString))
                {
                    if (doc.RootElement.TryGetProperty("id", out var idProp))
                    {
                        return idProp.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RazorpayService] Exception occurred in CreateCustomer");
            }
            return null;
        }

        public async Task<string?> CreateSubscriptionAsync(string planId, string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(planId))
                    return null;
                // If it is a placeholder plan code, attempt to create it dynamically in the merchant's Razorpay dashboard
                if (planId.StartsWith("plan_", StringComparison.OrdinalIgnoreCase) || planId.Contains("starter", StringComparison.OrdinalIgnoreCase) || planId.Contains("growth", StringComparison.OrdinalIgnoreCase) || planId.Contains("pro", StringComparison.OrdinalIgnoreCase) || planId.Contains("ent", StringComparison.OrdinalIgnoreCase))
                {
                    var resolvedRealPlanId = await CreatePlanOnTheFlyAsync(planId);
                    if (!string.IsNullOrEmpty(resolvedRealPlanId))
                    {
                        planId = resolvedRealPlanId;
                    }
                }

                var payload = new Dictionary<string, object>
                {
                    { "plan_id", planId },
                    { "total_count", 120 }, // 10 years of monthly/yearly cycles
                    { "quantity", 1 },
                    { "customer_notify", 1 }
                };

                if (!string.IsNullOrWhiteSpace(customerId) && !customerId.StartsWith("cust_simulated_"))
                {
                    payload["customer_id"] = customerId;
                }

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("subscriptions", content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[RazorpayService] CreateSubscription API returned non-success: {Status}.", response.StatusCode);
                    return null;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(responseString))
                {
                    if (doc.RootElement.TryGetProperty("id", out var idProp))
                    {
                        return idProp.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RazorpayService] Exception occurred in CreateSubscription.");
            }
            return null;
        }

        private async Task<string?> CreatePlanOnTheFlyAsync(string planCode)
        {
            try
            {
                string name = "BillCom Starter Plan";
                int amount = 49900; // in paise (₹499)
                if (planCode.Contains("growth") || planCode.Contains("pro") || planCode.Contains("professional"))
                {
                    name = "BillCom Growth Plan";
                    amount = 149900; // ₹1,499
                }
                else if (planCode.Contains("ent") || planCode.Contains("enterprise"))
                {
                    name = "BillCom Enterprise Plan";
                    amount = 499900; // ₹4,999
                }

                var payload = new
                {
                    period = "monthly",
                    interval = 1,
                    item = new
                    {
                        name = name,
                        amount = amount,
                        currency = "INR",
                        description = "Monthly recurring billing subscription plan"
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("plans", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using (var doc = JsonDocument.Parse(responseString))
                    {
                        if (doc.RootElement.TryGetProperty("id", out var idProp))
                        {
                            var realPlanId = idProp.GetString();
                            _logger.LogInformation("[RazorpayService] Dynamically created real subscription plan: {RealPlanId} for code {PlanCode}", realPlanId, planCode);
                            return realPlanId;
                        }
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[RazorpayService] Dynamic plan creation notice: {Error}", errorResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RazorpayService] Exception occurred in CreatePlanOnTheFly");
            }
            return null;
        }

        public bool VerifyPaymentSignature(string subscriptionId, string paymentId, string signature)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(signature))
                    return false;
                if (string.IsNullOrWhiteSpace(_keySecret))
                    return false;

                var payload = $"{paymentId}|{subscriptionId}";
                var secretBytes = Encoding.UTF8.GetBytes(_keySecret);
                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                using (var hmac = new HMACSHA256(secretBytes))
                {
                    var hashBytes = hmac.ComputeHash(payloadBytes);
                    var computedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();
                    return CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(computedSignature),
                        Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RazorpayService] Exception occurred in VerifyPaymentSignature");
                return false;
            }
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_webhookSecret)) return false;
                if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature)) return false;

                var secretBytes = Encoding.UTF8.GetBytes(_webhookSecret);
                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                using (var hmac = new HMACSHA256(secretBytes))
                {
                    var hashBytes = hmac.ComputeHash(payloadBytes);
                    var computedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();
                    return CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(computedSignature),
                        Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RazorpayService] Exception occurred in VerifyWebhookSignature");
                return false;
            }
        }
    }
}
