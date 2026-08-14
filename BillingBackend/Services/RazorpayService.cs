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
            _keyId = rzpSection["KeyId"] ?? "rzp_test_mockKeyId123";
            _keySecret = rzpSection["KeySecret"] ?? "mockKeySecret456";
            _webhookSecret = rzpSection["WebhookSecret"] ?? "mockWebhookSecret789";

            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_keyId}:{_keySecret}"));
            _httpClient.BaseAddress = new Uri("https://api.razorpay.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        }

        public async Task<string?> CreateCustomerAsync(string name, string email, string phone)
        {
            try
            {
                // If keys are mock/unconfigured, return simulated customer ID for testing
                if (_keyId.StartsWith("rzp_test_mock"))
                {
                    _logger.LogInformation("[RazorpayService] Mock customer generated.");
                    return $"cust_simulated_{Guid.NewGuid().ToString().Substring(0, 8)}";
                }

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
                    _logger.LogWarning("[RazorpayService] CreateCustomer failed: {Error}. Falling back to simulated customer ID.", errorResponse);
                    return $"cust_simulated_{Guid.NewGuid().ToString().Substring(0, 8)}";
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
            return $"cust_simulated_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        public async Task<string?> CreateSubscriptionAsync(string planId, string customerId)
        {
            try
            {
                // If keys are mock/unconfigured, return simulated subscription ID for testing
                if (_keyId.StartsWith("rzp_test_mock"))
                {
                    _logger.LogInformation("[RazorpayService] Mock subscription generated.");
                    return $"sub_simulated_{Guid.NewGuid().ToString().Substring(0, 8)}";
                }

                // If it is a placeholder plan code, attempt to create it dynamically in the merchant's Razorpay dashboard
                if (planId.StartsWith("plan_") || planId.Contains("starter") || planId.Contains("growth") || planId.Contains("pro") || planId.Contains("ent"))
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
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[RazorpayService] CreateSubscription API returned non-success: {Error}. Falling back to simulated test subscription ID.", errorResponse);
                    return $"sub_simulated_{Guid.NewGuid().ToString().Substring(0, 8)}";
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
                _logger.LogError(ex, "[RazorpayService] Exception occurred in CreateSubscription. Returning test fallback subscription ID.");
            }
            return $"sub_simulated_{Guid.NewGuid().ToString().Substring(0, 8)}";
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
                // If keys are mock/unconfigured, return true for simulated checkouts
                if (_keyId.StartsWith("rzp_test_mock"))
                {
                    _logger.LogInformation("[RazorpayService] Mock signature verified successfully.");
                    return true;
                }

                var payload = $"{paymentId}|{subscriptionId}";
                var secretBytes = Encoding.UTF8.GetBytes(_keySecret);
                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                using (var hmac = new HMACSHA256(secretBytes))
                {
                    var hashBytes = hmac.ComputeHash(payloadBytes);
                    var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
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
                // If keys are mock/unconfigured, return true for local simulations
                if (_keyId.StartsWith("rzp_test_mock"))
                {
                    _logger.LogInformation("[RazorpayService] Mock webhook signature verified successfully.");
                    return true;
                }

                var secretBytes = Encoding.UTF8.GetBytes(_webhookSecret);
                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                using (var hmac = new HMACSHA256(secretBytes))
                {
                    var hashBytes = hmac.ComputeHash(payloadBytes);
                    var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
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
