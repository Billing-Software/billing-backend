using BillingBackend.DTOs;
using BillingBackend.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class WebhookService : IWebhookService
    {
        private readonly IWhatsAppRepository _repository;
        private readonly string _appSecret;
        private readonly ILogger<WebhookService> _logger;

        public WebhookService(
            IWhatsAppRepository repository,
            IConfiguration configuration,
            ILogger<WebhookService> logger)
        {
            _repository = repository;
            _appSecret = configuration["Meta:AppSecret"] ?? string.Empty;
            _logger = logger;
        }

        public bool ValidateSignature(string payload, string signatureHeader)
        {
            if (string.IsNullOrEmpty(_appSecret) || string.IsNullOrEmpty(signatureHeader))
            {
                _logger.LogWarning("Webhook signature validation skipped — missing AppSecret or signature header");
                return false;
            }

            // Meta sends: "sha256=<hex-digest>"
            if (!signatureHeader.StartsWith("sha256="))
            {
                return false;
            }

            var expectedSignature = signatureHeader["sha256=".Length..];

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_appSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexStringLower(hash);

            return string.Equals(computedSignature, expectedSignature, StringComparison.OrdinalIgnoreCase);
        }

        public async Task ProcessWebhookAsync(MetaWebhookPayload payload)
        {
            if (payload.Entry == null) return;

            foreach (var entry in payload.Entry)
            {
                if (entry.Changes == null) continue;

                foreach (var change in entry.Changes)
                {
                    if (change.Field != "messages") continue;

                    var value = change.Value;

                    // Process status updates (sent, delivered, read, failed)
                    if (value?.Statuses != null)
                    {
                        foreach (var status in value.Statuses)
                        {
                            await ProcessStatusUpdateAsync(status);
                        }
                    }

                    // Process incoming messages (for future Phase 3 — chat)
                    if (value?.Messages != null)
                    {
                        foreach (var message in value.Messages)
                        {
                            _logger.LogInformation(
                                "Incoming WhatsApp message from {From}: {Type}",
                                message.From, message.Type);
                            // Incoming message handling will be implemented in Phase 3
                        }
                    }
                }
            }
        }

        private async Task ProcessStatusUpdateAsync(MetaWebhookStatus status)
        {
            if (string.IsNullOrEmpty(status.Id) || string.IsNullOrEmpty(status.Status))
            {
                _logger.LogWarning("Received status update with missing ID or status");
                return;
            }

            _logger.LogInformation(
                "Processing status update: MessageId={MessageId}, Status={Status}",
                status.Id, status.Status);

            DateTime? timestamp = null;
            if (!string.IsNullOrEmpty(status.Timestamp) && long.TryParse(status.Timestamp, out var unixTimestamp))
            {
                timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
            }

            string? failedReason = null;
            if (status.Errors != null && status.Errors.Length > 0)
            {
                failedReason = $"[{status.Errors[0].Code}] {status.Errors[0].Title}: {status.Errors[0].Message}";
            }

            // Map Meta status to our status values
            var mappedStatus = status.Status?.ToLowerInvariant() switch
            {
                "sent" => "Sent",
                "delivered" => "Delivered",
                "read" => "Read",
                "failed" => "Failed",
                _ => status.Status ?? "Unknown"
            };

            await _repository.UpdateMessageLogStatusAsync(status.Id, mappedStatus, timestamp, failedReason);
        }
    }
}
