using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/webhooks/razorpay")]
    public class RazorpayWebhookController : ControllerBase
    {
        private readonly BillingDbContext _context;
        private readonly IRazorpayService _razorpayService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RazorpayWebhookController> _logger;

        public RazorpayWebhookController(
            BillingDbContext context,
            IRazorpayService razorpayService,
            IServiceScopeFactory scopeFactory,
            ILogger<RazorpayWebhookController> logger)
        {
            _context = context;
            _razorpayService = razorpayService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [HttpPost]
        [EnableRateLimiting("api")]
        [RequestSizeLimit(1 * 1024 * 1024)]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                // 1. Read request body raw string
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var jsonPayload = await reader.ReadToEndAsync();

                // 2. Read Razorpay signature header
                if (!Request.Headers.TryGetValue("X-Razorpay-Signature", out var signatureHeader))
                {
                    _logger.LogWarning("[RazorpayWebhook] Missing Razorpay Webhook signature header.");
                    return BadRequest(new { message = "Missing Razorpay Webhook signature header." });
                }

                string signature = signatureHeader.ToString();

                // 3. Verify signature
                bool isValid = _razorpayService.VerifyWebhookSignature(jsonPayload, signature);
                if (!isValid)
                {
                    _logger.LogWarning("[RazorpayWebhook] Webhook signature verification failed.");
                    return Unauthorized(new { message = "Webhook signature verification failed." });
                }

                // 4. Parse Webhook Event JSON
                using var document = JsonDocument.Parse(jsonPayload);
                var root = document.RootElement;
                
                if (!root.TryGetProperty("event", out var eventProp))
                {
                    _logger.LogWarning("[RazorpayWebhook] Missing event property in webhook payload.");
                    return BadRequest(new { message = "Missing event property." });
                }

                string webhookEvent = eventProp.GetString() ?? "";
                
                // Get unique Event ID from Razorpay to deduplicate
                string externalEventId = "";
                if (root.TryGetProperty("id", out var idProp))
                {
                    externalEventId = idProp.GetString() ?? "";
                }

                _logger.LogInformation("[RazorpayWebhook] Received event: {Event} with ID: {EventId}", webhookEvent, externalEventId);

                // Deduplication check
                if (!string.IsNullOrEmpty(externalEventId))
                {
                    var alreadyProcessed = await _context.WebhookEventLogs
                        .AnyAsync(w => w.Source == "Razorpay" && w.ExternalEventId == externalEventId && w.ProcessingStatus == "Completed");
                    if (alreadyProcessed)
                    {
                        _logger.LogInformation("[RazorpayWebhook] Duplicate webhook detected: {EventId}. Skipping processing.", externalEventId);
                        return Ok(new { status = "success", message = "duplicate" });
                    }
                }

                // Save event log
                var logEntry = new WebhookEventLog
                {
                    Source = "Razorpay",
                    ExternalEventId = string.IsNullOrEmpty(externalEventId) ? null : externalEventId,
                    EventType = webhookEvent,
                    RawPayload = jsonPayload,
                    ProcessingStatus = "Processing",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.WebhookEventLogs.AddAsync(logEntry);
                await _context.SaveChangesAsync();

                // Process the event
                try
                {
                    if (root.TryGetProperty("payload", out var payloadProp))
                    {
                        if (webhookEvent == "subscription.charged")
                        {
                            await ProcessSubscriptionCharged(payloadProp, jsonPayload, logEntry.Id);
                        }
                        else if (webhookEvent == "subscription.cancelled" || webhookEvent == "subscription.halted")
                        {
                            await ProcessSubscriptionCancelledOrHalted(payloadProp, webhookEvent, logEntry.Id);
                        }
                        else if (webhookEvent == "subscription.activated")
                        {
                            await ProcessSubscriptionActivated(payloadProp, logEntry.Id);
                        }
                        else if (webhookEvent == "payment.failed")
                        {
                            await ProcessPaymentFailed(payloadProp, jsonPayload, logEntry.Id);
                        }
                        else if (webhookEvent == "payment.captured")
                        {
                            await ProcessPaymentCaptured(payloadProp, jsonPayload, logEntry.Id);
                        }
                        else
                        {
                            _logger.LogInformation("[RazorpayWebhook] Event {Event} is unhandled. Ignoring.", webhookEvent);
                        }
                    }

                    logEntry.ProcessingStatus = "Completed";
                    logEntry.ProcessedAt = DateTime.UtcNow;
                    _context.WebhookEventLogs.Update(logEntry);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logEntry.ProcessingStatus = "Failed";
                    logEntry.FailureReason = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                    _context.WebhookEventLogs.Update(logEntry);
                    await _context.SaveChangesAsync();

                    _logger.LogError(ex, "[RazorpayWebhook] Processing failed for event {EventId}", externalEventId);
                    // Return OK so Razorpay doesn't retry infinitely on business logic errors, since we've audited it
                    return Ok(new { status = "failed" });
                }

                return Ok(new { status = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RazorpayWebhook] HandleWebhook crashed");
                return StatusCode(500, new { error = "Webhook processing failed." });
            }
        }

        // The webhook is the durable reconciliation path. It is deliberately idempotent because
        // Razorpay can retry deliveries and Checkout verification may have already captured it.
        private async Task ProcessPaymentCaptured(JsonElement payload, string rawPayload, int webhookEventId)
        {
            if (!payload.TryGetProperty("payment", out var payment) || !payment.TryGetProperty("entity", out var entity)) return;
            var paymentId = entity.TryGetProperty("id", out var id) ? id.GetString() : null;
            var orderId = entity.TryGetProperty("order_id", out var order) ? order.GetString() : null;
            var amount = entity.TryGetProperty("amount", out var value) ? value.GetInt64() : 0;
            var method = entity.TryGetProperty("method", out var paymentMethod) ? paymentMethod.GetString() : null;
            if (string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(orderId)) return;

            var transaction = await _context.PaymentTransactions.SingleOrDefaultAsync(x => x.RazorpayOrderId == orderId);
            if (transaction is null || transaction.Status == "Captured") return;
            if (decimal.ToInt64(transaction.Amount * 100m) != amount)
            {
                _logger.LogWarning("[RazorpayWebhook] Captured payment amount mismatch for order {OrderId}.", orderId);
                transaction.Status = "Failed"; transaction.FailureReason = "Captured webhook amount mismatch"; transaction.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return;
            }

            var business = await _context.Businesses.SingleOrDefaultAsync(x => x.Id == transaction.BusinessId);
            var plan = transaction.SubscriptionPlanId.HasValue
                ? await _context.SubscriptionPlans.SingleOrDefaultAsync(x => x.Id == transaction.SubscriptionPlanId)
                : null;
            if (business is null || plan is null) throw new InvalidOperationException("Payment order is missing subscription context.");

            var start = business.SubscriptionExpiresAt > DateTime.UtcNow ? business.SubscriptionExpiresAt.Value : DateTime.UtcNow;
            business.ActivePlanId = plan.Id; business.AllowedBranches = plan.MaxBranches == -1 ? 999 : plan.MaxBranches; business.AllowedStaff = plan.MaxStaff == -1 ? 999 : plan.MaxStaff;
            business.IsTrial = false; business.SubscriptionStatus = "Active"; business.SubscriptionExpiresAt = string.Equals(transaction.BillingCycle, "yearly", StringComparison.OrdinalIgnoreCase) ? start.AddYears(1) : start.AddMonths(1); business.UpdatedAt = DateTime.UtcNow;
            transaction.RazorpayPaymentId = paymentId; transaction.PaymentMethod = method ?? "Razorpay"; transaction.RawWebhookPayload = rawPayload; transaction.WebhookEventId = webhookEventId; transaction.Status = "Captured"; transaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private async Task ProcessSubscriptionCharged(JsonElement payload, string rawPayload, int webhookEventId)
        {
            if (payload.TryGetProperty("subscription", out var subProp) &&
                subProp.TryGetProperty("entity", out var subEntity))
            {
                string subId = subEntity.GetProperty("id").GetString() ?? "";
                
                // Retrieve associated business
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.RazorpaySubscriptionId == subId);
                if (business == null)
                {
                    _logger.LogWarning("[RazorpayWebhook] Warning: Business with subscription ID '{SubId}' not found.", subId);
                    return;
                }

                // Get payment details
                string paymentId = "";
                decimal amount = 0;
                string method = "";
                if (payload.TryGetProperty("payment", out var payProp) &&
                    payProp.TryGetProperty("entity", out var payEntity))
                {
                    paymentId = payEntity.GetProperty("id").GetString() ?? "";
                    amount = payEntity.GetProperty("amount").GetDecimal() / 100.00m; // Convert paise to rupees
                    method = payEntity.GetProperty("method").GetString() ?? "";
                }

                // Update subscription validity
                business.SubscriptionStatus = "Active";
                business.SubscriptionExpiresAt = DateTime.UtcNow.AddMonths(1); // Auto extend subscription expiry by 1 month
                _context.Businesses.Update(business);

                // Add transaction record
                var transaction = new PaymentTransaction
                {
                    BusinessId = business.Id,
                    RazorpayPaymentId = paymentId,
                    RazorpaySubscriptionId = subId,
                    Amount = amount,
                    Status = "Captured",
                    PaymentMethod = method,
                    RawWebhookPayload = rawPayload,
                    WebhookEventId = webhookEventId,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.PaymentTransactions.AddAsync(transaction);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("[RazorpayWebhook Success] Business ID {BusinessId} subscription charged & extended successfully.", business.Id);
            }
        }

        private async Task ProcessSubscriptionActivated(JsonElement payload, int webhookEventId)
        {
            if (payload.TryGetProperty("subscription", out var subProp) &&
                subProp.TryGetProperty("entity", out var subEntity))
            {
                string subId = subEntity.GetProperty("id").GetString() ?? "";
                
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.RazorpaySubscriptionId == subId);
                if (business != null)
                {
                    business.SubscriptionStatus = "Active";
                    _context.Businesses.Update(business);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("[RazorpayWebhook Success] Business ID {BusinessId} subscription status set to: Active", business.Id);
                }
            }
        }

        private async Task ProcessPaymentFailed(JsonElement payload, string rawPayload, int webhookEventId)
        {
            string subId = "";
            string paymentId = "";
            decimal amount = 0;
            string method = "";
            string failureReason = "Unknown payment failure";

            if (payload.TryGetProperty("payment", out var payProp) &&
                payProp.TryGetProperty("entity", out var payEntity))
            {
                paymentId = payEntity.GetProperty("id").GetString() ?? "";
                amount = payEntity.GetProperty("amount").GetDecimal() / 100.00m;
                method = payEntity.GetProperty("method").GetString() ?? "";
                
                if (payEntity.TryGetProperty("error_description", out var descProp))
                {
                    failureReason = descProp.GetString() ?? failureReason;
                }

                if (payEntity.TryGetProperty("subscription_id", out var subIdProp))
                {
                    subId = subIdProp.GetString() ?? "";
                }
            }

            if (string.IsNullOrEmpty(subId) && payload.TryGetProperty("subscription", out var subProp) &&
                subProp.TryGetProperty("entity", out var subEntity))
            {
                subId = subEntity.GetProperty("id").GetString() ?? "";
            }

            if (!string.IsNullOrEmpty(subId))
            {
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.RazorpaySubscriptionId == subId);
                if (business != null)
                {
                    business.SubscriptionStatus = "PastDue";
                    _context.Businesses.Update(business);

                    // Add failed transaction log
                    var transaction = new PaymentTransaction
                    {
                        BusinessId = business.Id,
                        RazorpayPaymentId = paymentId,
                        RazorpaySubscriptionId = subId,
                        Amount = amount,
                        Status = "Failed",
                        PaymentMethod = method,
                        FailureReason = failureReason,
                        RawWebhookPayload = rawPayload,
                        WebhookEventId = webhookEventId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.PaymentTransactions.AddAsync(transaction);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("[RazorpayWebhook] Business {BusinessId} payment failed.", business.Id);

                    // Send billing warning email via a fresh scope (never use request DbContext in background thread).
                    var businessId = business.Id;
                    var ownerId = business.OwnerId;
                    var failedAmount = amount;
                    var gatewayReason = string.IsNullOrWhiteSpace(failureReason) ? "Payment failed." :
                        failureReason.Trim()[..Math.Min(200, failureReason.Trim().Length)];
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
                            var mail = scope.ServiceProvider.GetRequiredService<IEmailService>();
                            var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == ownerId);
                            var biz = await db.Businesses.FirstOrDefaultAsync(b => b.Id == businessId);
                            if (owner != null && biz != null && !string.IsNullOrEmpty(owner.Email))
                            {
                                // Escape user-controlled values before embedding in HTML email.
                                var safeUser = System.Net.WebUtility.HtmlEncode(owner.Username);
                                var safeBiz = System.Net.WebUtility.HtmlEncode(biz.LegalName);
                                var safeReason = System.Net.WebUtility.HtmlEncode(gatewayReason);
                                var emailBody = $@"
                                    <h2>BillCom - Subscription Renewal Payment Failed!</h2>
                                    <p>Dear {safeUser},</p>
                                    <p>We attempted to charge your payment method for your subscription renewal, but the transaction failed.</p>
                                    <p><strong>Failed Amount:</strong> ₹{failedAmount}</p>
                                    <p><strong>Gateway Reason:</strong> {safeReason}</p>
                                    <p>To avoid service disruption, please login to your billing page or contact support.</p>
                                    <br/>
                                    <p>Best regards,<br/>BillCom Support Team</p>";

                                await mail.SendEmailAsync(owner.Email, "BillCom - Action Required: Subscription Payment Failed", emailBody);
                                _logger.LogInformation("[RazorpayWebhook] Billing failure email sent for business {BusinessId}.", businessId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[RazorpayWebhook] Failed to send payment failure email for business {BusinessId}.", businessId);
                        }
                    });
                }
            }
        }

        private async Task ProcessSubscriptionCancelledOrHalted(JsonElement payload, string eventName, int webhookEventId)
        {
            if (payload.TryGetProperty("subscription", out var subProp) &&
                subProp.TryGetProperty("entity", out var subEntity))
            {
                string subId = subEntity.GetProperty("id").GetString() ?? "";
                
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.RazorpaySubscriptionId == subId);
                if (business != null)
                {
                    business.SubscriptionStatus = eventName == "subscription.cancelled" ? "Cancelled" : "Suspended";
                    _context.Businesses.Update(business);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("[RazorpayWebhook Success] Business ID {BusinessId} subscription status set to: {Status}", business.Id, business.SubscriptionStatus);
                }
            }
        }
    }
}
