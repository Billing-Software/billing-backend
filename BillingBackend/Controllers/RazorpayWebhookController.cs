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
using Microsoft.EntityFrameworkCore;
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
        private readonly IEmailService _emailService;
        private readonly ILogger<RazorpayWebhookController> _logger;

        public RazorpayWebhookController(
            BillingDbContext context, 
            IRazorpayService razorpayService,
            IEmailService emailService,
            ILogger<RazorpayWebhookController> logger)
        {
            _context = context;
            _razorpayService = razorpayService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost]
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
                    logEntry.FailureReason = ex.Message;
                    _context.WebhookEventLogs.Update(logEntry);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogError(ex, "[RazorpayWebhook Error] Processing failed for event {EventId}", externalEventId);
                    // Return OK so Razorpay doesn't retry infinitely on business logic errors, since we've audited it
                    return Ok(new { status = "failed", error = ex.Message });
                }

                return Ok(new { status = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RazorpayWebhook Exception] HandleWebhook crashed");
                return StatusCode(500, new { error = ex.Message });
            }
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

                    _logger.LogWarning("[RazorpayWebhook Warning] Business ID {BusinessId} payment failed. Status set to: PastDue. Reason: {Reason}", business.Id, failureReason);

                    // Send subscription billing warning notification email asynchronously (non-blocking)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var owner = await _context.Users.FirstOrDefaultAsync(u => u.Id == business.OwnerId);
                            if (owner != null && !string.IsNullOrEmpty(owner.Email))
                            {
                                var emailBody = $@"
                                    <h2>SmartBill Pro - Subscription Renewal Payment Failed!</h2>
                                    <p>Dear {owner.Username},</p>
                                    <p>We attempted to charge your card/UPI mandate for your subscription renewal, but the transaction failed.</p>
                                    <p><strong>Business Store:</strong> {business.LegalName}</p>
                                    <p><strong>Failed Amount:</strong> ₹{amount}</p>
                                    <p><strong>Gateway Reason:</strong> {failureReason}</p>
                                    <p>To avoid service disruption and lockout, please login to your billing manager page or contact platform support to resolve this payment issue.</p>
                                    <br/>
                                    <p>Best regards,<br/>SmartBill Pro Support Operations Team</p>";

                                await _emailService.SendEmailAsync(owner.Email, "SmartBill Pro - Action Required: Subscription Payment Failed", emailBody);
                                _logger.LogInformation("[RazorpayWebhook Success] Sent billing failure notification email to: {Email} for Business ID: {BusinessId}", owner.Email, business.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[RazorpayWebhook Error] Failed to send payment failure notification email for Business ID: {BusinessId}", business.Id);
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
