using System;
using System.Text.Json;
using System.Threading.Tasks;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Services
{
    public class AuditService : IAuditService
    {
        private readonly BillingDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            BillingDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogAsync(
            int? businessId,
            string entityType,
            int? entityId,
            string action,
            object? oldValues = null,
            object? newValues = null,
            string? performedBy = null,
            string? description = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var serializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var auditLog = new AuditLog
                {
                    BusinessId = businessId,
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = action,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues, serializerOptions) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues, serializerOptions) : null,
                    PerformedBy = performedBy ?? GetCurrentUserId(httpContext),
                    IpAddress = GetClientIpAddress(httpContext),
                    UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                    CorrelationId = httpContext?.Items["CorrelationId"]?.ToString(),
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "[AuditLog] {Action} on {EntityType}#{EntityId} by {PerformedBy} | Business: {BusinessId} | CorrelationId: {CorrelationId}",
                    action, entityType, entityId, auditLog.PerformedBy, businessId, auditLog.CorrelationId);
            }
            catch (Exception ex)
            {
                // Audit logging should never break the main flow
                _logger.LogError(ex,
                    "[AuditLog Error] Failed to log {Action} on {EntityType}#{EntityId}",
                    action, entityType, entityId);
            }
        }

        private string? GetCurrentUserId(HttpContext? httpContext)
        {
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.Identity.Name;
            }
            return "anonymous";
        }

        private string? GetClientIpAddress(HttpContext? httpContext)
        {
            if (httpContext == null) return null;

            // Check for forwarded IP (behind reverse proxy/load balancer)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
