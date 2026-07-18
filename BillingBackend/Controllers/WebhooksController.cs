using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace BillingBackend.Controllers
{
    /// <summary>
    /// Meta webhook endpoint — NOT JWT protected.
    /// Meta calls this endpoint directly to deliver message status updates.
    /// Security is via X-Hub-Signature-256 HMAC validation.
    /// </summary>
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly IWebhookService _webhookService;
        private readonly string _verifyToken;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            IWebhookService webhookService,
            IConfiguration configuration,
            ILogger<WebhooksController> logger)
        {
            _webhookService = webhookService;
            _verifyToken = configuration["Meta:WebhookVerifyToken"] ?? string.Empty;
            _logger = logger;
        }

        /// <summary>
        /// Webhook verification — Meta sends a GET with hub.challenge to verify your endpoint.
        /// </summary>
        [HttpGet("meta")]
        public IActionResult Verify(
            [FromQuery(Name = "hub.mode")] string? mode,
            [FromQuery(Name = "hub.challenge")] string? challenge,
            [FromQuery(Name = "hub.verify_token")] string? verifyToken)
        {
            _logger.LogInformation("Webhook verification request: mode={Mode}, token={Token}", mode, verifyToken);

            if (mode == "subscribe" && verifyToken == _verifyToken)
            {
                _logger.LogInformation("Webhook verified successfully");
                return Ok(challenge);
            }

            _logger.LogWarning("Webhook verification failed — token mismatch");
            return Forbid();
        }

        /// <summary>
        /// Receive webhook events from Meta — message status updates, incoming messages, etc.
        /// </summary>
        [HttpPost("meta")]
        public async Task<IActionResult> ReceiveWebhook()
        {
            // Read the raw body for signature validation
            string body;
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            // Validate signature
            var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            if (!string.IsNullOrEmpty(signature))
            {
                if (!_webhookService.ValidateSignature(body, signature))
                {
                    _logger.LogWarning("Webhook signature validation failed");
                    return Unauthorized();
                }
            }
            else
            {
                _logger.LogWarning("Webhook received without X-Hub-Signature-256 header");
                // In production, you should reject unsigned webhooks
                // For development, we'll allow them with a warning
            }

            // Parse and process the payload
            try
            {
                var payload = System.Text.Json.JsonSerializer.Deserialize<MetaWebhookPayload>(body,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                    });

                if (payload != null)
                {
                    await _webhookService.ProcessWebhookAsync(payload);
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize webhook payload: {Body}", body);
            }

            // Meta expects a 200 OK response within 20 seconds
            return Ok();
        }
    }
}
